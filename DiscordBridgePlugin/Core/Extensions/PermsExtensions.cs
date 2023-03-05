using PluginAPI.Core;

namespace DiscordBridgePlugin.Core.Extensions
{
    public static class PermsExtensions
    {
        public static bool TryGetRole(string role, out UserGroup group)
        {
            if (ServerStatic.PermissionsHandler is null)
            {
                Log.Warning($"The server's permissions handler is null.", "Discord Bridge :: PermsExtensions");

                group = null;
                return false;
            }

            if (!ServerStatic.PermissionsHandler._groups.TryGetValue(role, out group))
            {
                Log.Warning($"Missing server group definition: {role}", "Discord Bridge :: PermsExtensions");

                group = null; 
                return false;
            }

            return group != null;
        }
    }
}
