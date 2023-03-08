using Azure.Identity;
using AzureFileConverter;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureServerlessPDFConverter
{
    public class GraphItemsHandler
    {
        private readonly GraphApiAuthOptions _graphApiOptions;

        public GraphItemsHandler(IOptions<GraphApiAuthOptions> options)
        {
            _graphApiOptions = options.Value;
        }

        public GraphServiceClient GetGraphServiceClient()
        {
            // using Azure.Identity;
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            var clientSecretCredential = new ClientSecretCredential(
            _graphApiOptions.TenantId, _graphApiOptions.ClientId, _graphApiOptions.ClientSecret);

            var graphClient = new GraphServiceClient(clientSecretCredential);
            return graphClient;
        }

        //https://learn.microsoft.com/en-us/graph/sdks/large-file-upload?tabs=csharp
        public async Task<UploadResult<DriveItem>> UploadLargeFileAsync(string filePath, Stream content)
        {

            // Use properties to specify the conflict behavior
            // in this case, replace
            var uploadSessionRequestBody = new CreateUploadSessionPostRequestBody
            {
                Item = new DriveItemUploadableProperties
                {
                    AdditionalData = new Dictionary<string, object>
                    {
                        { "@microsoft.graph.conflictBehavior", "replace" }
                    }
                }
            };

            GraphServiceClient graphClient = GetGraphServiceClient();
            var uploadSession = await graphClient.Drives[$"{_graphApiOptions.DriveId}"].Root
                                        .ItemWithPath(filePath)
                                        .CreateUploadSession
                                        .PostAsync(uploadSessionRequestBody);

            // Max slice size must be a multiple of 320 KiB
            int maxSliceSize = 320 * 1024;
            var fileUploadTask = new LargeFileUploadTask<DriveItem>(uploadSession, content, maxSliceSize, graphClient.RequestAdapter);

            var totalLength = content.Length;
            // Create a callback that is invoked after each slice is uploaded
            IProgress<long> progress = new Progress<long>(prog => {
                Console.WriteLine($"Uploaded {prog} bytes of {totalLength} bytes");
            });

            try
            {
                // Upload the file
                var uploadResult = await fileUploadTask.UploadAsync(progress);
                Console.WriteLine(uploadResult.UploadSucceeded ?
                    $"Upload complete, item ID: {uploadResult.ItemResponse.Id}" :
                    "Upload failed");

                return uploadResult;

            }
            catch (ServiceException ex)
            {
                Console.WriteLine($"Error uploading: {ex.ToString()}");
                return null;
            }
        }

        public async Task DeleteFileFromDrive(string itemId)
        {
            GraphServiceClient graphClient = GetGraphServiceClient();

            await graphClient.Drives[$"{_graphApiOptions.DriveId}"].Items[itemId]
                .DeleteAsync();
        }

        //thank you unit tests! https://github.com/microsoftgraph/msgraph-sdk-dotnet/blob/dev/tests/Microsoft.Graph.DotnetCore.Test/Requests/Functional/OneDriveTests.cs
        public async Task<byte[]> OneDriveDownloadLargeFile(string itemId, string targetFormat)
        {            
            const long DefaultChunkSize = 50 * 1024; // 50 KB
            long ChunkSize = DefaultChunkSize;
            long offset = 0;                         
            byte[] bytesInStream;                   

            GraphServiceClient graphClient = GetGraphServiceClient();
            try
            {
                //directly get drive item
                var driveItemInfo = await graphClient.Drives[_graphApiOptions.DriveId].Items[itemId].GetAsync();

                // Get the download URL. This URL is preauthenticated and has a short TTL.
                object downloadUrl;
                driveItemInfo.AdditionalData.TryGetValue("@microsoft.graph.downloadUrl", out downloadUrl);

                long size = driveItemInfo.Size.Value;
                int numberOfChunks = Convert.ToInt32(size / DefaultChunkSize);              
                int lastChunkSize = Convert.ToInt32(size % DefaultChunkSize) - numberOfChunks - 1;
                if (lastChunkSize > 0) { numberOfChunks++; }

                // Create a file stream to contain the downloaded file.
                await using FileStream fileStream = File.Create((driveItemInfo.Name));
                for (int i = 0; i < numberOfChunks; i++)
                {
                    if (i == numberOfChunks - 1)
                    {
                        ChunkSize = lastChunkSize;
                    }

                    // Create the request message with the download URL and Range header.
                    HttpRequestMessage req = new(HttpMethod.Get, $"{(string)downloadUrl}/content?format={targetFormat}"); //use method as the rest api call to convert the file to pdf
                    req.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(offset, ChunkSize + offset);

                    var client = new HttpClient();
                    HttpResponseMessage response = await client.SendAsync(req);

                    await using (Stream responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        bytesInStream = new byte[ChunkSize];
                        int read;
                        do
                        {
                            read = responseStream.Read(bytesInStream, 0, (int)bytesInStream.Length);
                            if (read > 0)
                                fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                        }
                        while (read > 0);
                    }
                    offset += ChunkSize + 1; // Move the offset cursor to the next chunk.
                }

                return GetBytesFromStream(fileStream);
                
            }
            catch (ODataError e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private static byte[] GetBytesFromStream(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
