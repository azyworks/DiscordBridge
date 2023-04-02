using AzyWorks;

using DiscordBridgeBot.Core.Extensions;

namespace DiscordBridgeBot.Core.ScpSlServerListApi
{
    public class ScpSlServerListApiResponse
    {
        private List<ScpSlServerListApiItem> _servers;

        public ScpSlServerListApiResponse(IEnumerable<ScpSlServerListApiItem> servers)
        {
            _servers = servers.ToList();

            var totalPlayers = 0;

            foreach (var item in servers)
            {
                item.PastebinUrl = $"https://pastebin.com/{item.PastebinId}";

                if (ScpSlServerListApiService.TryGetPlayers(item.PlayersString, out var players, out var maxPlayers))
                {
                    item.Players = players;
                    item.MaxPlayers = maxPlayers;

                    totalPlayers += players;
                }
                else
                {
                    item.Players = 0;
                    item.MaxPlayers = 0;

                    Log.SendError("ScpSlServerListApiResponse", $"Failed to retrieve player count from {item.PlayersString}");
                }

                if (ScpSlServerListApiService.TryGetOfficialType(item.OfficialTypeString, out var scpSlServerListOfficialType))
                {
                    item.OfficialType = scpSlServerListOfficialType;
                }
                else
                {
                    item.OfficialType = ScpSlServerListOfficialType.Unknown;
                    Log.SendError("ScpSlServerListApiResponse", $"Failed to retrieve official type with code {item.OfficialCodeValue} and name {item.OfficialTypeString}");
                }

                if (ScpSlServerListApiService.TryGetName(item.HashedName, out var name))
                {
                    item.FullName = name;
                    item.ClearName = name.RemoveHtmlTags();
                }
                else
                {
                    item.FullName = item.HashedName;
                    item.ClearName = item.HashedName;

                    Log.SendError("ScpSlServerListApiResponse", $"Failed to retrieve server name from hash {item.HashedName}");
                }
            }

            TotalServers = _servers.Count;
            TotalPlayers = totalPlayers;
        }

        public int TotalPlayers { get; }
        public int TotalServers { get; }

        public IReadOnlyList<ScpSlServerListApiItem> Servers { get => _servers; }
    }
}