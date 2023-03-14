using System.Collections.Generic;

namespace DiscordBridge.CustomNetwork.RolePlay
{
    public class RolePlayRole
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";

        public int MaxPlayerCount { get; set; } = -1;

        public HashSet<string> Reserved = new HashSet<string>();
    }
}