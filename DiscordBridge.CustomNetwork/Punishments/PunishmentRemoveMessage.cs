using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.Punishments
{
    public struct PunishmentRemoveMessage : INetMessage
    {
        public string TargetId;
        public string TargetIp;

        public PunishmentType Type;

        public PunishmentRemoveMessage(string targetId, string targetedIp, PunishmentType type)
        {
            TargetId = targetId;
            TargetIp = targetedIp;

            Type = type;
        }

        public void Deserialize(BinaryReader reader)
        {
            TargetId = reader.ReadString();
            TargetIp = reader.ReadString();

            Type = (PunishmentType)reader.ReadByte();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TargetId);
            writer.Write(TargetIp);

            writer.Write((byte)Type);
        }
    }
}