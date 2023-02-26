using AzyWorks.Networking.Server;
using AzyWorks.Services;

using DiscordBridge.CustomNetwork.PluginMessages;
using DiscordBridge.CustomNetwork.ServerMessages;
using DiscordBridgeBot.Core.Configuration;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.ScpSl;

using System.Net;

namespace DiscordBridgeBot.Core.Network
{
    public class NetworkManagerService : ServiceBase
    {
        private bool _ipValidated;

        private LogService _log;
        private IPEndPoint _validatedIp;

        private Dictionary<ulong, NetworkService> _knownClients;

        [Config("Network.Port", "The port to listen on for incoming connections.")]
        public static int ListeningPort = 8888;

        [Config("Network.Ip", "The IP address to listen on for incoming connections.")]
        public static string IpAddress = "127.0.0.1";

        public override void Setup(object[] args)
        {
            _knownClients = new Dictionary<ulong, NetworkService>();
            _log = Collection.CreateService<LogService>("Services::NetworkManager");

            Validate();
        }

        public void Validate()
        {
            _log.Info("Validating IP ..");

            if (string.IsNullOrWhiteSpace(IpAddress))
            {
                _log.Error("IP validation failed - IP cannot be empty, null or white space!");
                _ipValidated = false;
                return;
            }

            if (ListeningPort <= 0 || ListeningPort >= 65536)
            {
                _log.Error($"IP validation failed - the port has to be between 1 and 65 535!");
                _ipValidated = false;
                return;
            }

            if (IpAddress is "127.0.0.1" or "0.0.0.0" or "localhost")
            {
                _log.Info("Listening on loopback IP.");
                _validatedIp = new IPEndPoint(IPAddress.Loopback, ListeningPort);
                _ipValidated = true;
            }
            else
            {
                if (!IPAddress.TryParse(IpAddress, out var ip))
                {
                    _log.Info("IP validation failed - cannot parse a valid IP!");
                    _ipValidated = false;
                    return;
                }

                _ipValidated = true;
                _validatedIp = new IPEndPoint(ip, ListeningPort);

                _log.Info($"Listening on {_validatedIp}.");
            }
        }

        public void StartListening()
        {
            if (!_ipValidated || _validatedIp is null)
            {
                _log.Error("Cannot start listening - IP validation failed!");
                return;
            }

            NetServer.Config.Port = ListeningPort;
            NetServer.Config.ListeningEndPoint = _validatedIp;

            NetServer.OnConnected += OnClientConnected;
            NetServer.OnDisconnected += OnClientDisconnected;

            NetServer.Start();

            _log.Info("Waiting for incoming connections ..");
        }

        public void StopListening()
        {
            NetServer.Stop();

            NetServer.OnConnected -= OnClientConnected;
            NetServer.OnDisconnected -= OnClientDisconnected;

            _log.Info("Stopped listening for incoming connections!");
        }

        private void OnClientDisconnected(NetConnection client)
        {
            if (_knownClients.TryGetValue(client.Id, out var service))
            {
                if (service.Collection is ScpSlServer server)
                    server.UnloadServer();
                else
                    _log.Error($"Parent collection of {service.Id} is not an ScpSlServer, but a {service.Collection}!");
            }

            _knownClients.Remove(client.Id);
            _log.Info($"Succesfully disconnected server {client.EndPoint}!");
        }

        private void OnClientConnected(NetConnection client)
        {
            client.AddTemporaryCallback<SyncServerInfoMessage>(x =>
            {
                _log.Info($"Received the SyncServerInfoMessage for client {client.EndPoint}! Setting up the server ..");

                var server = new ScpSlServer();

                server.AddService<NetworkService>(client);
                server.LoadServer(x);

                _log.Info("Server setup completed.");
            });

            client.Send(new AzyWorks.Networking.NetPayload()
                .WithMessage(new RequestServerInfoMessage()));

            _log.Info($"{client.EndPoint} is attempting connection! Waiting for a SyncServerInfoMessage ..");
        }
    }
}