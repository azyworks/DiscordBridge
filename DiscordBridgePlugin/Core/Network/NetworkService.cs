using AzyWorks.Networking.Client;
using AzyWorks.Networking;
using AzyWorks.System.Services;

using DiscordBridge.CustomNetwork;

using PluginAPI.Core;

using GameCore;

using Log = PluginAPI.Core.Log;

using MEC;

using System.Linq;
using System;

using DiscordBridgePlugin.Core.Whitelists;
using DiscordBridgePlugin.Core.RoleSync;
using DiscordBridge.CustomNetwork.RemoteAdmin;
using RemoteAdmin;
using DiscordBridge.CustomNetwork.Punishments;
using PluginAPI.Enums;
using PluginAPI.Events;
using DiscordBridge.CustomNetwork.PlayerCache;
using DiscordBridgePlugin.Core.PlayerCache;

namespace DiscordBridgePlugin.Core.Network
{
    public class NetworkService : IService
    {
        private bool _synchronizedInfo;

        public IServiceCollection Collection { get; set; }

        public bool IsValid()
        {
            return true;
        }

        public void Reconnect()
        {
            Log.Info("Reconnecting ..");

            Stop();

            Timing.CallDelayed(5f, () => Start(Collection, LoaderService.Config.NetworkPort));
        }

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
            Log.Info($"Starting on port {initArgs[0]} ..");

            NetClient.Config.Port = (int)initArgs[0];
            NetClient.Config.ServerEndpoint.Port = (int)initArgs[0];

            NetClient.OnConnected += OnConnected;
            NetClient.OnDisconnected += OnDisconnected;
            NetClient.OnDisposed += OnDisposed;
            NetClient.OnStarted += OnStarted;
            NetClient.OnStopped += OnStopped;
            NetClient.OnPayloadReceived += OnPayload;

            Log.Info($"Connecting to: {NetClient.Config.ServerEndpoint}", "Discord Bridge :: NetworkService");

            NetClient.Start();
        }

        public void Stop()
        {
            Collection.RemoveService<WhitelistsService>();
            Collection.RemoveService<RoleSyncService>();

            NetClient.OnConnected -= OnConnected;
            NetClient.OnDisconnected -= OnDisconnected;
            NetClient.OnDisposed -= OnDisposed;
            NetClient.OnStarted -= OnStarted;
            NetClient.OnStopped -= OnStopped;
            NetClient.OnPayloadReceived -= OnPayload;

            NetClient.Stop();
            NetClient.Dispose();

            Log.Info($"Stopped.", "Discord Bridge :: NetworkService");
        }

