using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.RemoteAdmin
{
    public struct RemoteAdminExecuteMessage : INetMessage
    {
        public string Command;

        public string SenderName;
        public string SenderId;

        public RemoteAdminExecuteMessage(string cmd, string name, string id)
        {
            Command = cmd;

            SenderName = name;
            SenderId = id;
        }

        public void Deserialize(BinaryReader reader)
        {
            Command = reader.ReadString();

            SenderName = reader.ReadString();
            SenderId = reader.ReadString();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Command);

            writer.Write(SenderName);
            writer.Write(SenderId);
        }
    }
}