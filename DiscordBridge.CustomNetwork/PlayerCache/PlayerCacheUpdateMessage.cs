using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.PlayerCache
{
    public struct PlayerCacheUpdateMessage : INetMessage
    {
        public string UserId;
        public string UserName;
        public string UserIp;

        public PlayerCacheUpdateMessage(string id, string name, string ip)
        {
            UserId = id;
            UserName = name;
            UserIp = ip;
        }

        public void Deserialize(BinaryReader reader)
        {
            UserId = reader.ReadString();
            UserName = reader.ReadString();
            UserIp = reader.ReadString();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(UserId);
            writer.Write(UserName);
            writer.Write(UserIp);
        }
    }
}