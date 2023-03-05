using AzyWorks.System.Services;

using DiscordBridgePlugin.Core.Network;
using DiscordBridgePlugin.Core.Punishments;
using DiscordBridgePlugin.Core.RoleSync;

using PluginAPI.Core;
using PluginAPI.Core.Attributes;

namespace DiscordBridgePlugin
{
    public class LoaderService : ServiceCollection
    {
        [PluginConfig]
        public ConfigService ConfigService;

        public static LoaderService Loader;
        public static ConfigService Config { get => Loader.ConfigService; }

        [PluginEntryPoint("DiscordBridge", "1.0.0", "Bridges your SCP:SL server and Discord together.", "azyworks")]
        public void Load()
        {
            Loader = this;

            Log.Info($"Registering services ..", "Discord Bridge");

            AddService<NetworkService>(Config.NetworkPort);
            AddService<RoleSyncService>();
            AddService<PunishmentsService>();

            Log.Info($"Services registered.", "Discord Bridge");
            Log.Info($"Loaded!", "Discord Bridge");
        }
    }
}