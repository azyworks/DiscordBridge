namespace DiscordBridgeBot.Core.RoleSync
{
    public class RoleSyncCacheAccount
    {
        public string UserId { get; set; }
        public string UserName { get; set; }

        public ulong DiscordId { get; set; }
    }
}