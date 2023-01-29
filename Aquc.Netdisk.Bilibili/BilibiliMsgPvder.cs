using System.Collections.Specialized;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;

namespace Aquc.Netdisk.Bilibili
{
    public class BilibiliMsgPvder
    {
        public static async Task<string> Get(string id,int index=0)
        {
            var http = new HttpClient
            {
                //BaseAddress = new Uri("https://api.vc.bilibili.com")
            };
            /*
            var response = await http.GetAsync($"/dynamic_svr/v1/dynamic_svr/get_dynamic_detail?dynamic_id={id}");
            var commitText=JsonNode.Parse(JsonNode.Parse(
                await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync())
                !["data"]!["card"]!["card"]!.ToString())
                !["item"]!["content"]!.ToString();
            */
            //Console.WriteLine($"https://api.vc.bilibili.com/x/v2/reply/main?jsonp=jsonp&next=0&type=17&mode=3&plat=1&oid={id}");
            var response = await http.GetAsync($"https://api.bilibili.com/x/v2/reply/main?jsonp=jsonp&next=0&type=11&oid={id}&mode=3&plat=1");
            var replies = JsonNode.Parse(await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync())!["data"]!["replies"]!.AsArray();
            http.Dispose();
            if (replies.Count > index)
                return replies[index]!["content"]!["message"]!.ToString();
            else
                throw new IndexOutOfRangeException();
        }
        public static List<string> GetFirstPageAll(string id)
        {
            throw new NotImplementedException();
        }
        public static async Task<string> Get(long id) =>
            await Get(id.ToString());
    }
}