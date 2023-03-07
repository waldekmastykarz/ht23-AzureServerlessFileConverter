using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AzureFileConverter;
internal class AuthenticationService
{
    private readonly GraphApiAuthOptions _graphApiOptions;

    public AuthenticationService(IOptions<GraphApiAuthOptions> options)
    {
        _graphApiOptions = options.Value;
    }

    internal async Task<string> GetAccessTokenAsync()
    {
        var values = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("client_id", _graphApiOptions.ClientId),
            new KeyValuePair<string, string>("client_secret", _graphApiOptions.ClientSecret),
            new KeyValuePair<string, string>("scope", _graphApiOptions.Scope),
            new KeyValuePair<string, string>("grant_type", _graphApiOptions.GrantType),
            new KeyValuePair<string, string>("resource", _graphApiOptions.Resource)
        };
        using var client = new HttpClient();
        var requestUrl = $"{_graphApiOptions.Endpoint}{_graphApiOptions.TenantId}/oauth2/token";
        var requestContent = new FormUrlEncodedContent(values);
        var response = await client.PostAsync(requestUrl, requestContent);
        var responseBody = await response.Content.ReadAsStringAsync();
        dynamic tokenResponse = JsonConvert.DeserializeObject(responseBody);
        return tokenResponse?.access_token;
    }

    internal class GraphApiAuthOptions
    {
        public string Endpoint { get; set; } = "https://login.microsoftonline.com/";
        public string GrantType { get; set; } = "client_credentials";
        public string Scope { get; set; } = "Files.ReadWrite.All";
        public string Resource { get; set; } = "https://graph.microsoft.com";
        public string TenantId { get; set; } //secrets
        public string ClientId { get; set; } //secrets
        public string ClientSecret { get; set; } //secrets
        public string GroupId { get; set;} //secrets
        public string TeamId { get; set; } //secrets
    }
}

