using AzyWorks.Logging;
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

            LogStream.OnMessageLogged += (x, y, z) => Log.Raw($"{x} {y} {z}");

            AzyWorks.Log.BlacklistedLevels.Clear();
            AzyWorks.Log.BlacklistedSources.Clear();

            Log.Info($"Registering services ..", "Discord Bridge");

            AddService<NetworkService>(Config.NetworkPort);
            AddService<RoleSyncService>();
            AddService<PunishmentsService>();

            Log.Info($"Services registered.", "Discord Bridge");
            Log.Info($"Loaded!", "Discord Bridge");
        }

        [PluginUnload]
        public void Unload()
        {
            Log.Info($"Unregistering services ..", "Discord Bridge");

            RemoveService<PunishmentsService>();
            RemoveService<RoleSyncService>();
            RemoveService<NetworkService>();

            Log.Info($"Services unregistered.", "Discord Bridge");
            Log.Info($"Unloaded!", "Discord Bridge");
        }
    }
}