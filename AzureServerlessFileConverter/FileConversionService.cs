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
        var driveResponse = await FileHandlerService.UploadFileStreamAsync(sitePath, req.Body, req.ContentType);
        var fileId = driveResponse.Id; 

        //todo: to extend - get download format from request (https://learn.microsoft.com/en-us/onedrive/developer/rest-api/api/driveitem_get_content_format?view=odsp-graph-online)
        var pdf = await _fileHandler.DownloadConvertedFileAsync(driveResponse.WebUrl, string.Empty, "pdf"); //todo: do using graph sdk
        //var path = "https://graph.microsoft.com/v1.0/sites/siteId/drive/items/root:/";
        
        //todo: handle clean up in case of exceptions
        await _fileHandler.CleanUpAsync($"{sitePath}", fileId);
         
        return new FileContentResult(pdf, "application/pdf");
    }
    
}
internal class SiteOptions
{
    public string GraphEndpoint { get; set; }
    public string SiteId { get; set; }
}
