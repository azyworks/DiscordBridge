using PluginAPI.Core;

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace DiscordBridgePlugin
{
    public class ConfigService
    {
        [Description("The network port to connect to.")]
        public int NetworkPort { get; set; } = 8888;

        [Description("The path to the global role sync database.")]
        public string RoleSyncDatabasePath { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}/db_rolesync_{Server.Port}";

        [Description("A list of role keys / user IDs that bypass the whitelist.")]
        public List<string> WhitelistIgnored { get; set; } = new List<string>()
        {
            "rp"
        };

        [Description("The message to display when a user is kicked by whitelists.")]
        public string WhitelistKickMessage { get; set; } = "You are not whitelisted on this server.";
    }
}