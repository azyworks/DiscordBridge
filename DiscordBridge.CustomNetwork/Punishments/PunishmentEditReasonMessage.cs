using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.Punishments
{
    public struct PunishmentEditReasonMessage : INetMessage
    {
        public string TargetId;
        public string TargetIp;

        public string Reason;

        public PunishmentType Type;

        public PunishmentEditReasonMessage(string targetId, string targetIp, string reason, PunishmentType type)
        {
            TargetId = targetId;
            TargetIp = targetIp;

            Reason = reason;

            Type = type;
        }

        public void Deserialize(BinaryReader reader)
        {
            TargetId = reader.ReadString();
            TargetIp = reader.ReadString();

            Reason = reader.ReadString();

            Type = (PunishmentType)reader.ReadByte();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TargetId);
            writer.Write(TargetIp);

            writer.Write(Reason);

            writer.Write((byte)Type);
        }
    }
}