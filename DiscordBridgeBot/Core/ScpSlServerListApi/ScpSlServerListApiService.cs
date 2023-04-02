using Newtonsoft.Json;

using System.Net;
using System.Text;

namespace DiscordBridgeBot.Core.ScpSlServerListApi
{
    public static class ScpSlServerListApiService
    {
        public const string ServerListApiAddress = "https://api.scpsecretlab.pl/lobbylist";

        public static async Task<List<ScpSlServerListApiItem>> WhereAsync(Func<ScpSlServerListApiItem, bool> predicate)
        {
            return (await GetAsync()).Servers.Where(predicate).ToList();
        }

        public static async Task<ScpSlServerListApiItem> QueryAsync(string queryValue, ScpSlServerListApiQueryType scpSlServerListApiQueryType)
        {
            var api = await GetAsync();

            foreach (var item in api.Servers)
            {
                if (TryMatch(item, queryValue, scpSlServerListApiQueryType))
                {
                    return item;
                }
            }

            return null;
        }

        public static async Task<List<ScpSlServerListApiItem>> MatchAsync(string matchValue, ScpSlServerListApiQueryType scpSlServerListApiQueryType)
        {
            var api = await GetAsync();
            var list = new List<ScpSlServerListApiItem>();

            foreach (var item in api.Servers)
            {
                if (TryMatch(item, matchValue, scpSlServerListApiQueryType))
                {
                    list.Add(item);
                }
            }

            return list;
        }

        public static async Task<ScpSlServerListApiResponse> GetAsync()
        {
            using (var webClient = new WebClient())
            {
                var jsonString = await webClient.DownloadStringTaskAsync(ServerListApiAddress);
                if (string.IsNullOrWhiteSpace(jsonString))
                    return null;

                return new ScpSlServerListApiResponse(JsonConvert.DeserializeObject<List<ScpSlServerListApiItem>>(jsonString));
            }
        }

        public static bool TryGetOfficialType(string officialTypeString, out ScpSlServerListOfficialType scpSlServerListOfficialType)
        {
            if (string.IsNullOrWhiteSpace(officialTypeString))
            {
                scpSlServerListOfficialType = ScpSlServerListOfficialType.None;
                return false;
            }

            if (officialTypeString is "REGIONAL OFFICIAL")
            {
                scpSlServerListOfficialType = ScpSlServerListOfficialType.RegionalOfficial;
                return true;
            }

            scpSlServerListOfficialType = ScpSlServerListOfficialType.Unknown;
            return false;
        }

        public static bool TryGetName(string nameHash, out string name)
        {
            if (string.IsNullOrEmpty(nameHash))
            {
                name = null;
                return false;
            }

            try
            {
                var decodedBytes = Convert.FromBase64String(nameHash);
                if (decodedBytes is null || decodedBytes.Length < 1)
                {
                    name = null;
                    return false;
                }

                name = Encoding.UTF8.GetString(decodedBytes);
                return !string.IsNullOrWhiteSpace(name);
            }
            catch
            {

            }

            name = null;
            return false;
        }

        public static bool TryGetPlayers(string playersString, out int players, out int maxPlayers)
        {
            if (string.IsNullOrEmpty(playersString))
            {
                players = 0;
                maxPlayers = 0;
                return false;
            }

            var args = playersString.Split('/');
            if (args is null || args.Length != 2)
            {
                players = 0;
                maxPlayers = 0;
                return false;
            }

            if (!int.TryParse(args[0], out players) || !int.TryParse(args[1], out maxPlayers))
            {
                players = 0;
                maxPlayers = 0;
                return false;
            }

            return true;
        }

        public static bool TryMatch(ScpSlServerListApiItem scpSlServerListApiItem, string matchValue, ScpSlServerListApiQueryType scpSlServerListApiQueryType)
        {
            switch (scpSlServerListApiQueryType)
            {
                case ScpSlServerListApiQueryType.Ip:
                    return scpSlServerListApiItem.Ip == matchValue;

                case ScpSlServerListApiQueryType.IpWithPort:
                    return $"{scpSlServerListApiItem.Ip}:{scpSlServerListApiItem.Port}" == matchValue;

                case ScpSlServerListApiQueryType.PastebinId:
                    return scpSlServerListApiItem.PastebinId == matchValue;

                case ScpSlServerListApiQueryType.ServerId:
                    return scpSlServerListApiItem.ServerId == matchValue;

                case ScpSlServerListApiQueryType.AccountId:
                    return scpSlServerListApiItem.AccountId == matchValue;

                case ScpSlServerListApiQueryType.ServerName:
                    {
                        var matchLowered = matchValue.ToLower();
                        var nameLowered = scpSlServerListApiItem.ClearName.ToLower();

                        return matchLowered == nameLowered || nameLowered.Contains(matchLowered);
                    }

                case ScpSlServerListApiQueryType.All:
                    return TryMatch(scpSlServerListApiItem, matchValue, ScpSlServerListApiQueryType.Ip)
                        || TryMatch(scpSlServerListApiItem, matchValue, ScpSlServerListApiQueryType.IpWithPort)
                        || TryMatch(scpSlServerListApiItem, matchValue, ScpSlServerListApiQueryType.PastebinId)
                        || TryMatch(scpSlServerListApiItem, matchValue, ScpSlServerListApiQueryType.ServerId)
                        || TryMatch(scpSlServerListApiItem, matchValue, ScpSlServerListApiQueryType.AccountId)
                        || TryMatch(scpSlServerListApiItem, matchValue, ScpSlServerListApiQueryType.ServerName);

                default:
                    return false;
            }
        }

        public static bool TryGetQueryType(string queryStr, out ScpSlServerListApiQueryType queryType)
        {
            if (string.IsNullOrWhiteSpace(queryStr))
            {
                queryType = default;
                return false;
            }

            switch (queryStr.ToLower()) 
            {
                case "ip":
                    queryType = ScpSlServerListApiQueryType.Ip;
                    return true;

                case "ip_port":
                case "ipport":
                    queryType = ScpSlServerListApiQueryType.IpWithPort;
                    return true;

                case "pasid":
                case "pastebin":
                case "pastebinid":
                    queryType = ScpSlServerListApiQueryType.PastebinId;
                    return true;

                case "id":
                case "sid":
                case "serid":
                case "serverid":
                    queryType = ScpSlServerListApiQueryType.ServerId;
                    return true;

                case "accountid":
                case "accid":
                case "aid":
                    queryType = ScpSlServerListApiQueryType.AccountId;
                    return true;

                case "name":
                case "servername":
                case "sername":
                    queryType = ScpSlServerListApiQueryType.ServerName;
                    return true;

                case "all":
                case "*":
                    queryType = ScpSlServerListApiQueryType.All;
                    return true;

                default:
                    queryType = ScpSlServerListApiQueryType.All;
                    return false;
            }
        }
    }
}