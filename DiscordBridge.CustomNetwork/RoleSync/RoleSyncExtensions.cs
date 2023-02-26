using System.IO;
using System;

namespace DiscordBridge.CustomNetwork.RoleSync
{
    public static class RoleSyncExtensions
    {
        public static BinaryWriter Write(this BinaryWriter writer, RoleSyncTicket ticket)
        {
            writer.Write(ticket.Code);
            writer.Write(ticket.ChangedAt.Ticks);
            writer.Write(ticket.CreatedAt.Ticks);
            writer.Write(ticket.Account);

            return writer;
        }

        public static BinaryWriter Write(this BinaryWriter writer, RoleSyncAccount account) 
        {
            writer.Write(account.Name);
            writer.Write(account.Id);

            return writer;
        }

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
    }
}
