namespace DiscordBridgeBot.Core.Punishments
{
    public class PunishmentHistory
    {
        public string HistoryOwnerId { get; set; } = "";
        public string HistoryOwnerName { get; set; } = "";
        public string HistoryId { get; set; } = "";

        public HashSet<PunishmentItem> Punishments { get; set; } = new HashSet<PunishmentItem>();

        public bool TryGetItem(string id, out PunishmentItem item)
        {
            item = Punishments.FirstOrDefault(x => x.Id == id);
            return item != null;
        }

        public void Issue(string id, string issuerId, string issuerName, string reason, DateTime issuedAt, DateTime endsAt)
        {
            Punishments.Add(new PunishmentItem
            {
                EndsAt = endsAt,
                IssuerId = issuerId,
                IssuerName = issuerName,
                Id = id,
                IssuedAt = issuedAt,
                Reason = reason
            });
        }

        public void Purge()
            => Punishments.Clear();
    }
}