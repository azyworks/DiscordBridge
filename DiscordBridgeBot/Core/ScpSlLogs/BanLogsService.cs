using AzyWorks.System.Services;

using Discord;
using Discord.WebSocket;

using DiscordBridge.CustomNetwork.Punishments;
using DiscordBridge.CustomNetwork.RemoteAdmin;
using DiscordBridgeBot.Core.DiscordBot;
using DiscordBridgeBot.Core.Extensions;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.PlayerCache;
using DiscordBridgeBot.Core.Punishments;

using System.Text.RegularExpressions;

namespace DiscordBridgeBot.Core.ScpSlLogs
{
    public class BanLogsService : IService
    {
        public IServiceCollection Collection { get; set; }

        public DiscordService Discord { get; private set; }
        public PunishmentsService Punishments { get; private set; }
        public LogService Log { get; private set; }
        public BanMessageCache Cache { get; private set; }

        public Dictionary<BanLogChannelType, HashSet<SocketTextChannel>> Channels { get; set; } = new Dictionary<BanLogChannelType, HashSet<SocketTextChannel>>()
        {
            [BanLogChannelType.AdminOnly] = new HashSet<SocketTextChannel>(),
            [BanLogChannelType.Public] = new HashSet<SocketTextChannel>()
        };

        public SocketTextChannel RevokeRequestsChannel { get; set; }

        public event Action<PunishmentIssuedMessage, SocketMessage> OnBanMessageCreated;

        public bool IsValid()
        {
            return true;
        }

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
            Discord = Collection.GetService<DiscordService>();
            Punishments = Collection.GetService<PunishmentsService>();
            Log = Collection.GetService<LogService>();

            if (Discord.IsReady)
                OnDiscordReady(Discord.User, Discord.Guild);

            Discord.OnReady += OnDiscordReady;
            Punishments.OnPunishmentIssued += OnPunishmentIssued;

            Cache = new BanMessageCache(this);
        }

        private void OnDiscordReady(SocketGuildUser user, SocketGuild guild)
        {
            Channels[BanLogChannelType.Public].Clear();
            Channels[BanLogChannelType.AdminOnly].Clear();

            foreach (var channelPair in Discord.ConfigChannelIds)
            {
                foreach (var channelId in channelPair.Value)
                {
                    var channel = Discord.Guild.GetTextChannel(channelId);
                    if (channel != null)
                    {
                        Channels[channelPair.Key].Add(channel);

                        Log.Info($"Found ban log channel: {channel.Name} ({channelPair.Key})");
                    }
                    else
                    {
                        Log.Warn($"Failed to find {channelPair.Key} channel ID {channelId}");
                    }
                }
            }

            RevokeRequestsChannel = Discord.Guild.GetTextChannel(Discord.RevokeRequestsChannelId);

            Discord.Client.ButtonExecuted += OnButtonInteraction;
        }

        private async Task<bool> VerifyBanLogInteraction(SocketMessageComponent component)
        {
            bool canContinue = false;

            foreach (var banChannelList in Channels)
            {
                if (banChannelList.Key is BanLogChannelType.Public)
                    continue;

                foreach (var banChannel in banChannelList.Value)
                {
                    if (banChannel.Id == component.Channel.Id)
                    {
                        canContinue = true;
                        break;
                    }
                }
            }

            if (!canContinue)
                return false;

            if (!Cache.TryRetrieve(component.Message.Id, out var cachedBan))
            {
                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle($"ℹ️ Tento ban nelze upravit - nenalezen v paměti.")
                    .Build() }, false, true);

