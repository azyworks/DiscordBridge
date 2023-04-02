using Newtonsoft.Json;

namespace DiscordBridgeBot.Core.ScpSlServerListApi
{
    public class ScpSlServerListApiItem
    {
        [JsonProperty("serverId")]
        public string ServerId { get; set; }

        [JsonProperty("accountId")]
        public string AccountId { get; set; }

        [JsonProperty("pastebin")]
        public string PastebinId { get; set; }

        [JsonIgnore]
        public string PastebinUrl { get; set; }

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("players")]
        public string PlayersString { get; set; }

        [JsonProperty("version")]
        public string VersionString { get; set; }

        [JsonProperty("official")]
        public string OfficialTypeString { get; set; }

        [JsonProperty("info")]
        public string HashedName { get; set; }

        [JsonIgnore]
        public string ClearName { get; set; }

        [JsonIgnore]
        public string FullName { get; set; }

        [JsonProperty("isoCode")]
        public string IsoCode { get; set; }

        [JsonProperty("continentCode")]
        public string ContinentCode { get; set; }

        [JsonProperty("privateBeta")]
        public bool IsPrivateBeta { get; set; }

        [JsonProperty("friendlyFire")]
        public bool IsFriendlyFireEnabled { get; set; }

        [JsonProperty("modded")]
        public bool IsModded { get; set; }

        [JsonProperty("whitelist")]
        public bool IsWhitelisted { get; set; }

        [JsonProperty("latitude")]
        public float Latitude { get; set; }

        [JsonProperty("longitude")]
        public float Longitude { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("distance")]
        public int Distance { get; set; }

        [JsonProperty("modFlags")]
        public int ModFlagsValue { get; set; }

        [JsonProperty("officialCode")]
        public int OfficialCodeValue { get; set; }

        [JsonProperty("displaySection")]
        public int DisplaySectionValue { get; set; }

        [JsonIgnore]
        public int Players { get; set; }

        [JsonIgnore]
        public int MaxPlayers { get; set; }

        [JsonIgnore]
        public ScpSlServerListOfficialType OfficialType { get; set; }

        [JsonIgnore]
        public Version Version { get; set; }
    }
}