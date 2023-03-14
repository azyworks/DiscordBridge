using AzyWorks.Extensions;
using AzyWorks.IO.Binary;
using AzyWorks.System;
using AzyWorks.System.Services;

using Discord;
using Discord.WebSocket;
using DiscordBridge.CustomNetwork.Punishments;
using DiscordBridge.CustomNetwork.Reports;
using DiscordBridgeBot.Core.DiscordBot;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.ScpSl;
using DiscordBridgeBot.Core.ScpSlLogs;

namespace DiscordBridgeBot.Core.Punishments
{
    public class PunishmentsService : IService
    {
        public IServiceCollection Collection { get; set; }

        public HashSet<PunishmentHistory> History { get; set; }

        public ScpSlServer Server { get; private set; }
        public DiscordService Discord { get; private set; }

        public LogService Log { get; private set; }

        public Dictionary<SocketTextChannel, ulong[]> ReportChannels { get; private set; } = new Dictionary<SocketTextChannel, ulong[]>();

        public event Action<PunishmentHistory, PunishmentIssuedMessage> OnPunishmentIssued;

        public bool IsValid()
        {
            return true;
        }

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
            Server = Collection as ScpSlServer;
            Discord = Collection.GetService<DiscordService>();

            Log = Collection.GetService<LogService>();

            Load();

            Server.Network.Client.AddCallback<PunishmentIssuedMessage>(OnPunishmentIssuedMessageHandler);
            Server.Network.Client.AddCallback<ReportMessage>(OnReportReceived);

            if (Discord.IsReady)
                OnDiscordReady(Discord.User, Discord.Guild);

            Discord.OnReady += OnDiscordReady;
        }

        public void OnDiscordReady(SocketGuildUser user, SocketGuild guild)
        {
            foreach (var channel in Discord.ReportChannels)
            {
                var textChannel = guild.GetTextChannel(channel.Key);
                if (textChannel is null)
                    continue;

                ReportChannels[textChannel] = channel.Value;
            }
        }

        public void OnReportReceived(ReportMessage reportMessage)
        {
            foreach (var channel in ReportChannels)
            {
                string mentions = string.Join(" ", channel.Value.Select(x => Discord.GetMention(x)));
                if (string.IsNullOrWhiteSpace(mentions))
                    mentions = null;

                Task.Run(async () =>
                {
                    if (!reportMessage.IsCheaterReport)
                    {
                        await channel.Key.SendMessageAsync(mentions, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithTitle("⚠️ Player Report!")
                            .WithColor(Color.Orange)
                            .WithFields(new EmbedFieldBuilder[]
                            {
                            new EmbedFieldBuilder()
                                .WithName("🔗 Reporter")
                                .WithValue(
                                $"**Player ID**: {reportMessage.ReporterPlayerId}\n" +
                                $"**Jméno**: {reportMessage.ReporterName}\n" +
                                $"**ID**: {reportMessage.ReporterId}\n" +
                                $"**IP**: {reportMessage.ReporterIp}\n" +
                                $"**Role**: {reportMessage.ReporterRole}\n" +
                                $"**Místnost**: {reportMessage.ReporterRoom}"),

                            new EmbedFieldBuilder()
                                .WithName("🔗 Reported")
                                .WithValue(
                                $"**Player ID**: {reportMessage.ReportedPlayerId}\n" +
                                $"**Jméno**: {reportMessage.ReportedName}\n" +
                                $"**ID**: {reportMessage.ReportedId}\n" +
                                $"**IP**: {reportMessage.ReportedIp}\n" +
                                $"**Role**: {reportMessage.ReportedRole}\n" +
                                $"**Místnost**: {reportMessage.ReportedRoom}"),

                            new EmbedFieldBuilder()
                                .WithName("❔ Důvod")
                                .WithValue($"```{reportMessage.Reason}```")
                            })
                            .Build());
                    }
                    else
                    {
                        await channel.Key.SendMessageAsync(mentions, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithTitle("🚫 Cheater Report!")
                            .WithColor(Color.Red)
                            .WithFields(new EmbedFieldBuilder[]
                            {
                            new EmbedFieldBuilder()
                                .WithIsInline(true)
                                .WithName("🔗 Reporter")
                                .WithValue(
                                $"**Player ID**: {reportMessage.ReporterPlayerId}\n" +
                                $"**Jméno**: {reportMessage.ReporterName}\n" +
                                $"**ID**: {reportMessage.ReporterId}\n" +
                                $"**IP**: {reportMessage.ReporterIp}\n" +
                                $"**Role**: {reportMessage.ReporterRole}\n" +
                                $"**Místnost**: {reportMessage.ReporterRoom}"),

                            new EmbedFieldBuilder()
                                .WithIsInline(true)
                                .WithName("🔗 Reported")
                                .WithValue(
                                $"**Player ID**: {reportMessage.ReportedPlayerId}\n" +
                                $"**Jméno**: {reportMessage.ReportedName}\n" +
                                $"**ID**: {reportMessage.ReportedId}\n" +
                                $"**IP**: {reportMessage.ReportedIp}\n" +
                                $"**Role**: {reportMessage.ReportedRole}\n" +
                                $"**Místnost**: {reportMessage.ReportedRoom}"),

                            new EmbedFieldBuilder()
                                .WithIsInline(true)
                                .WithName("❔ Důvod")
                                .WithValue($"```{reportMessage.Reason}```")
                            })
                            .WithFooter(new EmbedFooterBuilder()
                                .WithText($"⚠️ Tento report byl odeslán Northwood moderátorům."))
                            .Build());
                    }
                });
            }
        }