                return true;
            }

            if (component.Data.CustomId == "ButtonBan_Revoke")
                await RevokeBanAsync(component, cachedBan);
            else if (component.Data.CustomId == "ButtonBan_EditReason")
                await EditBanReasonAsync(component, cachedBan);
            else if (component.Data.CustomId == "ButtonBan_EditDuration")
                await EditBanDurationAsync(component, cachedBan);
            else
                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle($"ℹ️ Neplatné ID interakce: {component.Data.CustomId}")
                    .Build() }, false, true);

            return true;
        }

        private async Task<bool> VerifyManagementButtonInteraction(SocketMessageComponent component)
        {
            if (RevokeRequestsChannel is null)
                return false;

            var channel = await component.GetChannelAsync();
            if (channel.Id != RevokeRequestsChannel.Id)
                return false;

            return true;
        }

        private async Task OnButtonInteraction(SocketMessageComponent component)
        {
            if (await VerifyBanLogInteraction(component))
                return;

            if (await VerifyManagementButtonInteraction(component))
                return;
        }

        private async Task EditBanDurationAsync(SocketMessageComponent component, BanItem cachedBan)
        {
            if ((Punishments.Server.RoleSync.TryGetAccount(component.User.Id, out var account) && account.UserId == cachedBan.IssuerId)
                || (Discord.TryGetMember(component.User, out var member) && Discord.HasPermission(member, DiscordPermission.BanManagement)))
            {
                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle("ℹ️ Do další zprávy napiš novou délku (od aktuálního času).")
                    .Build() }, false, true);
                
                var channel = await component.GetChannelAsync();
                var next = await channel.GetNextMessageAsync(component.User.Id, Discord);
                if (next is null)
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithTitle("ℹ️ Čas vypršel.")
                    .Build() }, false, true);
                    return;
                }

                var duration = next.Content.ToSpan();

                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle($"ℹ️ Nová délka: `{duration.ToReadableString()}`. Další zprávou potvrď správnost (y/yes/ano/jo ..)")
                    .Build() }, false, true);

                next = await channel.GetNextMessageAsync(component.User.Id, Discord);
                if (next is null)
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithTitle("ℹ️ Čas vypršel.")
                    .Build() }, false, true);
                    return;
                }

                if (next.Content.IsTrue())
                {
                    await component.UpdateAsync(x =>
                    {
                        if (x.Embed.IsSpecified)
                        {
                            try
                            {
                                var builder = EmbedBuilderExtensions.ToEmbedBuilder(x.Embed.Value);

                                builder.Fields[3].WithValue($"➡️ **{duration.ToReadableString()}**");

                                x.Embed = builder.Build();
                            }
                            catch { }
                        }
                    });

                    Punishments.Server.Connection.Send(new AzyWorks.Networking.NetPayload()
                        .WithMessage(new PunishmentEditDurationMessage(cachedBan.TargetId, cachedBan.TargetIp, duration, PunishmentType.Ban)));

                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle($"ℹ️ Délka upravena na `{next.Content}`")
                    .Build() }, false, true);
                }
                else
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithTitle("ℹ️ Operace zrušena.")
                    .Build() }, false, true);
                    return;
                }
            }
            else
            {
                if (RevokeRequestsChannel is null)
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle($"ℹ️ Tento server nemá nastavený žádný kanál pro požadavky na úpravu banů (nemáš permise).")
                    .Build() }, false, true);

                    return;
                }

                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle("ℹ️ Do další zprávy napiš novou délku (od aktuálního času).")
                    .Build() }, false, true);

                var channel = await component.GetChannelAsync();
                var next = await channel.GetNextMessageAsync(component.User.Id, Discord);
                if (next is null)
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithTitle("ℹ️ Čas vypršel.")
                    .Build() }, false, true);
                    return;
                }

                var duration = next.Content.ToSpan();

                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle($"ℹ️ Nová délka: `{duration.ToReadableString()}`. Další zprávou potvrď správnost (y/yes/ano/jo ..)")
                    .Build() }, false, true);

                next = await channel.GetNextMessageAsync(component.User.Id, Discord);
                if (next is null)
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithTitle("ℹ️ Čas vypršel.")
                    .Build() }, false, true);
                    return;
                }

                await RevokeRequestsChannel.SendMessageAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle("ℹ️ Požadavek na úpravu délky banu")
                    .WithDescription($"**{member.Mention} poslal požadavek na úpravu délky [banu]({component.Message.GetJumpUrl()}) pro uživatele `{cachedBan.TargetName} ({cachedBan.TargetId})`, který byl zabanován `{cachedBan.IssuerName} ({cachedBan.IssuerId})` za `{cachedBan.Reason}` na `{duration.ToReadableString()}` místo původních `{cachedBan.Duration.ToReadableString()}`**.")
                    .Build());

                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle("ℹ️ Požadavek na úpravu délky banu byl odeslán Vedení.")
                    .Build() }, false, true);
            }
        }

        private async Task RevokeBanAsync(SocketMessageComponent component, BanItem cachedBan)
        {
            if ((Punishments.Server.RoleSync.TryGetAccount(component.User.Id, out var account) && account.UserId == cachedBan.IssuerId)
                || (Discord.TryGetMember(component.User, out var member) && Discord.HasPermission(member, DiscordPermission.BanManagement)))
            {
                Punishments.Server.Connection.Send(new AzyWorks.Networking.NetPayload()
                    .WithMessage(new RemoteAdminExecuteMessage($"/unban {cachedBan.TargetId}", "Dedicated Server", "ID_Host")));

                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle("ℹ️ Požadavek na zrušení banu byl odeslán serveru.")
                    .Build() }, false, true);

                Cache.Remove(component.Message.Id);

                if (Punishments.TryGetHistory(cachedBan.TargetId, out var history))
                {
                    if (history.Punishments.RemoveWhere(x => 
                    x.IssuerId == cachedBan.IssuerId 
                    && x.Id == cachedBan.TargetId
                    && x.IssuedAt == cachedBan.IssuedAt
                    && x.EndsAt == cachedBan.EndsAt) > 0)
                    {
                        Punishments.Save();
                    }
                }

                await component.DeleteOriginalResponseAsync();
                await component.Message.DeleteAsync();
            }
            else
            {
                if (RevokeRequestsChannel is null)
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle($"ℹ️ Tento server nemá nastavený žádný kanál pro požadavky na úpravu banů (nemáš permise).")
                    .Build() }, false, true);

                    return;
                }

                await RevokeRequestsChannel.SendMessageAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle("ℹ️ Požadavek na zrušení banu")
                    .WithDescription($"**{member.Mention} poslal požadavek na zrušení [banu]({component.Message.GetJumpUrl()}) pro uživatele `{cachedBan.TargetName} ({cachedBan.TargetId})`, který byl zabanován `{cachedBan.IssuerName} ({cachedBan.IssuerId})` za `{cachedBan.Reason}` na `{cachedBan.Duration.ToReadableString()}`**.")
                    .Build());

                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle("ℹ️ Požadavek na zrušení banu byl odeslán Vedení.")
                    .Build() }, false, true);
            }
        }

        private async Task EditBanReasonAsync(SocketMessageComponent component, BanItem cachedBan)
        {
            if ((Punishments.Server.RoleSync.TryGetAccount(component.User.Id, out var account) && account.UserId == cachedBan.IssuerId)
                || (Discord.TryGetMember(component.User, out var member) && Discord.HasPermission(member, DiscordPermission.BanManagement)))
            {
                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle("ℹ️ Do další zprávy napiš nový důvod.")
                    .Build() }, false, true);

                var channel = await component.GetChannelAsync();
                var next = await channel.GetNextMessageAsync(component.User.Id, Discord);
                if (next is null)
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithTitle("ℹ️ Čas vypršel.")
                    .Build() }, false, true);
                    return;
                }

                var reason = next.Content;

                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle($"ℹ️ Nový důvod: `{reason}`. Další zprávou potvrď správnost (y/yes/ano/jo ..)")
                    .Build() }, false, true);

                next = await channel.GetNextMessageAsync(component.User.Id, Discord);
                if (next is null)
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithTitle("ℹ️ Čas vypršel.")
                    .Build() }, false, true);
                    return;
                }

                if (next.Content.IsTrue())
                {
                    await component.UpdateAsync(x =>
                    {
                        if (x.Embed.IsSpecified)
                        {
                            try
                            {
                                var builder = EmbedBuilderExtensions.ToEmbedBuilder(x.Embed.Value);

                                builder.Fields[4].WithValue($"```{reason}```");

                                x.Embed = builder.Build();
                            }
                            catch { }
                        }
                    });

                    Punishments.Server.Connection.Send(new AzyWorks.Networking.NetPayload()
                        .WithMessage(new PunishmentEditReasonMessage(cachedBan.TargetId, cachedBan.TargetIp, reason, PunishmentType.Ban)));

                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle($"ℹ️ Důvod upraven na `{next.Content}`")
                    .Build() }, false, true);
                }
                else
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithTitle("ℹ️ Operace zrušena.")
                    .Build() }, false, true);
                    return;
                }
            }
            else
            {
                if (RevokeRequestsChannel is null)
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle($"ℹ️ Tento server nemá nastavený žádný kanál pro požadavky na úpravu banů (nemáš permise).")
                    .Build() }, false, true);

                    return;
                }

                var channel = await component.GetChannelAsync();
                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle("ℹ️ Do další zprávy napiš nový důvod.")
                    .Build() }, false, true);

                var next = await channel.GetNextMessageAsync(component.User.Id, Discord);
                if (next is null)
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithTitle("ℹ️ Čas vypršel.")
                    .Build() }, false, true);
                    return;
                }

                var reason = next.Content;
                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle($"ℹ️ Nový důvod: `{reason}`. Další zprávou potvrď správnost (y/yes/ano/jo ..)")
                    .Build() }, false, true);

                next = await channel.GetNextMessageAsync(component.User.Id, Discord);
                if (next is null)
                {
                    await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithTitle("ℹ️ Čas vypršel.")
                    .Build() }, false, true);
                    return;
                }

                await RevokeRequestsChannel.SendMessageAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle("ℹ️ Požadavek na úpravu důvodu banu")
                    .WithDescription($"**{member.Mention} poslal požadavek na úpravu důvodu [banu]({component.Message.GetJumpUrl()}) pro uživatele `{cachedBan.TargetName} ({cachedBan.TargetId})`, který byl zabanován `{cachedBan.IssuerName} ({cachedBan.IssuerId})` za `{cachedBan.Reason}` na `{reason}` místo  `{cachedBan.Reason}`**.")
                    .Build());

                await component.FollowupAsync(null, new Embed[] { new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Punishments.Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithTitle("ℹ️ Požadavek na úpravu důvodu banu byl odeslán Vedení.")
                    .Build() }, false, true);
            }
        }

        public void Stop()
        {
            Channels[BanLogChannelType.Public].Clear();
            Channels[BanLogChannelType.AdminOnly].Clear();

            Discord.Client.ButtonExecuted -= OnButtonInteraction;
            Discord.OnReady -= OnDiscordReady;
            Punishments.OnPunishmentIssued -= OnPunishmentIssued;
            Punishments = null;

            Log = null;
            Discord = null;
        }

        public MessageComponent BuildBanButtons()
        {
            return new ComponentBuilder()
                .WithButton("❔ Upravit důvod", "ButtonBan_EditReason", ButtonStyle.Success, null, null, false, 0)
                .WithButton("🕒 Upravit délku", "ButtonBan_EditDuration", ButtonStyle.Success, null, null, false, 0)
                .WithButton("🗙 Zrušit", "ButtonBan_Revoke", ButtonStyle.Danger, null, null, false, 1)
                .Build();
        }

        private void OnPunishmentIssued(PunishmentHistory history, PunishmentIssuedMessage punishmentIssuedMessage)
        {
            if (punishmentIssuedMessage.Type != PunishmentType.Ban)
                return;

            var banSaved = false;
            var duration = (punishmentIssuedMessage.EndsAt - punishmentIssuedMessage.IssuedAt);

            if (Collection.TryGetService(out PlayerCacheService playerCacheService))
            {
                if (playerCacheService.TryFetch(punishmentIssuedMessage.Id, out var playerCache))
                {
                    if (punishmentIssuedMessage.Name == "Unknown - offline ban")
                    {
                        punishmentIssuedMessage.Name = playerCache.LastUserName;
                    }

                    if (string.IsNullOrWhiteSpace(punishmentIssuedMessage.Ip))
                    {
                        punishmentIssuedMessage.Ip = playerCache.UserIp;
                    }
                }
            }          

            foreach (var channelPair in Channels)
            {
                foreach (var channel in channelPair.Value)
                {
                    if (channelPair.Key is BanLogChannelType.AdminOnly)
                    {
                        if (Discord.ShowIpAddressInAdminOnly)
                        {
                            Task.Run(async () =>
                            {
                                var message = await channel.SendMessageAsync(null, false, new EmbedBuilder()
                                    .WithAuthor(new EmbedAuthorBuilder()
                                        .WithName(Punishments.Server.ServerName)
                                        .WithIconUrl(Discord.User.GetIconUrl()))
                                    .WithTitle("⛔ Byl udělen nový ban!")
                                    .WithColor(Color.Red)
                                    .WithFields(new EmbedFieldBuilder[]
                                    {
                                        new EmbedFieldBuilder()
                                            .WithName("🔗 Hráč")
                                            .WithValue(
                                            $"**Jméno:** {punishmentIssuedMessage.Name}\n" +
                                            $"**ID:** {punishmentIssuedMessage.Id}\n" +
                                            $"**IP:** {punishmentIssuedMessage.Ip}"),

                                        new EmbedFieldBuilder()
                                            .WithName("🔗 Udělil")
                                            .WithValue(
                                            $"**Jméno:** {punishmentIssuedMessage.IssuerName}\n" +
                                            $"**ID:** {punishmentIssuedMessage.IssuerId}"),

                                        new EmbedFieldBuilder()
                                            .WithName("🕒 Délka")
                                            .WithValue($"➡️ **{duration.ToReadableString()}**"),

                                        new EmbedFieldBuilder()
                                            .WithName("❔ Důvod")
                                            .WithValue($"```{punishmentIssuedMessage.Reason}```")
                                    })
                                    .WithFooter(new EmbedFooterBuilder()
                                        .WithText(
                                        $"🕒 Udělen: {punishmentIssuedMessage.IssuedAt.ToLocalTime().ToString("g")}\n" +
                                        $"🕒 Končí: {punishmentIssuedMessage.EndsAt.ToLocalTime().ToString("g")}\n" +
                                        $"🔗 ID: {history?.HistoryId ?? ""}"))
                                    .Build(), null, null, null, BuildBanButtons());

                                if (!banSaved)
                                {
                                    Cache.Enqueue(new BanItem
                                    {
                                        Duration = duration,
                                        EndsAt = punishmentIssuedMessage.EndsAt,
                                        IssuedAt = punishmentIssuedMessage.IssuedAt,
                                        IssuerId = punishmentIssuedMessage.IssuerId,
                                        IssuerName = punishmentIssuedMessage.IssuerName,
                                        MessageId = message.Id,
                                        Reason = punishmentIssuedMessage.Reason,
                                        TargetId = punishmentIssuedMessage.Id,
                                        TargetIp = punishmentIssuedMessage.Ip,
                                        TargetName = punishmentIssuedMessage.Name
                                    });

                                    banSaved = true;
                                }
                            });
                        }
                        else
                        {
                            Task.Run(async () =>
                            {
                                var message = await channel.SendMessageAsync(null, false, new EmbedBuilder()
                                    .WithAuthor(new EmbedAuthorBuilder()
                                        .WithName(Punishments.Server.ServerName)
                                        .WithIconUrl(Discord.User.GetIconUrl()))
                                    .WithTitle("⛔ Byl udělen nový ban!")
                                    .WithColor(Color.Red)
                                    .WithFields(new EmbedFieldBuilder[]
                                    {
                                        new EmbedFieldBuilder()
                                            .WithName("🔗 Hráč")
                                            .WithValue(
                                            $"**Jméno:** {punishmentIssuedMessage.Name}\n" +
                                            $"**ID:** {punishmentIssuedMessage.Id}"),

                                        new EmbedFieldBuilder()
                                            .WithName("🔗 Udělil")
                                            .WithValue(
                                            $"**Jméno:** {punishmentIssuedMessage.IssuerName}\n" +
                                            $"**ID:** {punishmentIssuedMessage.IssuerId}"),

                                        new EmbedFieldBuilder()
                                            .WithName("🕒 Délka")
                                            .WithValue($"➡️ **{duration.ToReadableString()}**"),

                                        new EmbedFieldBuilder()
                                            .WithName("❔ Důvod")
                                            .WithValue($"```{punishmentIssuedMessage.Reason}```")
                                    })
                                    .WithFooter(new EmbedFooterBuilder()
                                        .WithText(
                                        $"🕒 Udělen: {punishmentIssuedMessage.IssuedAt.ToLocalTime().ToString("g")}\n" +
                                        $"🕒 Končí: {punishmentIssuedMessage.EndsAt.ToLocalTime().ToString("g")}\n" +
                                        $"🔗 ID: {history?.HistoryId ?? ""}"))
                                    .Build(), null, null, null, BuildBanButtons());

                                if (!banSaved)
                                {
                                    Cache.Enqueue(new BanItem
                                    {
                                        Duration = duration,
                                        EndsAt = punishmentIssuedMessage.EndsAt,
                                        IssuedAt = punishmentIssuedMessage.IssuedAt,
                                        IssuerId = punishmentIssuedMessage.IssuerId,
                                        IssuerName = punishmentIssuedMessage.IssuerName,
                                        MessageId = message.Id,
                                        Reason = punishmentIssuedMessage.Reason,
                                        TargetId = punishmentIssuedMessage.Id,
                                        TargetIp = punishmentIssuedMessage.Ip,
                                        TargetName = punishmentIssuedMessage.Name
                                    });

                                    banSaved = true;
                                }
                            });
                        }
                    }
                    else
                    {
                        Task.Run(async () =>
                        {
                            await channel.SendMessageAsync(null, false, new EmbedBuilder()
                                .WithAuthor(new EmbedAuthorBuilder()
                                    .WithName(Punishments.Server.ServerName)
                                    .WithIconUrl(Discord.User.GetIconUrl()))
                                .WithTitle("⛔ Byl udělen nový ban!")
                                .WithColor(Color.Red)
                                .WithFields(new EmbedFieldBuilder[]
                                {
                                        new EmbedFieldBuilder()
                                            .WithName("🔗 Hráč")
                                            .WithValue(
                                            $"**Jméno**: {punishmentIssuedMessage.Name}"),

                                        new EmbedFieldBuilder()
                                            .WithName("🔗 Udělil")
                                            .WithValue(
                                            $"**Jméno**: {punishmentIssuedMessage.IssuerName}"),

                                        new EmbedFieldBuilder()
                                            .WithName("🕒 Délka")
                                            .WithValue($"➡️ **{duration.ToReadableString()}**"),

                                        new EmbedFieldBuilder()
                                            .WithName("❔ Důvod")
                                            .WithValue($"```{punishmentIssuedMessage.Reason}```")
                                })
                                    .WithFooter(new EmbedFooterBuilder()
                                        .WithText(
                                        $"🕒 Udělen: {punishmentIssuedMessage.IssuedAt.ToLocalTime().ToString("g")}\n" +
                                        $"🕒 Končí: {punishmentIssuedMessage.EndsAt.ToLocalTime().ToString("g")}"))
                                .Build());
                        });
                    }
                }
            }
        }
    }
}
