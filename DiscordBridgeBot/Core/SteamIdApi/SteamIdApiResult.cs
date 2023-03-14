namespace DiscordBridgeBot.Core.SteamIdApi
{
    public class SteamIdApiResult
    {
        public bool IsVacBanned { get; set; }
        public bool IsTradeClean { get; set; }
        public bool IsCommunityClean { get; set; }

        public string AvatarUrl { get; set; } = "";
        public string RealName { get; set; } = "";
        public string Country { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        public string LastLogOffAt { get; set; } = "";
        public string Status { get; set; } = "";
        public string Visibility { get; set; } = "";
    }
}