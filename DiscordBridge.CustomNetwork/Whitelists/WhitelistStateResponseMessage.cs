using AzyWorks.Networking;

using System.Collections.Generic;
using System.IO;

namespace DiscordBridge.CustomNetwork.Whitelists
{
    public struct WhitelistStateResponseMessage : INetMessage
    {
        public bool IsActive;

        public HashSet<string> WhitelistedIds;

        public WhitelistStateResponseMessage(bool isActive, HashSet<string> whitelistedIds)
        {
            IsActive = isActive;
            WhitelistedIds = whitelistedIds;
        }

        public void Deserialize(BinaryReader reader)
        {
            IsActive = reader.ReadBoolean();
            
            var size = reader.ReadInt32();

            WhitelistedIds = new HashSet<string>(size);

            for (int i = 0; i < size; i++)
            {
                WhitelistedIds.Add(reader.ReadString());
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(IsActive);
            writer.Write(WhitelistedIds.Count);

            foreach (var id in WhitelistedIds)
                writer.Write(id);
        }
    }
}
