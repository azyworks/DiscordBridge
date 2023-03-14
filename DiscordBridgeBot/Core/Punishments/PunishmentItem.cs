namespace DiscordBridgeBot.Core.Punishments
{
    public class PunishmentItem
    {
        public string Id { get; set; } = "";

        public string IssuerId { get; set; } = "";
        public string IssuerName { get; set; } = "";

        public string Reason { get; set; } = "";

        public DateTime IssuedAt { get; set; } = DateTime.MinValue;
        public DateTime EndsAt { get; set; } = DateTime.MinValue;
    }
}