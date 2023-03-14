namespace DiscordBridgeBot.Core.PlayerCache
{
    public class PlayerCacheItem
    {
        public string UserIp { get; set; }

        public string LastUserName { get; set; }
        public string LastUserId { get; set; }

        public HashSet<string> AllNames { get; set; } = new HashSet<string>();
        public HashSet<string> AllIDs { get; set; } = new HashSet<string>();
    }
}