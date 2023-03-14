### AzureServerlessFileConverter

This was submitted for the Hack Together: Microsoft Graph and .NET hackathon using .NET and Microsoft Graph.

- Use Azure Functions and Graph SDK to convert file between different supported formats
- Coded to download uploaded file as pdf using session upload which can handle large size files
https://learn.microsoft.com/en-us/onedrive/developer/rest-api/api/driveitem_createuploadsession?view=odsp-graph-online
- Can be exteded  based on the formats supported here: 
https://learn.microsoft.com/en-us/onedrive/developer/rest-api/api/driveitem_get_content_format?view=odsp-graph-online
- Loved using graph explorer to get the clientid/secrets etc https://developer.microsoft.com/en-us/graph/graph-explorer

### Challenges faced:
- At the time of submitting this the SDK 5.0.0 was not working with .net 6 and functions runtime 4.0
- figuring out tenant id, client id/secrets etc for the first time
- assigning api permissions (needed admin access)
https://learn.microsoft.com/en-us/graph/migrate-azure-ad-graph-configure-permissions?tabs=http%2Cupdatepermissions-azureadgraph-powershell
- not sure about handing potentially infected files (security with using sharepoint/onedrive as upload folder), cost, clean up of abandoned/failed uploads
- performance was slow but could be due to free tier/using consumption plan for function

### Future potential extensions:
- once file is available for download post teams chat message or email using graph sdk
- extend to use cognitive-services for text summarization, key phrase and named entity recognition
https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/summarization/how-to/document-summarization
- integration potential for HealthCare to categorize patient data/charts etc, PII detection
https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/text-analytics-for-health/overview?tabs=ner
https://learn.microsoft.com/en-us/azure/cognitive-services/language-service/personally-identifiable-information/overview
- based on key phrase or named entity recognition using cognitive-services trigger notification alerts 
https://graph.microsoft.com/beta/me/notifications
- potentially use for translation?
