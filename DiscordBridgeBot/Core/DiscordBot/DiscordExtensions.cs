using AzyWorks.System;
using Discord.WebSocket;

namespace DiscordBridgeBot.Core.DiscordBot
{
    public static class DiscordExtensions
    {
        static Dictionary<string, SocketMessage> messageCache = new Dictionary<string, SocketMessage>();

        public const int NextMessageTimeout = 60;

        public static string GetIconUrl(this SocketGuildUser user)
        {
            var guildAvatarUrl = user.GetGuildAvatarUrl();

            if (string.IsNullOrWhiteSpace(guildAvatarUrl))
            {
                var avatarUrl = user.GetAvatarUrl();

                if (string.IsNullOrWhiteSpace(avatarUrl))
                {
                    var defaultAvatarUrl = user.GetDefaultAvatarUrl();

                    if (string.IsNullOrWhiteSpace(defaultAvatarUrl))
                        return null;
                    else
                        return defaultAvatarUrl;
                }
                else
                    return avatarUrl;
            }
            else
                return guildAvatarUrl;
        }

        public static async Task<SocketMessage> GetNextMessageAsync(this ISocketMessageChannel channel, ulong userId, DiscordService discordService)
        {
            var id = RandomGenerator.Ticket(5);
            var curTimeout = 0;

            Task MessageHandler(SocketMessage message)
            {
                if (message.Channel.Id != channel.Id)
                    return Task.CompletedTask;

                if (message.Author.Id != userId)
                    return Task.CompletedTask;

                messageCache[id] = message;

                return Task.CompletedTask;
            }

            discordService.Client.MessageReceived += MessageHandler;

            SocketMessage nextMessage = null;

            while (!messageCache.TryGetValue(id, out nextMessage))
            {
                await Task.Delay(500);

                curTimeout += 500;

                if (curTimeout >= NextMessageTimeout * 1000)
                    break;
            }

            discordService.Client.MessageReceived -= MessageHandler;

            messageCache.Remove(id);

            return nextMessage;
        }
    }
}
