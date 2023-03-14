using AzyWorks.Networking;
using AzyWorks.Networking.Client;
using AzyWorks.System.Services;

using DiscordBridge.CustomNetwork.Whitelists;
using DiscordBridgePlugin.Core.Extensions;

using MEC;

using PluginAPI.Core;
using PluginAPI.Events;
using PluginAPI.Helpers;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DiscordBridgePlugin.Core.Whitelists
{
    public class WhitelistsService : IService
    {
        public IServiceCollection Collection { get; set; }

        public bool IsActive { get; set; }

        public HashSet<string> Whitelisted { get; set; } = new HashSet<string>();

        public bool IsValid()
        {
            return true;
        }

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
            Load();

            EventManager.RegisterEvents<WhitelistEvents>(Collection);

            NetClient.OnPayloadReceived += OnPayload;

            Sync();
        }

        public void Stop()
        {
            NetClient.OnPayloadReceived -= OnPayload;

            Save();

            EventManager.UnregisterEvents<WhitelistEvents>(Collection);
        }

        private void OnPayload(NetPayload payload)
        {
            if (payload.Messages.Any(x => x is RequestWhitelistStateMessage))
            {
                Sync();
                return;
            }

            if (payload.Messages.Any(x => x is WhitelistModifyMessage))
            {
                var msg = (WhitelistModifyMessage)payload.Messages.First(x => x is WhitelistModifyMessage);

                if (Whitelisted.Contains(msg.UserId))
                    Whitelisted.Remove(msg.UserId);
                else
                    Whitelisted.Add(msg.UserId);

                Save();
                return;
            }

            if (payload.Messages.Any(x =>x is WhitelistModifyStateMessage))
            {
                var msg = (WhitelistModifyStateMessage)payload.Messages.First(x => x is WhitelistModifyStateMessage);

                IsActive = msg.NewState;

                Save();
                return;
            }
        }

        public void Sync()
        {
            Timing.CallDelayed(3f, () =>
            {
                NetClient.Send(new NetPayload()
                    .WithMessage(new WhitelistStateResponseMessage(IsActive, Whitelisted)));

                Log.Info($"Synchronized whitelist state with the server.");
            });
        }

        public void Load()
        {
            if (!File.Exists($"{Paths.AppData}/whitelist_{Server.Port}"))
            {
                Save();
                return;
            }

            Whitelisted.Clear();

            var lines = File.ReadAllLines($"{Paths.AppData}/whitelist_{Server.Port}");

            for (int i = 0; i < lines.Length; i++)
            {
                if (i is 0)
                {
                    IsActive = bool.Parse(lines[i]);
                    continue;
                }

                Whitelisted.Add(lines[i]);
            }

            Log.Info($"Loaded {Whitelisted.Count} whitelisted IDs. Active: {IsActive}");
        }

        public void Save()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine(IsActive.ToString());

            foreach (var id in Whitelisted)
            {
                stringBuilder.AppendLine(id);
            }

            File.WriteAllText($"{Paths.AppData}/whitelist_{Server.Port}", stringBuilder.ToString());

            Log.Info($"Saved {Whitelisted.Count} whitelisted IDs. Active: {IsActive}");

            if (!IsActive)
                return;

            foreach (var player in Player.GetPlayers())
            {
                if (!Whitelisted.Contains(player.UserId) 
                    && !LoaderService.Config.WhitelistIgnored.Contains(player.UserId) 
                    && !LoaderService.Config.WhitelistIgnored.Contains(player.ReferenceHub.GetRole()))
                {
                    player.Kick(LoaderService.Config.WhitelistKickMessage);

                    Log.Info($"Kicked {player.Nickname}: not on the whitelist.");
                }
            }
        }
    }
}