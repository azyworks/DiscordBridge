using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.PlayerCache
{
    public struct PlayerCacheRequestMessage : INetMessage
    {
        public string Target;

        public bool IsIpQuery;

        public PlayerCacheRequestMessage(string target, bool isIpQuery)
        {
            this.Target = target;
            this.IsIpQuery = isIpQuery;
        }

        public void Deserialize(BinaryReader reader)
        {
            this.Target = reader.ReadString();
            this.IsIpQuery = reader.ReadBoolean();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(this.Target);
            writer.Write(this.IsIpQuery);
        }
    }
}