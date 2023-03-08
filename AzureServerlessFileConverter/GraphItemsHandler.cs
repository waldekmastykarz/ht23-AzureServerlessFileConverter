using Azure.Identity;
using AzureFileConverter;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Drives.Item.Items.Item.CreateUploadSession;
using Microsoft.Graph.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AzureServerlessPDFConverter
{
    internal class GraphItemsHandler
    {
        private static GraphApiAuthOptions _graphApiOptions;

        internal GraphItemsHandler(IOptions<GraphApiAuthOptions> options)
        {
            _graphApiOptions = options.Value;
        }

        private static GraphServiceClient GetGraphServiceClient()
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

        internal static async Task<UploadResult<DriveItem>> UploadLargeFileAsync(string filePath, Stream content)
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
        internal static async Task DeleteFileFromDrive(string itemId)
        {
            GraphServiceClient graphClient = GetGraphServiceClient();
            await graphClient.Drives[$"{_graphApiOptions.DriveId}"].Items[itemId]
                .DeleteAsync();
        }

        //TODO: Figure out compiler errors for QueryOption
        internal static async Task<Stream> DownloadItemFromDrive(string itemId)
        {
            GraphServiceClient graphClient = GetGraphServiceClient();

            //todo: Figure out compiler errors for QueryOption 
            //var queryOptions = new List<QueryOption>()
            //{
            //    new QueryOption("format", "pdf")
            //};

            var fileContent = await graphClient.Drives[_graphApiOptions.DriveId].Items[itemId].Content
            // .Request(queryOptions)
            .GetAsync();
            
            return fileContent;
        }


    }
}
