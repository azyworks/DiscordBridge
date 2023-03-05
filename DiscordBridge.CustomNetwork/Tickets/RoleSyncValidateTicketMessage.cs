using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.Tickets
{
    public struct RoleSyncValidateTicketMessage : INetMessage
    {
        public RoleSyncTicket Ticket;
        public RoleSyncTicketValidationReason Reason;

        public RoleSyncValidateTicketMessage(RoleSyncTicket ticket, RoleSyncTicketValidationReason reason)
        {
            Ticket = ticket;
            Reason = reason;
        }

        public void Deserialize(BinaryReader reader)
        {
            Ticket = reader.ReadRoleSyncTicket();
            Reason = (RoleSyncTicketValidationReason)reader.ReadByte();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Ticket);
            writer.Write((byte)Reason);
        }
    }
}