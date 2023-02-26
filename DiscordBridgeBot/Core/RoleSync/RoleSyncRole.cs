namespace DiscordBridgeBot.Core.RoleSync
{
    public class RoleSyncRole
    {
        public HashSet<ulong> PossibleIds { get; set; } = new HashSet<ulong>();
        public HashSet<ulong> LinkedUsers { get; set; } = new HashSet<ulong>();

        public string Name { get; set; }
        public string Key { get; set; }
    }
}