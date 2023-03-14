using Discord.Commands;
using Discord;

using System.Text;
using System.Data;

using AzyWorks.Pooling;
using AzyWorks.Utilities;

using DiscordBridge.CustomNetwork.RemoteAdmin;
using DiscordBridge.CustomNetwork.Tickets;
using DiscordBridgeBot.Core.RoleSync;
using DiscordBridgeBot.Core.ScpSl;
using DiscordBridgeBot.Core.Whitelists;
using DiscordBridgeBot.Core.PlayerCache;
using DiscordBridgeBot.Core.IpApi;
using DiscordBridgeBot.Core.SteamIdApi;

namespace DiscordBridgeBot.Core.DiscordBot
{
    public class DiscordCommandService : ModuleBase<SocketCommandContext>
    {
        public DiscordService Discord { get; private set; }
        public ScpSlServer Server { get; private set; }

        public DiscordCommandService(DiscordService discordService)
        {
            Discord = discordService;
            Server = discordService.Collection as ScpSlServer;
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            await ReplyAsync(null, false, new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName(Server.ServerName)
                    .WithIconUrl(Discord.User.GetIconUrl()))
                .WithColor(Color.Blue)
                .WithDescription(
                    @"**❓ Vítej na pomocné stránce pro Discord Bridge!**
                        
                        🔗 **Přidávání propojitelných rolí**
                            Propojitelnou roli můžeš přidat pomocí příkazu `add`, naopak odebrat zase pomocí `remove`.

                        🔗 **Propojování herního účtu**
                            Pro propojení účtu musíte použít příkaz `.link` v konzoli, kterou otevřete pomocí klávesy `;`. V konzoli dostanete další instrukce.")
                .Build());
        }

