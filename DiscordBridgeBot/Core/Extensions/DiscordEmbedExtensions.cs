using Discord;

namespace DiscordBridgeBot.Core.Extensions
{
    public static class DiscordEmbedExtensions
    {
        public static EmbedBuilder WithField(this EmbedBuilder builder, string name, object value)
        {
            return builder.AddField(name, value, false);
        }

        public static EmbedBuilder WithInlineField(this EmbedBuilder builder, string name, object value)
        {
            return builder.AddField(name, value, true);
        }
    }
}