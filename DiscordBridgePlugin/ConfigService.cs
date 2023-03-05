using PluginAPI.Core;
using System;
using System.ComponentModel;

namespace DiscordBridgePlugin
{
    public class ConfigService
    {
        [Description("The network port to connect to.")]
        public int NetworkPort { get; set; } = 8888;

        [Description("The path to the global role sync database.")]
        public string RoleSyncDatabasePath { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/db_rolesync_{Server.Port}";
    }
}