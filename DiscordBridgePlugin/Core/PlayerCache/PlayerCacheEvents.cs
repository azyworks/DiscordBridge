using PluginAPI.Enums;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;

using AzyWorks.Networking.Client;

using DiscordBridge.CustomNetwork.PlayerCache;
using System;
using System.Collections.Generic;

namespace DiscordBridgePlugin.Core.PlayerCache
{
    public class PlayerCacheEvents
    {
        private static Dictionary<string, Action<PlayerCacheResponseMessage>> _idCallbacks = new Dictionary<string, Action<PlayerCacheResponseMessage>>();
        private static Dictionary<string, Action<PlayerCacheResponseMessage>> _ipCallbacks = new Dictionary<string, Action<PlayerCacheResponseMessage>>();

        [PluginEvent(ServerEventType.PlayerJoined)]
        public void OnJoined(Player player)
        {
            if (player.IsServer)
                return;

            NetClient.Send(new AzyWorks.Networking.NetPayload()
                .WithMessage(new PlayerCacheUpdateMessage(player.UserId, player.Nickname, player.IpAddress)));

            Log.Info($"Sent player cache update ({player.UserId};{player.Nickname};{player.IpAddress})");
        }

        public static void RequestIdCache(string id, Action<PlayerCacheResponseMessage> callback)
        {
            _idCallbacks[id] = callback;
            NetClient.Send(new AzyWorks.Networking.NetPayload()
                .WithMessage(new PlayerCacheRequestMessage(id, false)));
        }

        public static void RequestIpCache(string ip, Action<PlayerCacheResponseMessage> callback)
        {
            _ipCallbacks[ip] = callback;
            NetClient.Send(new AzyWorks.Networking.NetPayload()
                .WithMessage(new PlayerCacheRequestMessage(ip, true)));
        }

        public static void ExecuteCallbacks(PlayerCacheResponseMessage playerCacheResponseMessage)
        {
            if (!playerCacheResponseMessage.IsSuccess)
            {
                _ipCallbacks.Remove(playerCacheResponseMessage.Query);
                _idCallbacks.Remove(playerCacheResponseMessage.Query);
                return;
            }

            if (playerCacheResponseMessage.QueryIpType)
            {
                if (_ipCallbacks.TryGetValue(playerCacheResponseMessage.Ip, out var callback))
                {
                    callback?.Invoke(playerCacheResponseMessage);
                }
            }
            else
            {
                if (_idCallbacks.TryGetValue(playerCacheResponseMessage.Id, out var callback))
                {
                    callback?.Invoke(playerCacheResponseMessage);
                }
            }

            _ipCallbacks.Remove(playerCacheResponseMessage.Query);
            _idCallbacks.Remove(playerCacheResponseMessage.Query);
        }
    }
}