        [Command("add")]
        public async Task AddAsync(string role, string name, params ulong[] applicableIds)
        {
            if (Discord.TryGetMember(Context.User, out var sender))
            {
                if (Discord.HasPermission(sender, DiscordPermission.RoleSyncManagement))
                {
                    Server.RoleSync.UpdateRole(role, name, applicableIds);

                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Green)
                        .WithDescription(
                            @$"✅ **Role `{role}` aktualizována.**

                                  **Jméno**: {name}
                                  **Role**: {role}
                                  **ID**: {string.Join(", ", applicableIds.Select(x =>
                            {
                                var role = Discord.Guild.GetRole(x);

                                if (role != null)
                                    return role.Mention;

                                var user = Discord.Guild.GetUser(x);

                                if (user != null)
                                    return user.Mention;

                                return $"Neznámé ID ({x})";
                            }))}")
                        .Build());
                }
                else
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Blue)
                        .WithDescription(@"❌ **Chybí ti permise!**")
                        .WithFooter(new EmbedFooterBuilder()
                            .WithText($"Tento server běží na portu {Server.ServerPort}"))
                        .Build());
                }
            }
            else
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithDescription(@"❌ `Discord::TryGetMember`")
                    .Build());
            }
        }

        [Command("remove")]
        public async Task RemoveAsync(string role)
        {
            if (Discord.TryGetMember(Context.User, out var sender))
            {
                if (Discord.HasPermission(sender, DiscordPermission.RoleSyncManagement))
                {
                    Server.RoleSync.RemoveRole(role);

                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Green)
                        .WithDescription(@$"✅ **Role `{role}` smazána.**")
                        .Build());
                }
                else
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Blue)
                        .WithDescription(@"❌ **Chybí ti permise!**")
                        .Build());
                }
            }
            else
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription(@"❌ `Discord::TryGetMember`")
                    .Build());
            }
        }

        [Command("link")]
        public async Task LinkAsync(string code)
        {
            try
            {
                if (Discord.TryGetMember(Context.User, out var sender))
                {
                    if (Server.RoleSyncTickets.TryGetTicket(code, out var ticket))
                    {
                        Server.RoleSyncTickets.ValidateTicket(ticket, Context.User.Id, RoleSyncTicketValidationReason.UserVerified);

                        await ReplyAsync(null, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Blue)
                            .WithDescription(@$"✅ **Ticket s ID `{ticket.Code}` úspěšně verifikován.**")
                            .Build());

                        if (Server.RoleSync.TryGetLinkedRole(ticket.Account.Id, out var role))
                        {
                            await ReplyAsync(null, false, new EmbedBuilder()
                                .WithAuthor(new EmbedAuthorBuilder()
                                    .WithName(Server.ServerName)
                                    .WithIconUrl(Discord.User.GetIconUrl()))
                                .WithColor(Color.Blue)
                                .WithDescription(
                                    @$"**✅ Byla ti přiřazena role `{role.Name}`!**
                                   ℹ️ Roli na serveru si můžeš změnit příkazem `role`.")
                                .Build());
                        }
                        else
                        {
                            await ReplyAsync(null, false, new EmbedBuilder()
                                .WithAuthor(new EmbedAuthorBuilder()
                                    .WithName(Server.ServerName)
                                    .WithIconUrl(Discord.User.GetIconUrl()))
                                .WithColor(Color.Blue)
                                .WithDescription(@$"**ℹ️ Na server ti nebyla přiřazena žádná role. Můžeš to udělat manuálně příkazem `role`.**")
                                .Build());
                        }
                    }
                    else
                    {
                        await ReplyAsync(null, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Red)
                            .WithDescription($@"❌ **Ticket s ID `{code}` nebyl nalezen.**")
                            .Build());
                    }
                }
                else
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Red)
                        .WithDescription(@"❌ `Discord::TryGetMember`")
                        .Build());
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription(ex.Message)
                    .Build());
            }
        }

        [Command("role")]
        public async Task RoleAsync()
        {
            try
            {
                if (Discord.TryGetMember(Context.User, out var sender))
                {
                    if (Server.RoleSync.TryGetAccount(sender.Id, out var account))
                    {
                        if (Server.RoleSync.TryGetLinkableRoles(sender.Id, out var roles))
                        {
                            var builder = PoolManager.Get<StringBuilder>();

                            for (int i = 0; i < roles.Count; i++)
                            {
                                var role = roles.ElementAt(i);

                                builder.AppendLine($"🔗 **[{i + 1}] {role.Name}** *({Discord.GetMention(Server.RoleSync.GetBoundId(role.PossibleIds.ToArray(), sender))})*");
                            }

                            await ReplyAsync(null, false, new EmbedBuilder()
                                .WithAuthor(new EmbedAuthorBuilder()
                                    .WithName(Server.ServerName)
                                    .WithIconUrl(Discord.User.GetIconUrl()))
                                .WithColor(Color.Blue)
                                .WithDescription(
                                    @$"**ℹ️ Vyber si z těchto rolí (do další zprávy napiš číslo nebo jméno role).**

                               {builder}")
                                .Build());

                            PoolManager.Return(builder);

                            var next = await Context.Channel.GetNextMessageAsync(sender.Id, Discord);

                            if (next is null)
                            {
                                await ReplyAsync(null, false, new EmbedBuilder()
                                    .WithAuthor(new EmbedAuthorBuilder()
                                        .WithName(Server.ServerName)
                                        .WithIconUrl(Discord.User.GetIconUrl()))
                                    .WithColor(Color.Red)
                                    .WithDescription(@"❌ **Čas vypršel.**")
                                    .Build());
                            }

                            RoleSyncRole chosenRole = null;

                            if (int.TryParse(next.CleanContent, out var index))
                            {
                                index--;
                                chosenRole = roles.ElementAtOrDefault(index);
                            }
                            else
                            {
                                chosenRole = roles.FirstOrDefault(x => x.Name.ToLower() == next.CleanContent.ToLower());
                            }

                            if (chosenRole is null)
                            {
                                await ReplyAsync(null, false, new EmbedBuilder()
                                    .WithAuthor(new EmbedAuthorBuilder()
                                        .WithName(Server.ServerName)
                                        .WithIconUrl(Discord.User.GetIconUrl()))
                                    .WithColor(Color.Red)
                                    .WithDescription(@$"❌ **Role `{next.CleanContent}` nebyla nalezena mezi dostupnými.**")
                                    .Build());
                            }
                            else
                            {
                                Server.RoleSync.LinkRole(account, chosenRole);

                                await ReplyAsync(null, false, new EmbedBuilder()
                                    .WithAuthor(new EmbedAuthorBuilder()
                                        .WithName(Server.ServerName)
                                        .WithIconUrl(Discord.User.GetIconUrl()))
                                    .WithColor(Color.Blue)
                                    .WithDescription(
                                        @$"✅ **Byla ti přiřazena role `{chosenRole.Name}`!**
                                       ℹ️ **Roli na serveru si můžeš změnit příkazem `role`.")
                                    .Build());
                            }
                        }
                        else
                        {
                            await ReplyAsync(null, false, new EmbedBuilder()
                                .WithAuthor(new EmbedAuthorBuilder()
                                    .WithName(Server.ServerName)
                                    .WithIconUrl(Discord.User.GetIconUrl()))
                                .WithColor(Color.Red)
                                .WithDescription(@"❌ **Nemáš dostupné žádné role.**")
                                .Build());
                        }
                    }
                    else
                    {
                        await ReplyAsync(null, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Red)
                            .WithDescription(@"❌ **První si musíš propojit účet.**")
                            .Build());
                    }
                }
                else
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Red)
                        .WithDescription(@"❌ `Discord::TryGetMember`")
                        .Build());
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription(ex.Message)
                    .Build());
            }
        }

        [Command("cache")]
        public async Task CacheAsync(string userValue)
        {
            try
            {
                if (!Discord.TryGetMember(Context.User, out var member))
                    return;

                if (!Discord.HasPermission(member, DiscordPermission.PlayerCacheAccess))
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Blue)
                        .WithDescription(@"❌ **Chybí ti permise!**")
                        .Build());

                    return;
                }

                var pCache = Server.GetService<PlayerCacheService>();
                if (pCache is null)
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Red)
                        .WithDescription(@"❌ Player Cache služba není aktivní.")
                        .Build());

                    return;
                }

                if (pCache.TryFetch(userValue, out var cached))
                {
                    var builder = new StringBuilder();

                    builder.AppendLine()
                        .AppendLine($"**Nicknames ({cached.AllNames.Count})**");

                    foreach (var name in cached.AllNames)
                        builder.AppendLine($"**>-** {name}");

                    builder.AppendLine()
                        .AppendLine($"**Steam accounts ({cached.AllIDs.Count})**");

                    foreach (var id in cached.AllIDs)
                        builder.AppendLine($"**>-** {id}");

                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Red)
                        .WithDescription($"✅ **Query:** {userValue}\n" +
                        $"**ID:** {cached.LastUserId}\n" +
                        $"**IP:** {cached.UserIp}\n" +
                        $"**Name:** {cached.LastUserName}\n" +
                        $"{builder}")
                        .Build());
                }
                else
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Red)
                        .WithDescription($"❌ Uživatel `{userValue}` nemá zaznamenané ID.")
                        .Build());
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription(ex.Message)
                    .Build());
            }
        }

        [Command("whitelist")]
        [Alias("wh")]
        public async Task WhitelistAsync([Remainder] string value)
        {
            try
            {
                if (!Discord.TryGetMember(Context.User, out var member))
                    return;

                if (!Discord.HasPermission(member, DiscordPermission.WhitelistManagement))
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Blue)
                        .WithDescription(@"❌ **Chybí ti permise!**")
                        .Build());

                    return;
                }

                var whService = Server.GetService<WhitelistService>();
                if (whService is null)
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Red)
                        .WithDescription(@"❌ Whitelist služba není aktivní.")
                        .Build());

                    return;
                }

                if (bool.TryParse(value, out var whitelistState))
                {
                    whService.IsActive = whitelistState;

                    if (whitelistState)
                    {
                        await ReplyAsync(null, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Red)
                            .WithDescription(@"✅ **Whitelist aktivován.**")
                            .Build());

                        return;
                    }
                    else
                    {
                        await ReplyAsync(null, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Red)
                            .WithDescription(@"✅ **Whitelist deaktivován.**")
                            .Build());

                        return;
                    }
                }
                else
                {
                    if (MentionUtils.TryParseUser(value, out var userId))
                    {
                        if (Discord.TryGetMember(userId, out member))
                        {
                            if (Server.RoleSync.TryGetAccount(member.Id, out var account))
                            {
                                if (!whService.IsWhitelisted(account.UserId))
                                {
                                    whService.Add(account.UserId);

                                    await ReplyAsync(null, false, new EmbedBuilder()
                                        .WithAuthor(new EmbedAuthorBuilder()
                                            .WithName(Server.ServerName)
                                            .WithIconUrl(Discord.User.GetIconUrl()))
                                        .WithColor(Color.Red)
                                        .WithDescription($"✅ **{member.Mention} ({account.UserId}) byl přidán na whitelist.**")
                                        .Build());
                                }
                                else
                                {
                                    whService.Remove(account.UserId);

                                    await ReplyAsync(null, false, new EmbedBuilder()
                                        .WithAuthor(new EmbedAuthorBuilder()
                                            .WithName(Server.ServerName)
                                            .WithIconUrl(Discord.User.GetIconUrl()))
                                        .WithColor(Color.Red)
                                        .WithDescription($"✅ **{member.Mention} ({account.UserId}) byl odebrán z whitelistu.**")
                                        .Build());
                                }

                                return;
                            }
                            else
                            {
                                await ReplyAsync(null, false, new EmbedBuilder()
                                    .WithAuthor(new EmbedAuthorBuilder()
                                        .WithName(Server.ServerName)
                                        .WithIconUrl(Discord.User.GetIconUrl()))
                                    .WithColor(Color.Red)
                                    .WithDescription($"❌ **{member.Mention} nemá propojený účet.**")
                                    .Build());

                                return;
                            }
                        }
                        else
                        {
                            await ReplyAsync(null, false, new EmbedBuilder()
                                .WithAuthor(new EmbedAuthorBuilder()
                                    .WithName(Server.ServerName)
                                    .WithIconUrl(Discord.User.GetIconUrl()))
                                .WithColor(Color.Red)
                                .WithDescription($"❌ **Uživatel s ID `{userId}` nenalazen.**")
                                .Build());

                            return;
                        }
                    }
                    else
                    {
                        if (!value.Contains("@"))
                        {
                            await ReplyAsync(null, false, new EmbedBuilder()
                                .WithAuthor(new EmbedAuthorBuilder()
                                    .WithName(Server.ServerName)
                                    .WithIconUrl(Discord.User.GetIconUrl()))
                                .WithColor(Color.Red)
                                .WithDescription($"❌ **Neplatné ID - musí obsahovat identifikátor (`@steam` / `@discord`)**")
                                .Build());

                            return;
                        }

                        var pureId = value.Split('@')[0];
                        var idType = value.Split('@')[1];

                        if (idType != "steam" && idType != "discord")
                        {
                            await ReplyAsync(null, false, new EmbedBuilder()
                                .WithAuthor(new EmbedAuthorBuilder()
                                    .WithName(Server.ServerName)
                                    .WithIconUrl(Discord.User.GetIconUrl()))
                                .WithColor(Color.Red)
                                .WithDescription($"❌ **Neplatné ID - musí obsahovat identifikátor (`@steam` / `@discord`)**")
                                .Build());

                            return;
                        }

                        if (pureId.Length == 17)
                        {
                            if (idType != "steam")
                            {
                                await ReplyAsync(null, false, new EmbedBuilder()
                                    .WithAuthor(new EmbedAuthorBuilder()
                                        .WithName(Server.ServerName)
                                        .WithIconUrl(Discord.User.GetIconUrl()))
                                    .WithColor(Color.Red)
                                    .WithDescription($"❌ **Neplatné ID.**")
                                    .Build());

                                return;
                            }
                        }
                        else if (pureId.Length == 18)
                        {
                            if (idType != "discord")
                            {
                                await ReplyAsync(null, false, new EmbedBuilder()
                                    .WithAuthor(new EmbedAuthorBuilder()
                                        .WithName(Server.ServerName)
                                        .WithIconUrl(Discord.User.GetIconUrl()))
                                    .WithColor(Color.Red)
                                    .WithDescription($"❌ **Neplatné ID.**")
                                    .Build());

                                return;
                            }
                        }

                        var id = $"{pureId}@{idType}";

                        if (!whService.IsWhitelisted(id))
                        {
                            whService.Add(id);

                            await ReplyAsync(null, false, new EmbedBuilder()
                                .WithAuthor(new EmbedAuthorBuilder()
                                    .WithName(Server.ServerName)
                                    .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Red)
                                .WithDescription($"✅ **ID `{id}` bylo přidáno na whitelist.**")
                                .Build());
                        }
                        else
                        {
                            whService.Remove(id);

                            await ReplyAsync(null, false, new EmbedBuilder()
                                .WithAuthor(new EmbedAuthorBuilder()
                                    .WithName(Server.ServerName)
                                    .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Red)
                                .WithDescription($"✅ **ID `{id}` bylo odebráno z whitelistu.**")
                                .Build());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription(ex.Message)
                    .Build());
            }
        }

        [Command("ssh")]
        public async Task SshAsync([Remainder] string command)
        {
            if (!Discord.TryGetMember(Context.User, out var member))
                return;

            if (!Discord.HasPermission(member, DiscordPermission.SshExecute))
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithDescription(@"❌ **Chybí ti permise!**")
                    .Build());

                return;
            }

            var linuxCommand = new LinuxCommand()
                .WithArg(command);

            var output = linuxCommand.Execute();

            await ReplyAsync(null, false, new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName(Server.ServerName)
                    .WithIconUrl(Discord.User.GetIconUrl()))
                .WithColor(Color.Red)
                .WithDescription($"`{(string.IsNullOrWhiteSpace(output) ? "No output." : output)}`")
                .Build());

            linuxCommand = null;
        }

        [Command("ra")]
        public async Task RemoteAdminAsync([Remainder] string command)
        {
            if (!Discord.TryGetMember(Context.User, out var member))
                return;

            if (!Discord.HasPermission(member, DiscordPermission.RemoteAdminExecute))
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Blue)
                    .WithDescription(@"❌ **Chybí ti permise!**")
                    .Build());

                return;
            }

            var channel = Context.Channel;

            Server.Connection.AddTemporaryCallback<RemoteAdminExecuteResponseMessage>(x =>
            {
                Task.Run(async () =>
                {
                    if (x.IsSuccess)
                    {
                        await channel.SendMessageAsync(null, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Red)
                            .WithDescription($"❌ `{x.Response}`")
                            .Build());
                    }
                    else
                    {
                        await channel.SendMessageAsync(null, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Green)
                            .WithDescription($"✅ `{x.Response}`")
                            .Build());
                    }
                });
            });

            Server.Connection.Send(new AzyWorks.Networking.NetPayload()
                .WithMessage(new RemoteAdminExecuteMessage(command, 
                        $"{Context.User.Username}#{Context.User.Discriminator}", 
                           Context.User.Id.ToString())));
        }

        [Command("ip")]
        public async Task IpAsync([Remainder] string ip)
        {
            var response = await IpApiService.GetAsync(ip);
            if (response.Status != "success")
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription($"❌ `{response.Message}`")
                    .Build());

                return;
            }

            await ReplyAsync(null, false, new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName(Server.ServerName)
                    .WithIconUrl(Discord.User.GetIconUrl()))
                .WithColor(Color.Red)
                .WithFields(new EmbedFieldBuilder[]
                {
                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("IP")
                        .WithValue(string.IsNullOrWhiteSpace(response.Query) ? "Unknown" : response.Query),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("ASN ID")
                        .WithValue(string.IsNullOrWhiteSpace(response.AsId) ? "Unknown" : response.AsId),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("ASN Name")
                        .WithValue(string.IsNullOrWhiteSpace(response.AsName) ? "Unknown" : response.AsName),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Internet Service Provider")
                        .WithValue(string.IsNullOrWhiteSpace(response.InternetServiceProviderName) ? "Unknown" : response.InternetServiceProviderName),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Organization")
                        .WithValue(string.IsNullOrWhiteSpace(response.OrganizationName) ? "Unknown" : response.OrganizationName),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Reverse DNS")
                        .WithValue(string.IsNullOrWhiteSpace(response.ReverseDns) ? "Unknown" : response.ReverseDns),
                        
                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Continent")
                        .WithValue($"{response.Continent ?? "Unknown"} ({response.ContinentCode ?? "Unknown"}) [Currency: **{response.Currency ?? "Unknown"}**]"),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Country")
                        .WithValue($"{response.Country ?? "Unknown"} ({response.CountryIso ?? "Unknown"})"),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Region")
                        .WithValue($"{response.RegionName ?? "Unknown"} ({response.Region ?? "Unknown"})"),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("City")
                        .WithValue($"{response.City ?? "Unknown"} ({response.ZipCode ?? "Unknown"})"),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("District")
                        .WithValue(string.IsNullOrWhiteSpace(response.District) ? "Unknown" : response.District),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName($"Coordinates")
                        .WithValue($"Latitude: {response.Latitude}; Longitude: {response.Longitude}"),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Time Zone")
                        .WithValue($"{response.TimeZone ?? "Unknown"} ({response.TimeZoneOffset})"),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Flags")
                        .WithValue(string.IsNullOrWhiteSpace(
                            $"{(response.IsMobile ? "Mobile, " : "")}" +
                            $"{(response.IsProxy ? "Proxy, " : "")}" +
                            $"{(response.IsHosting ? "Hosting" : "")}")
                        
                        ? "None" :

                            $"{(response.IsMobile ? "Mobile, " : "")}" +
                            $"{(response.IsProxy ? "Proxy, " : "")}" +
                            $"{(response.IsHosting ? "Hosting" : "")}")
                })
                .Build());
        }

        [Command("steamid")]
        public async Task SteamIdAsync([Remainder] string steamId)
        {
            var response = await SteamIdApiService.GetAsync(steamId);
            if (response is null)
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription($"❌ Vrácená odpověď je chybová.")
                    .Build());

                return;
            }

            await ReplyAsync(null, false, new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName(Server.ServerName)
                    .WithIconUrl(Discord.User.GetIconUrl()))
                .WithColor(Color.Red)
                .WithFields(new EmbedFieldBuilder[]
                {
                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("ID")
                        .WithValue(steamId),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Real Name")
                        .WithValue(string.IsNullOrWhiteSpace(response.RealName) ? "Private" : response.RealName),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Country")
                        .WithValue(string.IsNullOrWhiteSpace(response.Country) ? "Private" : response.Country),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Status")
                        .WithValue(response.Status),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Visibility")
                        .WithValue(response.Visibility),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Created At")
                        .WithValue(response.CreatedAt),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Last LogOff At")
                        .WithValue(response.LastLogOffAt),

                    new EmbedFieldBuilder()
                        .WithIsInline(true)
                        .WithName("Flags")
                        .WithValue($"" +
                            $"{(response.IsVacBanned ? "VAC Banned, " : "")}" +
                            $"{(!response.IsTradeClean ? "Not Trade Clean, " : "")}" +
                            $"{(!response.IsCommunityClean ? "Not Community Clean" : "")}")
                })
                .Build());
        }
    }
}