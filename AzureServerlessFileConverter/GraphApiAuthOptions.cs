namespace AzureFileConverter;

internal class GraphApiAuthOptions
{
    public string Endpoint { get; set; } = "https://login.microsoftonline.com/";
    public string GrantType { get; set; } = "client_credentials";
    public string Scope { get; set; } = "Files.ReadWrite.All";
    public string Resource { get; set; } = "https://graph.microsoft.com";
    public string TenantId { get; set; } //secrets
    public string ClientId { get; set; } //secrets
    public string ClientSecret { get; set; } //secrets
    public string SiteId { get; set; } //secrets
    public string DriveId { get; set; } //secrets
}
