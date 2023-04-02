using AzyWorks.Utilities;

using Discord;
using Discord.Commands;
using DiscordBridgeBot.Core.DiscordBot;
using DiscordBridgeBot.Core.Extensions;
using DiscordBridgeBot.Core.IpApi;
using DiscordBridgeBot.Core.ScpSlServerListApi;
using DiscordBridgeBot.Core.SteamIdApi;

namespace DiscordBridgeBot.Core.Main
{
    public class MainDiscordInstanceCommands : ModuleBase<SocketCommandContext>
    {
        [Command("restart")]
        public async Task RestartAsync(string type)
        {
            MainDiscordInstance.TryGetMember(Context.User, out var member);

            if (!MainDiscordInstance.HasPermission(member, DiscordPermission.ServerRestart))
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(MainDiscordInstance.User.Nickname)
                        .WithIconUrl(MainDiscordInstance.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithTitle($"❌ Nemáš práva na start serveru.")
                    .Build());

                return;
            }

            new LinuxCommand()
                .WithArg($"stop{type.ToLower()}")
                .Execute();

            new LinuxCommand()
                .WithArg($"start{type.ToLower()}")
                .Execute();

            await ReplyAsync(null, false, new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName(MainDiscordInstance.User.Nickname)
                    .WithIconUrl(MainDiscordInstance.User.GetIconUrl()))
                .WithColor(Color.Red)
                .WithTitle($"✅ Server `{type.ToUpper()}` restartován")
                .Build());
        }

        [Command("ip")]
        public async Task IpAsync([Remainder] string ip)
        {
            var response = await IpApiService.GetAsync(ip);
            if (response.Status != "success")
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(MainDiscordInstance.User.Nickname)
                        .WithIconUrl(MainDiscordInstance.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription($"❌ `{response.Message}`")
                    .Build());

                return;
            }

            await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(MainDiscordInstance.User.Nickname)
                        .WithIconUrl(MainDiscordInstance.User.GetIconUrl()))
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
                        .WithName(MainDiscordInstance.User.Nickname)
                        .WithIconUrl(MainDiscordInstance.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription($"❌ Vrácená odpověď je chybová.")
                    .Build());

                return;
            }

            await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(MainDiscordInstance.User.Nickname)
                        .WithIconUrl(MainDiscordInstance.User.GetIconUrl()))
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

        [Command("ssh")]
        public async Task SshAsync([Remainder] string command)
        {
            if (!MainDiscordInstance.TryGetMember(Context.User, out var member))
                return;

            if (!MainDiscordInstance.HasPermission(member, DiscordPermission.SshExecute))
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(MainDiscordInstance.User.Nickname)
                        .WithIconUrl(MainDiscordInstance.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription(@"❌ **Chybí ti permise!**")
                    .Build());

                return;
            }

            var linuxCommand = new LinuxCommand()
                .WithArg(command);

            var output = linuxCommand.Execute();

            await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(MainDiscordInstance.User.Nickname)
                        .WithIconUrl(MainDiscordInstance.User.GetIconUrl()))
                    .WithColor(Color.Red)
                .WithDescription($"`{(string.IsNullOrWhiteSpace(output) ? "No output." : output)}`")
                .Build());

            linuxCommand = null;
        }

        [Command("servers")]
        [Alias("serverlist", "slist")]
        public async Task ServerListAsync(string input, string query = null)
        {
            var queryType = ScpSlServerListApiQueryType.All;
            if (!string.IsNullOrWhiteSpace(query))
            {
                if (query.StartsWith("-q="))
                    query = query.Replace("-q=", "");

                if (query.StartsWith("--query="))
                    query = query.Replace("--query=", "");

                if (!ScpSlServerListApiService.TryGetQueryType(query, out queryType))
                {
                    await ReplyAsync(null, false, new EmbedBuilder()
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName(MainDiscordInstance.User.Nickname)
                            .WithIconUrl(MainDiscordInstance.User.GetIconUrl()))
                        .WithColor(Color.Red)
                        .WithDescription(@"❌ **Invalidní query type!**")
                        .Build());

                    return;
                }
            }

            var matches = await ScpSlServerListApiService.MatchAsync(input, queryType);
            if (!matches.Any())
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(MainDiscordInstance.User.Nickname)
                        .WithIconUrl(MainDiscordInstance.User.GetIconUrl()))
                    .WithColor(Color.Red)
                    .WithDescription(@"❌ **Nenalezeny žádné odpovídající servery.**")
                    .Build());

                return;
            }

            string GetServerFlags(ScpSlServerListApiItem item)
            {
                string str = "";

                if (item.IsModded)
                    str += "Módovaný, ";

                if (item.IsFriendlyFireEnabled)
                    str += "Zapnutý friendly fire, ";

                if (item.IsPrivateBeta)
                    str += "Soukromá beta, ";

                if (item.IsWhitelisted)
                    str += "Zapnutý whitelist, ";

                if (string.IsNullOrEmpty(str))
                    str = "Žádné";
                else
                    str = str.Remove(str.LastIndexOf(','), 1).Trim();

                return str;
            }

            if (matches.Count is 1)
            {
                await ReplyAsync(null, false, new EmbedBuilder()
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(MainDiscordInstance.User.Nickname)
                        .WithIconUrl(MainDiscordInstance.User.GetIconUrl()))
                    .WithColor(Color.Green)
                    .WithTitle("✅ Nalezen jeden výsledek!")
                    .WithInlineField("Jméno", matches[0].ClearName)
                    .WithInlineField("IP", $"{matches[0].Ip}:{matches[0].Port}")
                    .WithInlineField("Počet hráčů", $"{matches[0].Players} / {matches[0].MaxPlayers}")
                    .WithInlineField("Pastebin", $"[{matches[0].PastebinId}]({matches[0].PastebinUrl})")
                    .WithInlineField("Version", matches[0].VersionString)
                    .WithInlineField("Server ID", matches[0].ServerId)
                    .WithInlineField("Account ID", matches[0].AccountId)
                    .WithInlineField("Official Type", $"{matches[0].OfficialType} ({matches[0].OfficialCodeValue})")
                    .WithInlineField("Kontinent", matches[0].ContinentCode)
                    .WithInlineField("ISO", matches[0].IsoCode)
                    .WithInlineField("Pozice", $"Šířka: {matches[0].Latitude}\nDélka: {matches[0].Longitude}")
                    .WithInlineField("Vzdálenost", $"{matches[0].Distance} km")
                    .WithInlineField("Sekce", matches[0].DisplaySectionValue)
                    .WithInlineField("Mod Flags", matches[0].ModFlagsValue)
                    .WithInlineField("Server Flags", GetServerFlags(matches[0]))
                    .Build());

                return;
            }
        }
    }
}