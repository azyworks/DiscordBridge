using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.RemoteAdmin
{
    public struct RemoteAdminExecuteResponseMessage : INetMessage
    {
        public string Command;

        public string SenderName;
        public string SenderId;

        public string Response;

        public bool IsSuccess;

        public RemoteAdminExecuteResponseMessage(string cmd, string name, string id, string response, bool success)
        {
            Command = cmd;

            SenderName = name;
            SenderId = id;

            Response = response;

            IsSuccess = success;
        }

        public void Deserialize(BinaryReader reader)
        {
            Command = reader.ReadString();

            SenderName = reader.ReadString();
            SenderId = reader.ReadString();

            Response = reader.ReadString();

            IsSuccess = reader.ReadBoolean();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Command);

            writer.Write(SenderName);
            writer.Write(SenderId);

            writer.Write(Response);

            writer.Write(IsSuccess);
        }
    }
}