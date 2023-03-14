using DiscordBridge.CustomNetwork.RoleSync;
using DiscordBridge.CustomNetwork.Tickets;

using System;
using System.IO;

namespace DiscordBridge.CustomNetwork
{
    public static class ReaderExtensions
    {
        public static RoleSyncAccount ReadRoleSyncAccount(this BinaryReader reader)
        {
            var account = new RoleSyncAccount();

            account.Name = reader.ReadString();
            account.Id = reader.ReadString();
            account.Role = reader.ReadString();

            return account;
        }

        public static RoleSyncTicket ReadRoleSyncTicket(this BinaryReader reader)
        {
            var ticket = new RoleSyncTicket();

            ticket.Code = reader.ReadString();
            ticket.ChangedAt = new DateTime(reader.ReadInt64());
            ticket.CreatedAt = new DateTime(reader.ReadInt64());
            ticket.Account = reader.ReadRoleSyncAccount();

            return ticket;
        }

        public static DateTime ReadDateTime(this BinaryReader reader)
            => DateTime.FromBinary(reader.ReadInt64());

        public static TimeSpan ReadTimeSpan(this BinaryReader reader)
            => TimeSpan.FromTicks(reader.ReadInt64());
    }
}