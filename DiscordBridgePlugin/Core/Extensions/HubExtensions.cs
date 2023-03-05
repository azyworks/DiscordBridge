using CommandSystem;

using DiscordBridge.CustomNetwork;

using RemoteAdmin;

namespace DiscordBridgePlugin.Core.Extensions
{
    public static class HubExtensions
    {
        public static void SetRole(this ReferenceHub hub, string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return;

            ServerStatic.PermissionsHandler._members[hub.characterClassManager.UserId] = role;

            hub.serverRoles.RefreshPermissions();
            hub.characterClassManager.ConsolePrint($"[Role Sync] Updated role: {role}", "green");
        }
        
        public static PlayerData ToData(this ReferenceHub hub)
        {
            return new PlayerData(
                hub.nicknameSync.Network_myNickSync,
                hub.characterClassManager.UserId,
                hub.GetRole(),
                hub.roleManager.CurrentRole.RoleName,
                hub.connectionToClient.address,
                hub.PlayerId);
        }

        public static PlayerData ToData(this ICommandSender sender)
        {
            if (sender is PlayerCommandSender player)
                return player.ReferenceHub.ToData();

            return new PlayerData("Dedicated Server", "ID_Host");
        }

        public static string GetRole(this ReferenceHub hub)
        {
            if (ServerStatic.PermissionsHandler is null)
                return "";

            if (ServerStatic.PermissionsHandler._members.TryGetValue(hub.characterClassManager.UserId, out var role))
                return role;

            return "";
        }

        public static void RemoveRole(this ReferenceHub hub)
        {
            ServerStatic.PermissionsHandler._members.Remove(hub.characterClassManager.UserId);

            hub.serverRoles.RefreshPermissions();
            hub.serverRoles.SetGroup(null, false);
            hub.characterClassManager.ConsolePrint($"[Role Sync] Role removed.", "green");
        }
    }
}