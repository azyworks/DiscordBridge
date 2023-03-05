using AzyWorks.Networking.Client;
using AzyWorks.Networking;
using AzyWorks.System.Services;

using DiscordBridge.CustomNetwork;

using PluginAPI.Core;

using GameCore;

using Log = PluginAPI.Core.Log;

using AzyWorks.Logging;
using MEC;
using System.Linq;

namespace DiscordBridgePlugin.Core.Network
{
    public class NetworkService : IService
    {
        public IServiceCollection Collection { get; set; }

        public bool IsValid()
        {
            return true;
        }

        public void Reconnect()
        {
            Stop();

            Timing.CallDelayed(5f, () => Start(Collection, LoaderService.Config.NetworkPort));
        }

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
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
                NetClient.Send(new NetPayload()
                    .WithMessage(new SyncServerInfoMessage(
                        ConfigFile.ServerConfig.GetString("server_name", "My Server Name"),
                        Server.Port)));
        }

        private void OnConnected()
        {
            Log.Info($"Connected!", "Discord Bridge :: NetworkService");

            NetClient.AddCallback<RequestServerInfoMessage>(OnInfoRequested);
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

        private void OnInfoRequested(RequestServerInfoMessage message)
        {
            ConfigFile.OnConfigReloaded = () =>
            {
                NetClient.Send(new NetPayload()
                    .WithMessage(new SyncServerInfoMessage(
                        ConfigFile.ServerConfig.GetString("server_name", "My Server Name"),
                        Server.Port)));
            };

            ConfigFile.ReloadGameConfigs(true);
        }
    }
}