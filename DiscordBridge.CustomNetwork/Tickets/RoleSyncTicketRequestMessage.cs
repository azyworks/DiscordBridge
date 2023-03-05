using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.Tickets
{
    public struct RoleSyncTicketRequestMessage : INetMessage
    {
        public RoleSyncTicket Ticket;

        public RoleSyncTicketRequestMessage(RoleSyncTicket ticket)
        {
            Ticket = ticket;
        }

        public void Deserialize(BinaryReader reader)
        {
            Ticket = reader.ReadRoleSyncTicket();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Ticket);
        }
    }
}