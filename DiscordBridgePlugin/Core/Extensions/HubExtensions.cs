using PluginAPI.Core;

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

            Log.Info($"Set server role of {hub.nicknameSync.Network_myNickSync} ({hub.characterClassManager.UserId}) to {role}");
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