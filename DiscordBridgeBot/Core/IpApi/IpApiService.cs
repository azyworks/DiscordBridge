using Newtonsoft.Json;

using System.Net;

namespace DiscordBridgeBot.Core.IpApi
{
    public static class IpApiService
    {
        public const string BaseUrl = "http://ip-api.com/json/query?fields=66846719";

        public static string FormatUrl(string ipStr)
        {
            return BaseUrl.Replace("query", ipStr);
        }

        public static async Task<IpApiResult> GetAsync(string ip)
        {
            var ipUrl = FormatUrl(ip);

            using (var webClient = new WebClient())
            {
                var response = await webClient.DownloadStringTaskAsync(ipUrl);
                var result = JsonConvert.DeserializeObject<IpApiResult>(response);

                return result;
            }
        }
    }
}