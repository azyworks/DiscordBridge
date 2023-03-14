using AzyWorks.Networking;

using System;
using System.IO;

namespace DiscordBridge.CustomNetwork.Punishments
{
    public struct PunishmentEditDurationMessage : INetMessage
    {
        public string TargetId;
        public string TargetIp;

        public TimeSpan Duration;

        public PunishmentType Type;

        public PunishmentEditDurationMessage(string targetId, string targetIp, TimeSpan duration, PunishmentType type)
        {
            TargetId = targetId;
            TargetIp = targetIp;

            Duration = duration;

            Type = type;
        }

        public void Deserialize(BinaryReader reader)
        {
            TargetId = reader.ReadString();
            TargetIp = reader.ReadString();

            Duration = reader.ReadTimeSpan();

            Type = (PunishmentType)reader.ReadByte();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(TargetId);
            writer.Write(TargetIp);

            writer.Write(Duration);

            writer.Write((byte)Type);
        }
    }
}