using AzyWorks.Networking.Client;
using AzyWorks.Networking;
using AzyWorks.System.Services;

using DiscordBridge.CustomNetwork;

using PluginAPI.Core;

using GameCore;

using Log = PluginAPI.Core.Log;

namespace DiscordBridgePlugin.Core.Network
{
    public class NetworkService : IService
    {
        public IServiceCollection Collection { get; set; }

        public bool IsValid()
        {
            return true;
        }

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
            NetClient.Config.Port = (int)initArgs[0];
            NetClient.Config.ServerEndpoint.Port = (int)initArgs[0];

            NetClient.OnConnected += () => Log.Info($"Connected!", "Discord Bridge :: NetworkService");
            NetClient.OnDisconnected += () => Log.Info($"Disconnected!", "Discord Bridge :: NetworkService");
            NetClient.OnDisposed += () => Log.Info($"Disposed.", "Discord Bridge :: NetworkService");
            NetClient.OnStarted += () => Log.Info($"Client started.", "Discord Bridge :: NetworkService");
            NetClient.OnStopped += () => Log.Info($"Client stopped.", "Discord Bridge :: NetworkService");

            Log.Info($"Connecting to: {NetClient.Config.ServerEndpoint}", "Discord Bridge :: NetworkService");

            NetClient.Start();
            NetClient.AddCallback<RequestServerInfoMessage>(OnInfoRequested);
        }

        public void Stop()
        {
            NetClient.Stop();

            Log.Info($"Stopped.", "Discord Bridge :: NetworkService");
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