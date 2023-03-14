using AzyWorks.Networking;

using System;
using System.IO;

namespace DiscordBridge.CustomNetwork.Punishments
{
    public struct PunishmentIssuedMessage : INetMessage
    {
        public string IssuerId;
        public string IssuerName;

        public string Name;
        public string Id;
        public string Ip;

        public DateTime IssuedAt;
        public DateTime EndsAt;

        public string Reason;

        public PunishmentType Type;

        public PunishmentIssuedMessage(string issuerId, string issuerName, string name, string id, string ip, string reason, DateTime issuedAt, DateTime endsAt, PunishmentType type)
        {
            IssuerId = issuerId;
            IssuerName = issuerName;

            Name = name;
            Id = id;
            Ip = ip;

            Reason = reason;

            Type = type;

            IssuedAt = issuedAt;
            EndsAt = endsAt;
        }

        public void Deserialize(BinaryReader reader)
        {
            IssuerId = reader.ReadString();
            IssuerName = reader.ReadString();

            Name = reader.ReadString();
            Id = reader.ReadString();
            Ip = reader.ReadString();

            IssuedAt = reader.ReadDateTime();
            EndsAt = reader.ReadDateTime();

            Reason = reader.ReadString();

            Type = (PunishmentType)reader.ReadByte();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(IssuerId);
            writer.Write(IssuerName);

            writer.Write(Name);
            writer.Write(Id);
            writer.Write(Ip);

            writer.Write(IssuedAt);
            writer.Write(EndsAt);

            writer.Write(Reason);

            writer.Write((byte)Type);
        }
    }
}
