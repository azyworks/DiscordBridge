using Discord.Commands;
using Discord;

using System.Text;
using System.Data;

using AzyWorks.Pooling;

using DiscordBridge.CustomNetwork.Tickets;
using DiscordBridgeBot.Core.RoleSync;
using DiscordBridgeBot.Core.ScpSl;

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
                .WithFooter(new EmbedFooterBuilder()
                    .WithText($"Tento server běží na portu {Server.ServerPort}"))
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
                        .WithFooter(new EmbedFooterBuilder()
                            .WithText($"Tento server běží na portu {Server.ServerPort}"))
                        .Build());
                }
                else
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Blue)
                        .WithDescription(
                            @"❌ **Chybí ti permise!**")
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
                    .WithDescription(
                        @"❌ `Discord::TryGetMember`")
                    .WithFooter(new EmbedFooterBuilder()
                        .WithText($"Tento server běží na portu {Server.ServerPort}"))
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
                        .WithDescription(
                            @$"✅ **Role `{role}` smazána.**")
                        .WithFooter(new EmbedFooterBuilder()
                            .WithText($"Tento server běží na portu {Server.ServerPort}"))
                        .Build());
                }
                else
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Blue)
                        .WithDescription(
                            @"❌ **Chybí ti permise!**")
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
                    .WithColor(Color.Red)
                    .WithDescription(
                        @"❌ `Discord::TryGetMember`")
                    .WithFooter(new EmbedFooterBuilder()
                        .WithText($"Tento server běží na portu {Server.ServerPort}"))
                    .Build());
            }
        }

        [Command("link")]
        public async Task LinkAsync(string code)
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
                        .WithDescription(
                            @$"✅ **Ticket s ID `{ticket.Code}` úspěšně verifikován.**")
                        .WithFooter(new EmbedFooterBuilder()
                            .WithText($"Tento server běží na portu {Server.ServerPort}"))
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
                            .WithFooter(new EmbedFooterBuilder()
                                .WithText($"Tento server běží na portu {Server.ServerPort}"))
                            .Build());
                    }
                    else
                    {
                        await ReplyAsync(null, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Blue)
                            .WithDescription(
                                @$"**ℹ️ Na server ti nebyla přiřazena žádná role. Můžeš to udělat manuálně příkazem `role`.**")
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
                        .WithColor(Color.Red)
                        .WithDescription(
                            $@"❌ **Ticket s ID `{code}` nebyl nalezen.**")
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
                    .WithColor(Color.Red)
                    .WithDescription(
                        @"❌ `Discord::TryGetMember`")
                    .WithFooter(new EmbedFooterBuilder()
                        .WithText($"Tento server běží na portu {Server.ServerPort}"))
                    .Build());
            }
        }

        [Command("role")]
        public async Task RoleAsync()
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
                            .WithFooter(new EmbedFooterBuilder()
                                .WithText($"Tento server běží na portu {Server.ServerPort}"))
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
                                .WithDescription(
                                    @"❌ **Čas vypršel.**")
                                .WithFooter(new EmbedFooterBuilder()
                                    .WithText($"Tento server běží na portu {Server.ServerPort}"))
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
                                .WithDescription(
                                    @$"❌ **Role `{next.CleanContent}` nebyla nalezena mezi dostupnými.**")
                                .WithFooter(new EmbedFooterBuilder()
                                    .WithText($"Tento server běží na portu {Server.ServerPort}"))
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
                            .WithColor(Color.Red)
                            .WithDescription(
                                @"❌ **Nemáš dostupné žádné role.**")
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
                        .WithColor(Color.Red)
                        .WithDescription(
                            @"❌ **První si musíš propojit účet.**")
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
                    .WithColor(Color.Red)
                    .WithDescription(
                        @"❌ `Discord::TryGetMember`")
                    .WithFooter(new EmbedFooterBuilder()
                        .WithText($"Tento server běží na portu {Server.ServerPort}"))
                    .Build());
            }
        }
    }
}