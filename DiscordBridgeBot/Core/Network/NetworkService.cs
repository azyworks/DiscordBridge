using AzyWorks.Networking.Server;
using AzyWorks.Services;

namespace DiscordBridgeBot.Core.Network
{
    public class NetworkService : ServiceBase
    {
        public ulong Id { get; private set; }

        public NetConnection Client { get; private set; }

        public override void Setup(object[] args)
        {
            Client = (NetConnection)args[0];
            Id = Client.Id;
        }
    }
}