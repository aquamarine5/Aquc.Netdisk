using System.Net;
using System.Text.Json.Nodes;

namespace Aquc.Netdisk.Smms;

public class SmmsImageHost
{
    public string token;
    public SmmsImageHost(string token)=>this.token = token;
    public async Task<bool> UploadAsync(string filePath)
    {
        using var httpClient=new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(token);
        httpClient.Timeout = TimeSpan.FromSeconds(200);
        var multipartFormData=new MultipartFormDataContent();
        using var file = new FileStream(filePath, FileMode.Open,FileAccess.Read);
        using var content = new StreamContent(file);
        multipartFormData.Add(content, "smfile", DateTime.Now.ToString("yyMMdd_HHmmss_ff")+Path.GetExtension(filePath));
        var response=await httpClient.PostAsync("https://smms.app/api/v2/upload", multipartFormData);
        return (bool)((JsonNode.Parse(await response.Content.ReadAsStringAsync())?["success"])??false);
    }
}