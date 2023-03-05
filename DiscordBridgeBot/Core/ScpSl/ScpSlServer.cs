using AzyWorks.System.Services;

using DiscordBridge.CustomNetwork;
using DiscordBridgeBot.Core.Configuration;
using DiscordBridgeBot.Core.DiscordBot;
using DiscordBridgeBot.Core.Extensions;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.Network;
using DiscordBridgeBot.Core.RoleSync;
using DiscordBridgeBot.Core.RoleSync.Tickets;

namespace DiscordBridgeBot.Core.ScpSl
{
    public class ScpSlServer : ServiceCollection
    {
        private static HashSet<ScpSlServer> _servers = new HashSet<ScpSlServer>();

        public static IReadOnlyCollection<ScpSlServer> AllServers { get => _servers; }

        private LogService _log;

        public int ServerPort { get; private set; }

        public string ServerName { get; private set; }
        public string ServerPath { get => $"{Program.ConfigServersFolder}/{ServerPort}"; }

        public NetworkService Network { get; private set; }
        public DiscordService Discord { get; private set; }
        public RoleSyncService RoleSync { get; private set; }
        public RoleSyncTicketService RoleSyncTickets { get => RoleSync?.Tickets; }
        public ConfigManagerService ConfigManager { get; private set; }

        public void LoadServer(SyncServerInfoMessage syncServerInfoMessage)
        {
            ServerPort = syncServerInfoMessage.Port;
            ServerName = syncServerInfoMessage.Name.RemoveHtmlTags();

            if (!Directory.Exists(ServerPath)) Directory.CreateDirectory(ServerPath);

            AddService<LogService>($"SCP SL ({ServerPort})");
            AddService<ConfigManagerService>($"{ServerPath}/main.ini", null);
            AddService<DiscordService>();
            AddService<RoleSyncService>();

            Network = GetService<NetworkService>();
            Discord = GetService<DiscordService>();
            RoleSync = GetService<RoleSyncService>();
            ConfigManager = GetService<ConfigManagerService>();

            _log = GetService<LogService>();
            _log.Info($"Logger started for server: {ServerName} on port: {ServerPort}");

            ConfigManager.ConfigHandler.RegisterConfigs(this);
            ConfigManager.ReloadAll();

            Discord.Connect();

            _log.Info("Your server is ready!");
        }

        public void UnloadServer()
        {
            _servers.RemoveWhere(x => x == this);
            _log.Info("Your server is unloading ..");

            RemoveService<NetworkService>();
            RemoveService<DiscordService>();
            RemoveService<RoleSyncService>();
            RemoveService<ConfigManagerService>();
            RemoveService<LogService>();

            _log.Info("Server unloaded.");
            _log = null;

            Network = null;
            Discord = null;
            RoleSync = null;
            ConfigManager = null;
        }
    }
}