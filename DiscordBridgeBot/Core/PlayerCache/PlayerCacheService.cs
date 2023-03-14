using AzyWorks.Extensions;
using AzyWorks.IO.Binary;
using AzyWorks.System.Services;

using DiscordBridge.CustomNetwork.PlayerCache;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.Network;
using DiscordBridgeBot.Core.ScpSl;

namespace DiscordBridgeBot.Core.PlayerCache
{
    public class PlayerCacheService : IService
    {
        public IServiceCollection Collection { get; set; }

        public ScpSlServer Server { get; private set; }
        public NetworkService Network { get; private set; }
        public LogService Log { get; private set; }

        public HashSet<PlayerCacheItem> Cache { get; } = new HashSet<PlayerCacheItem>();

        public bool IsValid()
        {
            return true;
        }

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
            Server = Collection as ScpSlServer;
            Log = Collection.GetService<LogService>();
            Network = Collection.GetService<NetworkService>();

            Load();

            Server.Network.Client.AddCallback<PlayerCacheUpdateMessage>(OnPlayerCacheUpdateReceived);
            Server.Network.Client.AddCallback<PlayerCacheRequestMessage>(OnPlayerCacheRequestReceived);
        }

        public void Stop()
        {
            Save();

            Server = null;
        }

        public bool TryFetch(string value, out PlayerCacheItem cacheItem)
        {
            cacheItem = null;

            foreach (var item in Cache)
            {
                if (item.UserIp == value)
                {
                    cacheItem = item; 
                    break;
                }

                if (item.LastUserId == value)
                {
                    cacheItem = item;
                    break;
                }

                if (item.AllIDs.Contains(value))
                {
                    cacheItem = item;
                    break;
                }
            }

            if (cacheItem != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnPlayerCacheRequestReceived(PlayerCacheRequestMessage playerCacheRequestMessage)
        {
            if (TryFetch(playerCacheRequestMessage.Target, out var cacheItem))
            {
                Server.Connection.Send(new AzyWorks.Networking.NetPayload()
                    .WithMessage(new PlayerCacheResponseMessage(true, playerCacheRequestMessage.IsIpQuery, playerCacheRequestMessage.Target, cacheItem.LastUserId, cacheItem.UserIp, cacheItem.LastUserName, cacheItem.AllIDs.ToList(), cacheItem.AllNames.ToList())));
            }
            else
            {
                Server.Connection.Send(new AzyWorks.Networking.NetPayload()
                    .WithMessage(new PlayerCacheResponseMessage(false, playerCacheRequestMessage.IsIpQuery, null, null, null, null, null, null)));
            }
        }

        private void OnPlayerCacheUpdateReceived(PlayerCacheUpdateMessage playerCacheUpdateMessage)
        {
            if (TryFetch(playerCacheUpdateMessage.UserIp, out PlayerCacheItem cacheItem))
            {
                if (playerCacheUpdateMessage.UserId != cacheItem.LastUserId)
                    cacheItem.LastUserId = playerCacheUpdateMessage.UserId;

                if (playerCacheUpdateMessage.UserName != cacheItem.LastUserName)
                    cacheItem.LastUserName = playerCacheUpdateMessage.UserName;

                if (!cacheItem.AllNames.Contains(playerCacheUpdateMessage.UserName))
                    cacheItem.AllNames.Add(playerCacheUpdateMessage.UserName);

                if (!cacheItem.AllIDs.Contains(playerCacheUpdateMessage.UserId))
                    cacheItem.AllIDs.Add(playerCacheUpdateMessage.UserId);

                Save();
            }
            else
            {
                cacheItem = new PlayerCacheItem()
                {
                    LastUserId = playerCacheUpdateMessage.UserId,
                    LastUserName = playerCacheUpdateMessage.UserName,
                    UserIp = playerCacheUpdateMessage.UserIp
                };

                cacheItem.AllIDs.Add(cacheItem.LastUserId);
                cacheItem.AllNames.Add(cacheItem.LastUserName);

                Cache.Add(cacheItem);

                Save();
            }
        }

        private void Load()
        {
            if (!File.Exists($"{Server.ServerPath}/cache"))
            {
                Save();
                return;
            }

            Cache.Clear();

            var cache = new BinaryFile($"{Server.ServerPath}/cache");

            cache.ReadFile();

            Cache.AddRange(cache.GetData<HashSet<PlayerCacheItem>>("cache"));
        }

        private void Save()
        {
            var cache = new BinaryFile($"{Server.ServerPath}/cache");

            cache.WriteData("cache", Cache);
            cache.WriteFile();
        }
    }
}