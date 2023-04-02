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
using System.Net;
using System.Reflection;
using Discord.WebSocket;

namespace DiscordBridgeBot.Core.DiscordBot
{
    public static class TempDir
    {
        public static string Path;

        static TempDir()
        {
            Path = $"{Directory.GetCurrentDirectory()}/Temporary";

            if (!Directory.Exists(Path))
                Directory.CreateDirectory(Path);
        }

        public static string Get(string fileName)
            => $"{Path}/{fileName}";

        public static string Place(string fileName)
        {
            var path = Get(fileName);
            if (File.Exists(path))
                File.Delete(path);
            return path;
        }
    }

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
                                    $"**ℹ️ Vyber si z těchto rolí (do další zprávy napiš číslo nebo jméno role).**\n" +
                                    $"{builder}")
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
                                    .WithDescription("❌ **Čas vypršel.**")
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
                                    .WithDescription($"❌ **Role `{next.CleanContent}` nebyla nalezena mezi dostupnými.**")
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
                                        $"✅ **Byla ti přiřazena role `{chosenRole.Name}`!**\n" +
                                        "ℹ️ **Roli na serveru si můžeš změnit příkazem `role`.")
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
                        .WithDescription("❌ `Discord::TryGetMember`")
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
                        .WithDescription("❌ **Chybí ti permise!**")
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
                        .WithDescription("❌ Player Cache služba není aktivní.")
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
                        .WithDescription("❌ **Chybí ti permise!**")
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
                        .WithDescription("❌ Whitelist služba není aktivní.")
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
                            .WithDescription("✅ **Whitelist aktivován.**")
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
                            .WithDescription("✅ **Whitelist deaktivován.**")
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
                                        .WithColor(Color.Green)
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
                                        .WithColor(Color.Green)
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
                            .WithColor(Color.Green)
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
                            .WithColor(Color.Green)
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

        public async Task UploadSchematicAsync(Attachment attachment)
        {
            var dest = $"{Discord.UploadSchematicsPath}/{attachment.Filename}";
            var destDir = $"{Discord.UploadSchematicsPath}/{Path.GetFileNameWithoutExtension(dest)}";

            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            dest = $"{destDir}/{attachment.Filename}";

            if (File.Exists(dest))
                File.Delete(dest);

            using (var webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(attachment.Url, dest);
            }
        }

        public async Task UploadMethodAsync(Attachment attachment, string type, Func<string, bool> pluginIsAllowed)
        {
            if (type is "schematic")
            {
                await UploadSchematicAsync(attachment);
                return;
            }

            await UploadPluginAsync(attachment, pluginIsAllowed);
        }

        public async Task UploadPluginAsync(Attachment attachment, Func<string, bool> isAllowed)
        {
            var tempPath = Path.GetTempPath();
            var tempFilePath = $"{tempPath}/{attachment.Filename}";

            using (var webClient = new WebClient())
            {
                await webClient.DownloadFileTaskAsync(attachment.Url, tempFilePath);
            }

            var assembly = Assembly.Load(await File.ReadAllBytesAsync(tempFilePath));
            string pluginType = null;

            foreach (var type in assembly.GetTypes())
            {
                if (type.GetMethods().Any(x => x.CustomAttributes.Any(x => x.AttributeType.Name == "PluginAPI.Core.Attributes.PluginEntryPoint")))
                {
                    pluginType = "nwapi";
                    break;
                }

                if (type.GetMethods().Any(x => x.Name == "OnEnabled"))
                {
                    pluginType = "exiled";
                    break;
                }
            }

            if (pluginType is null)
                throw new InvalidOperationException();

            if (!isAllowed(pluginType))
                return;

            var dest = pluginType is "nwapi" ?
                $"{Discord.UploadNwApiPath}/{attachment.Filename}" :
                $"{Discord.UploadExiledPluginPath}/{attachment.Filename}";

            if (File.Exists(dest))
                File.Delete(dest);

            if (pluginType is "nwapi")
                File.Move(tempFilePath, dest);
            else
                File.Move(tempFilePath, dest);
        }

        [Command("upload")]
        public async Task UploadAsync()
        {
            string GetUploadType(Attachment attachment)
            {
                if (attachment.Filename.EndsWith("json"))
                    return "schematic";

                if (attachment.Filename.EndsWith("dll"))
                    return "plugin";

                return null;
            }

            Discord.TryGetMember(Context.User, out var member);

            bool IsAllowed(string pluginType)
            {
                if (pluginType is "nwapi" && !Discord.HasPermission(member, DiscordPermission.UploadNwApiPlugins))
                {
                    Task.Run(async () =>
                    {
                        await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Red)
                        .WithDescription($"❌ **Nemáš práva na nahrávání NW API pluginů.**")
                        .Build());
                    });

                    return false;
                }

                if (pluginType is "exiled" && !Discord.HasPermission(member, DiscordPermission.UploadExiledPlugins))
                {
                    Task.Run(async () =>
                    {
                        await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Red)
                        .WithDescription($"❌ **Nemáš práva na nahrávání EXILED pluginů.**")
                        .Build());
                    });

                    return false;
                }

                return true;
            }

            if (!Context.Message.Attachments.Any())
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription($"❌ **Zpráva neobsahuje žádné attachmenty.**")
                    .Build());

                return;
            }

            if (Context.Message.Attachments.Count is 1)
            {
                var type = GetUploadType(Context.Message.Attachments.ElementAt(0));
                if (type is null)
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Red)
                        .WithDescription($"❌ **Neplatný attachment. Povolené jsou pouze soubory s příponou dll nebo json.**")
                        .Build());

                    return;
                }

                if (type is "schematic" && !Discord.HasPermission(member, DiscordPermission.UploadSchematics))
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Red)
                        .WithDescription($"❌ **Nemáš práva na nahrávání schematik.**")
                        .Build());

                    return;
                }

                await UploadMethodAsync(Context.Message.Attachments.First(), type, IsAllowed);
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(Server.ServerName)
                        .WithIconUrl(Discord.User.GetIconUrl()))
                    .WithColor(Color.Green)
                    .WithDescription($"✅ **Attachment `{Context.Message.Attachments.First().Filename}` nahrán na server.**")
                    .Build());
            }
            else
            {
                foreach (var attachment in Context.Message.Attachments)
                {
                    var type = GetUploadType(attachment);
                    if (type is null)
                    {
                        await ReplyAsync(null, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Red)
                            .WithDescription($"❌ **Neplatný attachment (`{attachment.Filename}`). Povolené jsou pouze soubory s příponou dll nebo json.**")
                            .Build());

                        continue;
                    }

                    if (type is "schematic" && !Discord.HasPermission(member, DiscordPermission.UploadSchematics))
                    {
                        await ReplyAsync(null, false, new EmbedBuilder()
                            .WithAuthor(new EmbedAuthorBuilder()
                                .WithName(Server.ServerName)
                                .WithIconUrl(Discord.User.GetIconUrl()))
                            .WithColor(Color.Red)
                            .WithDescription($"❌ **Nemáš práva na nahrávání schematik. Přeskočeno `{attachment.Filename}`**")
                            .Build());

                        continue;
                    }

                    await UploadMethodAsync(attachment, type, IsAllowed);
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(Server.ServerName)
                            .WithIconUrl(Discord.User.GetIconUrl()))
                        .WithColor(Color.Green)
                        .WithDescription($"✅ **Attachment `{attachment.Filename}` nahrán na server.**")
                        .Build());
                }
            }
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
    }
}