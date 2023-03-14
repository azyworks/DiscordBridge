using Newtonsoft.Json;

namespace DiscordBridgeBot.Core.IpApi
{
    public class IpApiResult
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("continent")]
        public string Continent { get; set; }

        [JsonProperty("continentCode")]
        public string ContinentCode { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("countryCode")]
        public string CountryIso { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("regionName")]
        public string RegionName { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("district")]
        public string District { get; set; }

        [JsonProperty("zip")]
        public string ZipCode { get; set; }

        [JsonProperty("lat")]
        public float Latitude { get; set; }

        [JsonProperty("lon")]
        public float Longitude { get; set; }

        [JsonProperty("timezone")]
        public string TimeZone { get; set; }

        [JsonProperty("offset")]
        public int TimeZoneOffset { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("isp")]
        public string InternetServiceProviderName { get; set; }

        [JsonProperty("org")]
        public string OrganizationName { get; set; }

        [JsonProperty("as")]
        public string AsId { get; set; }

        [JsonProperty("asname")]
        public string AsName { get; set; }

        [JsonProperty("reverse")]
        public string ReverseDns { get; set; }

        [JsonProperty("mobile")]
        public bool IsMobile { get; set; }

        [JsonProperty("proxy")]
        public bool IsProxy { get; set; }

        [JsonProperty("hosting")]
        public bool IsHosting { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }
    }
}