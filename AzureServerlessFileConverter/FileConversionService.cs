using AzureServerlessPDFConverter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace AzureFileConverter;
public class FileConversionService
{
    private readonly GraphItemsHandler _graphItemsHandler;
    private readonly SiteOptions _siteOptions;

    public FileConversionService(GraphItemsHandler graphItemsHandler, IOptions<SiteOptions> siteOptions)
    {
        _graphItemsHandler = graphItemsHandler;
        _siteOptions = siteOptions.Value;
    }

    [FunctionName("FileConversionService")]
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
        var driveResponse = await _graphItemsHandler.UploadLargeFileAsync(sitePath, req.Body);
        var fileId = driveResponse.ItemResponse.Id; 

        var pdf = await _graphItemsHandler.OneDriveDownloadLargeFile(fileId, "pdf"); //todo: do using graph sdk
        //var path = "https://graph.microsoft.com/v1.0/sites/siteId/drive/items/root:/";
        
        //todo: handle clean up in case of exceptions
        await _graphItemsHandler.DeleteFileFromDrive(fileId);
         
        return new FileContentResult(pdf, "application/pdf");
    }    
}

public class SiteOptions
{
    public string GraphEndpoint { get; set; }
    public string SiteId { get; set; }
}
