namespace DiscordBridge.CustomNetwork.RoleSync
{
    public struct RoleSyncAccount
    {
        public const string NoneRole = "<!--NONE--!>";

        public string Name;
        public string Id;

        public string Role;

        public RoleSyncAccount(string name, string id, string role = NoneRole)
        {
            Name = name; 
            Id = id;
            Role = role;
        }
    }
}