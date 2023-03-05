using AzyWorks.Networking;

using System;
using System.IO;

namespace DiscordBridge.CustomNetwork.Punishments
{
    public struct PlayerBannedMessage : INetMessage
    {
        public PlayerData Issuer;
        public PlayerData Banned;

        public DateTime IssuedAt;
        public DateTime ExpiresAt;

        public string Reason;

        public PlayerBannedMessage(PlayerData issuer, PlayerData banned, string reason, DateTime issuedAt, DateTime expiresAt)
        {
            Issuer = issuer;
            Banned = banned;

            Reason = reason;

            IssuedAt = issuedAt;
            ExpiresAt = expiresAt;
        }

        public void Deserialize(BinaryReader reader)
        {
            Issuer = reader.ReadPlayerData();
            Banned = reader.ReadPlayerData();

            IssuedAt = reader.ReadDateTime();
            ExpiresAt = reader.ReadDateTime();

            Reason = reader.ReadString();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Issuer);
            writer.Write(Banned);

            writer.Write(IssuedAt);
            writer.Write(ExpiresAt);

            writer.Write(Reason);
        }
    }
}
