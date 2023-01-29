using Microsoft.Extensions.Hosting;
using System.Text.Json.Nodes;
using System;

namespace Aquc.Netdisk.Huang1111;

public class Huang1111Netdisk
{
    public static async Task<string> Download(string shareKey,string toDirectory,string filename)
    {
        using var http = new HttpClient();
        var url = await http.PutAsync($"https://pan.huang1111.cn/api/v3/share/download/{shareKey}",new StringContent(""));
        var filemsg=await http.GetAsync( JsonNode.Parse(await url.Content.ReadAsStreamAsync())!["data"]!.ToString());
        using Stream stream = await filemsg.Content.ReadAsStreamAsync();
        var filepath = Path.Combine(toDirectory, filename);
        using FileStream fs = new(filepath, FileMode.CreateNew);
        byte[] buffer = new byte[1024];
        int size= await stream.ReadAsync(buffer);
        while (size > 0)
        {
            fs.Write(buffer, 0, size);
            size = await stream.ReadAsync(buffer);
        }
        return filepath;
    }   
    
}