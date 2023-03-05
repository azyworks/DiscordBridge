using System;

using DiscordBridge.CustomNetwork.RoleSync;

namespace DiscordBridge.CustomNetwork.Tickets
{
    public struct RoleSyncTicket
    {
        public string Code;

        public RoleSyncAccount Account;

        public DateTime CreatedAt;
        public DateTime ChangedAt;

        public RoleSyncTicket(string code, RoleSyncAccount account, DateTime createdAt, DateTime changedAt)
        {
            Code = code; 
            Account = account;
            ChangedAt = createdAt;
            CreatedAt = createdAt;
        }
    }
}
