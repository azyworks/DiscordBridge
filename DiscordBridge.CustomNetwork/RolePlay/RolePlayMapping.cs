using System.Collections.Generic;

namespace DiscordBridge.CustomNetwork.RolePlay
{
    public class RolePlayMapping
    {
        public List<RolePlayRole> Roles { get; set; } = new List<RolePlayRole>();

        public string Id { get; set; }
        public string Name { get; set; }
        public string OwnerName { get; set; }

        public ulong OwnerId { get; set; }

        public bool IsActive;
    }
}