        public PunishmentHistory GetOrCreateHistory(string userId, string userName = "")
        {
            if (!TryGetHistory(userId, out var history))
            {
                history = new PunishmentHistory
                {
                    HistoryId = RandomGenerator.Ticket(15),

                    HistoryOwnerId = userId,
                    HistoryOwnerName = userName,

                    Punishments = new HashSet<PunishmentItem>()
                };

                History.Add(history);
            }

            return history;
        }

        public bool TryGetHistory(string userId, out PunishmentHistory history)
        {
            history = History.FirstOrDefault(x => x.HistoryOwnerId == userId);
            return history != null;
        }

        private void OnPunishmentIssuedMessageHandler(PunishmentIssuedMessage punishmentIssuedMessage)
        {
            var history = GetOrCreateHistory(punishmentIssuedMessage.Id, punishmentIssuedMessage.Name);

            history.Issue(
                RandomGenerator.Ticket(4),

                punishmentIssuedMessage.IssuerId,
                punishmentIssuedMessage.IssuerName,

                punishmentIssuedMessage.Reason,

                punishmentIssuedMessage.IssuedAt,
                punishmentIssuedMessage.EndsAt);

            OnPunishmentIssued?.Invoke(history, punishmentIssuedMessage);

            Save();
        }

        public void Stop()
        {
            Save();

            ReportChannels.Clear();

            Discord.OnReady -= OnDiscordReady;

            Server.RemoveService<BanLogsService>();
            Server = null;
            Discord = null;
        }

        public void Load()
        {
            if (!File.Exists($"{Server.ServerPath}/punishments"))
            {
                Save();
                return;
            }

            if (History is null)
                History = new HashSet<PunishmentHistory>();

            History.Clear();

            var cache = new BinaryFile($"{Server.ServerPath}/punishments");

            cache.ReadFile();

            History.AddRange(cache.GetData<HashSet<PunishmentHistory>>("history"));
        }

        public void Save()
        {
            if (History is null)
                History = new HashSet<PunishmentHistory>();

            History.Clear();

            var cache = new BinaryFile($"{Server.ServerPath}/punishments");

            cache.WriteData("history", History);
            cache.WriteFile();
        }
    }
}