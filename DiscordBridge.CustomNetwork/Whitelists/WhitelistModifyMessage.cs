using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.Whitelists
{
    public struct WhitelistModifyMessage : INetMessage
    {
        public string UserId;

        public WhitelistModifyMessage(string userId)
        {
            UserId = userId;
        }

        public void Deserialize(BinaryReader reader)
        {
            UserId = reader.ReadString();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(UserId);
        }
    }
}
