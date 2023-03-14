using AzyWorks.Networking.Server;
using AzyWorks.System.Services;

using DiscordBridge.CustomNetwork;
using DiscordBridgeBot.Core.Configuration;
using DiscordBridgeBot.Core.DiscordBot;
using DiscordBridgeBot.Core.Extensions;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.Network;
using DiscordBridgeBot.Core.PlayerCache;
using DiscordBridgeBot.Core.Punishments;
using DiscordBridgeBot.Core.RoleSync;
using DiscordBridgeBot.Core.RoleSync.Tickets;
using DiscordBridgeBot.Core.ScpSlLogs;
using DiscordBridgeBot.Core.Whitelists;

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

        public NetworkService Network { get; set; }
        public DiscordService Discord { get; private set; }
        public RoleSyncService RoleSync { get; private set; }
        public RoleSyncTicketService RoleSyncTickets { get => RoleSync?.Tickets; }
        public ConfigManagerService ConfigManager { get; private set; }
        public NetConnection Connection { get; set; }

        public LogService Log { get => _log; }

        public void LoadServer(SyncServerInfoMessage syncServerInfoMessage, NetworkService networkService, NetConnection connection)
        {
            try
            {
                ServerPort = syncServerInfoMessage.Port;
                ServerName = syncServerInfoMessage.Name.RemoveHtmlTags();

                if (!Directory.Exists(ServerPath)) Directory.CreateDirectory(ServerPath);

                AddService<LogService>($"SCP SL ({ServerPort})");

                _log = GetService<LogService>();

                AddService<ConfigManagerService>($"{ServerPath}/main.ini", null);
                AddService<DiscordService>();
                AddService<RoleSyncService>();
                AddService<PlayerCacheService>();
                AddService<WhitelistService>();
                AddService<PunishmentsService>();
                AddService<BanLogsService>();

                Discord = GetService<DiscordService>();
                RoleSync = GetService<RoleSyncService>();
                ConfigManager = GetService<ConfigManagerService>();

                ConfigManager.ConfigHandler.RegisterConfigs(this);
                ConfigManager.ReloadAll();

                Discord.Connect();

                _log.Info("Your server is ready!");
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }

        public void UnloadServer()
        {
            try
            {
                _servers.RemoveWhere(x => x.Connection.Id == Connection.Id);
                _log.Info("Your server is unloading ..");

                RemoveService<DiscordService>();
                RemoveService<PunishmentsService>();
                RemoveService<BanLogsService>();
                RemoveService<WhitelistService>();
                RemoveService<PlayerCacheService>();
                RemoveService<RoleSyncService>();
                RemoveService<NetworkService>();
                RemoveService<ConfigManagerService>();
                RemoveService<LogService>();

                _log.Info("Server unloaded.");

                Network = null;
                Discord = null;
                Connection = null;
                RoleSync = null;
                ConfigManager = null;
            }
            catch (Exception ex)
            {
                _log.Error(ex);
            }

            _log = null;
        }
    }
}