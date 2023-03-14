using AzyWorks.Extensions;
using AzyWorks.IO.Binary;

namespace DiscordBridgeBot.Core.ScpSlLogs
{
    public class BanMessageCache
    {
        private HashSet<BanItem> _cachedBans = new HashSet<BanItem>();

        public string Path { get; }

        public BanLogsService BanLogs { get; }

        public BanMessageCache(BanLogsService logsService)
        {
            Path = $"{logsService.Punishments.Server.ServerPath}/ban_cache";
            BanLogs = logsService;

            Load();
        }

        public void Remove(ulong messageId)
        {
            if (_cachedBans.RemoveWhere(x => x.MessageId == messageId) > 0)
            {
                Save();
            }
        }

        public bool TryRetrieve(ulong messageId, out BanItem item)
        {
            item = _cachedBans.FirstOrDefault(x => x.MessageId == messageId);
            return item != null;
        }

        public bool TryRetrieveWithIssuer(string issuerValue, out BanItem item)
        {
            item = _cachedBans.FirstOrDefault(x => x.IssuerId == issuerValue);
            return item != null;
        }

        public bool TryRetrieveWithBanned(string bannedValue, out BanItem item)
        {
            item = _cachedBans.FirstOrDefault(x => x.TargetId == bannedValue || x.TargetIp == bannedValue);
            return item != null;
        }

        public void Enqueue(BanItem item)
        {
            _cachedBans.Add(item);
            Save();
        }

        public void Load()
        {
            if (!File.Exists(Path))
            {
                Save();
                return;
            }

            _cachedBans.Clear();

            var cache = new BinaryFile(Path);

            cache.ReadFile();

            _cachedBans.Clear();
            _cachedBans.AddRange(cache.GetData<HashSet<BanItem>>("cache"));
        }

        public void Save()
        {
            var cache = new BinaryFile(Path);

            cache.WriteData("cache", _cachedBans);
            cache.WriteFile();
        }
    }
}
