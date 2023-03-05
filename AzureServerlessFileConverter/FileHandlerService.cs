﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace AzureFileConverter;
internal class FileHandlerService
{
    private readonly AuthenticationService _authenticationService;
    private HttpClient _httpClient;

    internal FileHandlerService(AuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
    }

    private async Task<HttpClient> CreateAuthorizedHttpClient()
    {
        if (_httpClient != null)
        {
            return _httpClient;
        }

        var token = await _authenticationService.GetAccessTokenAsync();
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

        return _httpClient;
    }

    internal async Task<byte[]> DownloadConvertedFileAsync(string path, string fileId, string targetFormat)
    {
        var httpClient = await CreateAuthorizedHttpClient();

        var requestUrl = $"{path}{fileId}/content?format={targetFormat}";
        var response = await httpClient.GetAsync(requestUrl);
        if (response.IsSuccessStatusCode)
        {
            var fileContent = await response.Content.ReadAsByteArrayAsync();
            return fileContent;
        }
        else
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception($"Download of converted file failed with status {response.StatusCode} and message {message}");
        }
    }

    //clean up resources after conversion; could be re-used to delete any file by passing the fileId
    internal async Task CleanUpAsync(string path, string fileId)
    {
        var httpClient = await CreateAuthorizedHttpClient();

        var requestUrl = $"{path}{fileId}";
        var response = await httpClient.DeleteAsync(requestUrl);
        if (!response.IsSuccessStatusCode)
        {
            var message = await response.Content.ReadAsStringAsync();
            throw new Exception($"CleanUp failed with status {response.StatusCode} and message {message}");
        }
    }

    //Upload large file: https://docs.microsoft.com/en-us/onedrive/developer/rest-api/api/driveitem_createuploadsession?view=odsp-graph-online
    //This can be used for smaller file sizes as well
    internal async Task<string> UploadFileStreamAsync(string path, Stream content, string contentType)
    {
        var httpClient = await CreateAuthorizedHttpClient();

        string tmpFileName = $"{Guid.NewGuid()}{MimeTypes.MimeTypeMap.GetExtension(contentType)}";
        
        string requestUrl = $"{path}root:/{tmpFileName}:/createUploadSession"; //create session for uploading the file stream
        //sample url: "https://graph.microsoft.com/v1.0/sites/siteId/drive/items/root:/createUploadSession"

        var requestContent = new StreamContent(content);
        requestContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var sessionResponse = httpClient.PostAsync(requestUrl, null).Result.Content.ReadAsStringAsync().Result;
        
        var uploadSession = JsonConvert.DeserializeObject<UploadSessionResponse>(sessionResponse);

            byte[] sContents = GetBytesFromStream(content);
            string response = UploadFileBySession(uploadSession.uploadUrl, sContents);
            return response;            
    }

    private static string UploadFileBySession(string url, byte[] file)
    {
        int fragSize = 1024 * 1024 * 4;
        var arrayBatches = ByteArrayIntoBatches(file, fragSize);
        int start = 0;
        string response = "";

        foreach (var byteArray in arrayBatches) //todo: better way to do this?
        {
            int byteArrayLength = byteArray.Length;
            var contentRange = " bytes " + start + "-" + (start + (byteArrayLength - 1)) + "/" + file.Length;

            using (var client = new HttpClient())
            {
                var content = new ByteArrayContent(byteArray);
                content.Headers.Add("Content-Length", byteArrayLength.ToString());
                content.Headers.Add("Content-Range", contentRange);

                response = client.PutAsync(url, content).Result.Content.ReadAsStringAsync().Result;
            }

            start += byteArrayLength;
        }
        return response;
    }

    private static IEnumerable<byte[]> ByteArrayIntoBatches(byte[] bArray, int intBufforLengt)
    {
        int bArrayLenght = bArray.Length;
        int i = 0;
        byte[] bReturn;
        for (; bArrayLenght > (i + 1) * intBufforLengt; i++)
        {
            bReturn = new byte[intBufforLengt];
            Array.Copy(bArray, i * intBufforLengt, bReturn, 0, intBufforLengt);
            yield return bReturn;
        }

        int intBufforLeft = bArrayLenght - i * intBufforLengt;
        if (intBufforLeft > 0)
        {
            bReturn = new byte[intBufforLeft];
            Array.Copy(bArray, i * intBufforLengt, bReturn, 0, intBufforLeft);
            yield return bReturn;
        }
    }

    private static byte[] GetBytesFromStream(Stream input)
    {
        using MemoryStream ms = new();
        input.CopyTo(ms);
        return ms.ToArray();
    }
}

internal class UploadSessionResponse
{
    public string odatacontext { get; set; }
    public DateTime expirationDateTime { get; set; }
    public string[] nextExpectedRanges { get; set; }
    public string uploadUrl { get; set; }
}
