using AzyWorks.Networking.Server;
using AzyWorks.System.Services;

namespace DiscordBridgeBot.Core.Network
{
    public class NetworkService : IService
    {
        public ulong Id { get; private set; }

        public NetConnection Client { get; private set; }
        public IServiceCollection Collection { get; set; }

        public bool IsValid()
        {
            return true;
        }

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
            Client = (NetConnection)initArgs[0];
            Id = Client.Id;
        }

        public void Stop()
        {
            Client = null;
        }
    }
}