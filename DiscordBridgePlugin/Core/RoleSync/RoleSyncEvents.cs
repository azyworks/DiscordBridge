using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Enums;

using DiscordBridgePlugin.Core.Extensions;

namespace DiscordBridgePlugin.Core.RoleSync
{
    public class RoleSyncEvents
    {
        public RoleSyncService RoleSync { get; private set; }

        public RoleSyncEvents()
        {
            RoleSync = LoaderService.Loader.GetService<RoleSyncService>();
            RoleSync.Tickets.OnRoleUpdated += OnRoleUpdated;
        }

        ~RoleSyncEvents()
        {
            RoleSync.Tickets.OnRoleUpdated -= OnRoleUpdated;
            RoleSync = null;
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void OnPlayerJoined(Player player)
        {
            if (RoleSync.TryGetRole(player.UserId, out var role))
            {
                if (!string.IsNullOrWhiteSpace(role.RoleKey) && role.RoleKey != "<NONE>")
                {
                    player.ReferenceHub.SetRole(role.RoleKey);
                }
            }
        }

        public void OnRoleUpdated(string userId, string role)
        {
            if (Player.TryGet(userId, out var player))
            {
                if (role is "<NONE>")
                    player.ReferenceHub.RemoveRole();
                else
                    player.ReferenceHub.SetRole(role);
            }
        }
    }
}