using AzyWorks.Networking;

using DiscordBridge.CustomNetwork.RoleSync;

using System.IO;

namespace DiscordBridge.CustomNetwork.ServerMessages.Tickets
{
    public struct RoleSyncInvalidateTicketMessage : INetMessage
    {
        public RoleSyncTicket Ticket;
        public RoleSyncTicketInvalidationReason Reason;

        public RoleSyncInvalidateTicketMessage(RoleSyncTicket ticket, RoleSyncTicketInvalidationReason reason)
        {
            Ticket = ticket;
            Reason = reason;
        }

        public void Deserialize(BinaryReader reader)
        {
            Ticket = reader.ReadRoleSyncTicket();
            Reason = (RoleSyncTicketInvalidationReason)reader.ReadByte();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Ticket);
            writer.Write((byte)Reason);
        }
    }
}