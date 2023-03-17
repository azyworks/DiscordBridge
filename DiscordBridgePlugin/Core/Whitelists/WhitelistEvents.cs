using DiscordBridgePlugin.Core.Extensions;

using PluginAPI.Core;
using PluginAPI.Core.Attributes;

using PluginAPI.Enums;

namespace DiscordBridgePlugin.Core.Whitelists
{
    public class WhitelistEvents
    {
        public WhitelistsService Whitelists { get; }

        public WhitelistEvents()
        {
            Whitelists = LoaderService.Loader.GetService<WhitelistsService>();
        }

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void OnPlayerJoined(Player player)
        {
            if (player.IsServer)
                return;

            if (Whitelists.IsActive)
            {
                if (!Whitelists.Whitelisted.Contains(player.UserId))
                {
                    if (LoaderService.Config.WhitelistIgnored.Contains(player.UserId) 
                        || LoaderService.Config.WhitelistIgnored.Contains(player.ReferenceHub.GetRole())
                        || player.IsNorthwoodStaff 
                        || player.IsGlobalModerator)
                        return;

                    player.Kick(LoaderService.Config.WhitelistKickMessage);

                    Log.Info($"Kicked {player.Nickname}: not on the whitelist");
                }
            }
        }
    }
}