        private void OnPayload(NetPayload payload)
        {
            if (payload.Messages.Any(x => x is RequestServerInfoMessage))
            {
                try
                {
                    Log.Info("Received the RequestServerInfoMessage.");

                    ConfigFile.ReloadGameConfigs();

                    NetClient.Send(new NetPayload()
                        .WithMessage(new SyncServerInfoMessage(
                            ConfigFile.ServerConfig.GetString("server_name", "My Server Name"),
                            Server.Port)));

                    Log.Info("Sent the SyncServerInfoMessage");

                    if (!Collection.HasService<RoleSyncService>())
                        Collection.AddService<RoleSyncService>();

                    if (!Collection.HasService<WhitelistsService>())
                        Collection.AddService<WhitelistsService>();
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }

            if (payload.Messages.Any(x => x is RemoteAdminExecuteMessage))
            {
                var msg = (RemoteAdminExecuteMessage)payload.Messages.First(x => x is RemoteAdminExecuteMessage);
                var sender = new FakeCommandSender(msg.SenderName, msg.SenderId);

                Server.RunCommand(msg.Command, sender);
            }

            if (payload.Messages.Any(x => x is PunishmentRemoveMessage))
            {
                var msg = (PunishmentRemoveMessage)payload.Messages.First(x => x is PunishmentRemoveMessage);

                BanHandler.RemoveBan(msg.TargetId, BanHandler.BanType.UserId, true);
                BanHandler.RemoveBan(msg.TargetIp, BanHandler.BanType.IP, true);
            }

            if (payload.Messages.Any(x => x is PunishmentEditDurationMessage))
            {
                var msg = (PunishmentEditDurationMessage)payload.Messages.First(x => x is PunishmentEditDurationMessage);

                var newTicks = (DateTime.Now.ToLocalTime() + msg.Duration).Ticks;
                var idBan = BanHandler.GetBan(msg.TargetId, BanHandler.BanType.UserId);
                var ipBan = BanHandler.GetBan(msg.TargetIp, BanHandler.BanType.IP);

                if (idBan != null)
                {
                    idBan.Expires = newTicks;

                    RemoveBanNoEvent(idBan.Id, BanHandler.BanType.UserId);
                    IssueBanNoEvent(idBan, BanHandler.BanType.UserId);
                }

                if (ipBan != null)
                {
                    ipBan.Expires = newTicks;

                    RemoveBanNoEvent(ipBan.Id, BanHandler.BanType.IP);
                    IssueBanNoEvent(ipBan, BanHandler.BanType.IP);
                }
            }


            if (payload.Messages.Any(x => x is PunishmentEditReasonMessage))
            {
                var msg = (PunishmentEditReasonMessage)payload.Messages.First(x => x is PunishmentEditReasonMessage);
                var idBan = BanHandler.GetBan(msg.TargetId, BanHandler.BanType.UserId);
                var ipBan = BanHandler.GetBan(msg.TargetIp, BanHandler.BanType.IP);

                if (idBan != null)
                {
                    idBan.Reason = msg.Reason;

                    RemoveBanNoEvent(idBan.Id, BanHandler.BanType.UserId);
                    IssueBanNoEvent(idBan, BanHandler.BanType.UserId);
                }

                if (ipBan != null)
                {
                    ipBan.Reason = msg.Reason;

                    RemoveBanNoEvent(ipBan.Id, BanHandler.BanType.IP);
                    IssueBanNoEvent(ipBan, BanHandler.BanType.IP);
                }
            }

            if (payload.Messages.Any(x => x is PlayerCacheResponseMessage))
            {
                var msg = (PlayerCacheResponseMessage)payload.Messages.First(x => x is PlayerCacheResponseMessage);

                PlayerCacheEvents.ExecuteCallbacks(msg);
            }
        }

        public void RemoveBanNoEvent(string id, BanHandler.BanType banType)
        {
            id = id.Replace(";", ":").Replace(Environment.NewLine, "").Replace("\n", "");
            FileManager.WriteToFile((from l in FileManager.ReadAllLines(BanHandler.GetPath(banType))
                                     where BanHandler.ProcessBanItem(l, banType) != null && BanHandler.ProcessBanItem(l, banType).Id != id
                                     select l).ToArray<string>(), BanHandler.GetPath(banType), false);
        }

        public void IssueBanNoEvent(BanDetails ban, BanHandler.BanType banType)
        {
            try
            {
                if (banType == BanHandler.BanType.IP && ban.Id.Equals("localClient", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
                else
                {
                    ban.OriginalName = ban.OriginalName.Replace(";", ":");
                    ban.Issuer = ban.Issuer.Replace(";", ":");
                    ban.Reason = ban.Reason.Replace(";", ":");

                    Misc.ReplaceUnsafeCharacters(ref ban.OriginalName, '?');
                    Misc.ReplaceUnsafeCharacters(ref ban.Issuer, '?');

                    if (!BanHandler.GetBans(banType).Any((BanDetails b) => b.Id == ban.Id))
                    {
                        FileManager.AppendFile(ban.ToString(), BanHandler.GetPath(banType), true);
                        FileManager.RemoveEmptyLines(BanHandler.GetPath(banType));
                    }
                    else
                    {
                        BanHandler.RemoveBan(ban.Id, banType, true);
                        BanHandler.IssueBan(ban, banType, true);
                    }
                }
            }
            catch
            {

            }
        }

        private void OnConnected()
        {
            Log.Info($"Connected!", "Discord Bridge :: NetworkService");
        }

        private void OnDisconnected()
        {
            Log.Info($"Disconnected!", "Discord Bridge :: NetworkService");
        }

        private void OnDisposed()
        {
            Log.Info($"Disposed.", "Discord Bridge :: NetworkService");
        }

        private void OnStarted()
        {
            Log.Info($"Client started.", "Discord Bridge :: NetworkService");
        }

        private void OnStopped()
        {
            Log.Info($"Client stopped.", "Discord Bridge :: NetworkService");
        }
    }
}