using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace AzureFileConverter;
internal class FileConversionService
{
    private readonly FileHandlerService _fileHandler;
    private readonly SiteOptions _siteOptions;

    public FileConversionService(FileHandlerService fileService, IOptions<SiteOptions> siteOptions)
    {
        _fileHandler = fileService;
        _siteOptions = siteOptions.Value;
    }

    //todo: could be extended to any other format based on input format passed so pdf to doc too etc
    //function endpoint could be service consumed by other services   
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = "FileConverter")] HttpRequest req,
                                              ILogger log)
    {
        if (req.Headers.ContentLength < 0) 
        {
            log.LogWarning("Invalid file");
            return new BadRequestObjectResult("Invalid file.");
        }

        var sitePath = $"{_siteOptions.GraphEndpoint}sites/{_siteOptions.SiteId}/drive/items/";
        var response = await _fileHandler.UploadFileStreamAsync(sitePath, req.Body, req.ContentType);

        //get odata response for download path and file id
        var odataResponse = JsonConvert.DeserializeObject<OData<string>>(response);
        var fileId = odataResponse.Id; //"largefiletest.pptx:";

        //todo: to extend - get download format from request (https://learn.microsoft.com/en-us/onedrive/developer/rest-api/api/driveitem_get_content_format?view=odsp-graph-online)
        var pdf = await _fileHandler.DownloadConvertedFileAsync(odataResponse.DownloadUrl, string.Empty, "pdf");
        //var path = "https://graph.microsoft.com/v1.0/sites/siteId/drive/items/root:/";
        
        //todo: handle clean up in case of exceptions
        await _fileHandler.CleanUpAsync($"{sitePath}", fileId);
         
        return new FileContentResult(pdf, "application/pdf");
    }

    //todo: better way to do handle OData response?
    private class OData<T>
    {
        [JsonProperty("@content.downloadUrl")]
        public string DownloadUrl { get; set; }
        
        [JsonProperty("id")]
        public string Id { get; set; }  
    }    
}
internal class SiteOptions
{
    public string GraphEndpoint { get; set; }
    public string SiteId { get; set; }
}
