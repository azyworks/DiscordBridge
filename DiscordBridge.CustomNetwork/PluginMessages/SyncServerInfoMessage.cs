using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.PluginMessages
{
    public struct SyncServerInfoMessage : INetMessage
    {
        public string Name;
        public int Port;

        public SyncServerInfoMessage(string name, int port)
        {
            Name = name;
            Port = port;
        }

        public void Deserialize(BinaryReader reader)
        {
            Name = reader.ReadString();
            Port = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Port);
        }
    }
}