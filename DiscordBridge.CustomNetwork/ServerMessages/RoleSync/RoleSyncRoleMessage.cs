using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.ServerMessages.RoleSync
{
    public struct RoleSyncRoleMessage : INetMessage
    {
        public string UserId;
        public string NewRole;

        public RoleSyncRoleMessage(string userId, string role)
        {
            UserId = userId;
            NewRole = role;
        }

        public bool IsNoneRole()
            => string.IsNullOrWhiteSpace(NewRole) || NewRole == "<NONE>";

        public void Deserialize(BinaryReader reader)
        {
            UserId = reader.ReadString();
            NewRole = reader.ReadString();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(UserId);
            writer.Write(NewRole);
        }
    }
}