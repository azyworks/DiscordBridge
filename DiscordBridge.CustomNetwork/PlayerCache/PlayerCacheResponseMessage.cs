using AzyWorks.Networking;

using System.Collections.Generic;
using System.IO;

namespace DiscordBridge.CustomNetwork.PlayerCache
{
    public struct PlayerCacheResponseMessage : INetMessage
    {
        public bool IsSuccess;
        public bool QueryIpType;

        public string Query;

        public string Id;
        public string Ip;
        public string Name;

        public List<string> AllIDs;
        public List<string> AllNames;

        public PlayerCacheResponseMessage(bool success, bool queryIp, string query, string id, string ip, string name, List<string> allIDs, List<string> allNames)
        {
            IsSuccess = success;
            QueryIpType = queryIp;

            Query = query ?? "NONE";

            Id = id ?? "NONE";
            Ip = ip ?? "NONE";
            Name = name ?? "NONE";

            AllIDs = allIDs ?? new List<string>();
            AllNames = allNames ?? new List<string>();
        }

        public void Deserialize(BinaryReader reader)
        {
            IsSuccess = reader.ReadBoolean();
            QueryIpType = reader.ReadBoolean();

            Query = reader.ReadString();

            Id = reader.ReadString();
            Ip = reader.ReadString();
            Name = reader.ReadString();

            var idSize = reader.ReadInt32();
            var nameSize = reader.ReadInt32();

            AllIDs = new List<string>(idSize);
            AllNames = new List<string>(nameSize);

            for (int i = 0; i < idSize; i++)
                AllIDs.Add(reader.ReadString());

            for (int i = 0; i < nameSize; i++)
                AllNames.Add(reader.ReadString());
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(IsSuccess);
            writer.Write(QueryIpType);

            writer.Write(Query);

            writer.Write(Id);
            writer.Write(Ip);
            writer.Write(Name);

            writer.Write(AllIDs.Count);
            writer.Write(AllNames.Count);

            foreach (var item in AllIDs)
                writer.Write(item);

            foreach (var item in AllNames)
                writer.Write(item);
        }
    }
}