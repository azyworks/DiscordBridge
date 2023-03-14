using AzyWorks.Extensions;
using AzyWorks.System.Services;

using DiscordBridge.CustomNetwork.Whitelists;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.ScpSl;

namespace DiscordBridgeBot.Core.Whitelists
{
    public class WhitelistService : IService
    {
        private bool _isActive = false;

        public IServiceCollection Collection { get; set; }

        public ScpSlServer Server { get; private set; }

        public LogService Log { get; private set; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;

                    Server.Network.Client.Send(new AzyWorks.Networking.NetPayload()
                        .WithMessage(new WhitelistModifyStateMessage(value)));
                }
            }
        }

        public HashSet<string> WhitelistedIds { get; set; } = new HashSet<string>();

        public bool IsValid()
        {
            return true;
        }

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
            Server = Collection as ScpSlServer;
            Log = Collection.GetService<LogService>();

            try
            {
                Server.Network.Client.AddCallback<WhitelistStateResponseMessage>(OnWhitelistStateReceived);
                Server.Network.Client.Send(new AzyWorks.Networking.NetPayload()
                    .WithMessage(new RequestWhitelistStateMessage()));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void Stop()
        {
            WhitelistedIds.Clear();

            Server = null;
        }

        public bool IsWhitelisted(string id)
            => WhitelistedIds.Contains(id);

        public void Add(string id)
        {
            if (WhitelistedIds.Add(id))
            {
                Server.Network.Client.Send(new AzyWorks.Networking.NetPayload()
                    .WithMessage(new WhitelistModifyMessage(id)));

                Task.Run(async () =>
                {
                    await Task.Delay(1500);

                    ReSync();
                });
            }
        }

        public void Remove(string id)
        {
            if (WhitelistedIds.Remove(id))
            {
                Server.Network.Client.Send(new AzyWorks.Networking.NetPayload()
                    .WithMessage(new WhitelistModifyMessage(id)));

                Task.Run(async () =>
                {
                    await Task.Delay(1500);

                    ReSync();
                });
            }
        }

        public void ReSync()
        {
            Server.Network.Client.Send(new AzyWorks.Networking.NetPayload()
                .WithMessage(new RequestWhitelistStateMessage()));
        }

        private void OnWhitelistStateReceived(WhitelistStateResponseMessage whitelistStateResponseMessage)
        {
            _isActive = whitelistStateResponseMessage.IsActive;

            WhitelistedIds.Clear();
            WhitelistedIds.AddRange(whitelistStateResponseMessage.WhitelistedIds);
        }
    }
}