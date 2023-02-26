using AzyWorks.Services;

using PluginAPI.Core.Attributes;
using PluginAPI.Enums;

namespace DiscordBridgePlugin
{
    public class Loader : ServiceCollectionBase
    {
        public Config Config;

        [PluginEntryPoint("DiscordBridge", "1.0.0", "Bridges your SCP:SL server and Discord together.", "azyworks")]
        public void Load()
        {
            
        }
    }
}