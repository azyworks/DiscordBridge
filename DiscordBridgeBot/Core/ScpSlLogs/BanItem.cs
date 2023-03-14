namespace DiscordBridgeBot.Core.ScpSlLogs
{
    public class BanItem
    {
        public ulong MessageId { get; set; }

        public string IssuerName { get; set; }
        public string IssuerId { get; set; }

        public string TargetName { get; set; }
        public string TargetId { get; set; }
        public string TargetIp { get; set; }

        public string Reason { get; set; }

        public DateTime IssuedAt { get; set; }
        public DateTime EndsAt { get; set; }

        public TimeSpan Duration { get; set; }
    }
}