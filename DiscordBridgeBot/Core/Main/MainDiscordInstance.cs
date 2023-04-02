using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBridgeBot.Core.Configuration;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.Main;

namespace DiscordBridgeBot.Core.DiscordBot
{
    public static class MainDiscordInstance 
    {
        public static DiscordSocketClient Client { get; private set; }
        public static CommandService Commands { get; private set; }

        public static SocketGuildUser User { get; private set; }
        public static SocketGuild Guild { get; private set; }

        public static SocketTextChannel[] AdminChannels { get; private set; }
        public static SocketTextChannel[] Channels { get; private set; }

        public static LogService Log { get; set; }

        public static bool IsReady { get; private set; }

        [Config("Discord.Token", "The token to use for your Discord bot.")]
        public static string Token = "none";

        [Config("Discord.Prefix", "The prefix to use for commands.")]
        public static string Prefix = "!";

        [Config("Discord.AdminOverride", "Whether or not to allow the Administrator permission to bypass all permission checks.")]
        public static bool AdminOverride = true;

        [Config("Discord.GuildId", "The ID of your Discord guild. Required if the bot is in more than one guild.")]
        public static ulong GuildId = 0;

        [Config("Discord.AdminChannelIds", "A list of admin-only text channel IDs.")]
        public static ulong[] AdminChannelIds = new ulong[]
        {
            0
        };

        [Config("Discord.ChannelIds", "A list of channels where commands can be used.")]
        public static ulong[] ChannelIds = new ulong[]
        {
            0
        };

        [Config("Discord.Permissions", "A list of all custom permissions.")]
        public static Dictionary<ulong, DiscordPermission[]> Permissions = new Dictionary<ulong, DiscordPermission[]>()
        {
            [0] = new DiscordPermission[] { DiscordPermission.Linking }
        };

        public static event Action<SocketGuildUser, SocketGuild> OnReady;

        public static void Connect()
        {
            Log.Info("Connecting to Discord ..");

            Client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                AlwaysDownloadDefaultStickers = true,
                AlwaysDownloadUsers = true,
                AlwaysResolveStickers = true,
                APIOnRestInteractionCreation = true,
                ConnectionTimeout = 2500,
                DefaultRetryMode = Discord.RetryMode.AlwaysRetry,
                FormatUsersInBidirectionalUnicode = true,
                GatewayIntents = Discord.GatewayIntents.All,
                HandlerTimeout = null,
                LargeThreshold = 250,
                LogGatewayIntentWarnings = true,
                LogLevel = Discord.LogSeverity.Critical,
                UseInteractionSnowflakeDate = true,
                UseSystemClock = true
            });

            Commands = new CommandService(new CommandServiceConfig()
            {
                CaseSensitiveCommands = false,
                DefaultRunMode = RunMode.Async,
                IgnoreExtraArgs = false,
                ThrowOnError = true,
                LogLevel = LogSeverity.Warning
            });

            Commands.CommandExecuted += OnCommandExecuted;

            Client.GuildAvailable += OnGuildAvailable;

            Task.Run(async () => await ConnectAsync());
        }

        public static void Disconnect()
        {
            Log.Info("Disconnecting from Discord ..");

            Task.Run(async () => await DisconnectAsync());
        }

        public static bool IsConsideredAdmin(SocketGuildUser user) => HasPermission(user, DiscordPermission.Administrator);

        public static bool HasPermission(SocketGuildUser user, DiscordPermission permission)
        {
            if (permission is DiscordPermission.None)
                return true;

            if (permission is DiscordPermission.Administrator || AdminOverride)
            {
                if (user.GuildPermissions.Administrator)
                {
                    return true;
                }

                if (user.Roles.Any(x => x.Permissions.Administrator))
                {
                    return true;
                }
            }

            if (Permissions.TryGetValue(user.Id, out var userPerms))
            {
                if (userPerms.Contains(permission))
                {
                    return true;
                }
            }

            foreach (var role in user.Roles)
            {
                if (Permissions.TryGetValue(role.Id, out var rolePerms))
                {
                    if (rolePerms.Contains(permission))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool TryGetMember(SocketUser user, out SocketGuildUser member)
        {
            member = Guild.GetUser(user.Id);
            return member != null;
        }

        public static bool TryGetMember(ulong memberId, out SocketGuildUser member)
        {
            member = Guild.GetUser(memberId);
            return member != null;
        }

        public static string GetMention(ulong id)
        {
            var role = Guild.GetRole(id);
            if (role != null)
                return role.Mention;

            var user = Guild.GetUser(id);
            if (user != null)
                return user.Mention;

            var channel = Guild.GetChannel(id);
            if (channel != null)
                return $"<#{channel.Id}>";

            return "Unknown ID";
        }

        internal static async Task ConnectAsync()
        {
            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();

            await Commands.AddModuleAsync<MainDiscordInstanceCommands>(null);

            Log.Info("Connected to Discord!");
        }

        internal static async Task DisconnectAsync()
        {
            await Client.LogoutAsync();
            await Client.StopAsync();
            await Client.DisposeAsync();

            Client = null;

            Log.Info("Disconnected!");
        }

        private static Task OnCommandExecuted(Optional<CommandInfo> command, ICommandContext ctx, IResult result)
        {
            if (!command.IsSpecified)
                return Task.CompletedTask;

            if (result is ExecuteResult executeResult)
            {
                if (!executeResult.IsSuccess)
                {
                    if (executeResult.Error.HasValue)
                        Log.Error($"Command execution failed: {executeResult.Error.Value}");
                    else
                        Log.Error($"Command execution failed: unknown");

                    if (!string.IsNullOrWhiteSpace(executeResult.ErrorReason))
                        Log.Error(executeResult.ErrorReason);

                    if (executeResult.Exception != null)
                        Log.Error(executeResult.Exception);
                }
                else
                {
                    Log.Debug($"Command executed: {command.Value.Name}");
                }
            }

            return Task.CompletedTask;
        }

        private static async Task OnMessageReceived(SocketMessage message)
        {
            if (message is not SocketUserMessage userMessage)
                return;

            int argPos = 0;

            if (!(userMessage.HasCharPrefix(Prefix[0], ref argPos) ||
                userMessage.HasMentionPrefix(Client.CurrentUser, ref argPos)) ||
                userMessage.Author.IsBot)
                return;

            if (!TryGetMember(userMessage.Author, out var member))
                return;

            if (!Channels.Any(x => x.Id == userMessage.Channel.Id))
            {
                if (IsConsideredAdmin(member))
                {
                    if (!AdminChannels.Any(x => x.Id == userMessage.Channel.Id))
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }

            var context = new SocketCommandContext(Client, userMessage);
            await Commands.ExecuteAsync(context, argPos, null, MultiMatchHandling.Best);
        }

        private static Task OnGuildAvailable(SocketGuild guild)
        {
            if (GuildId != 0 && GuildId != guild.Id)
                return Task.CompletedTask;

            if (Guild != null)
                return Task.CompletedTask;

            Guild = guild;
            User = guild.CurrentUser;

            Log.Info($"Found Discord guild: {Guild.Name} ({Guild.Id}), searching for channels ..");

            AdminChannels = new SocketTextChannel[AdminChannelIds.Length];
            Channels = new SocketTextChannel[ChannelIds.Length];

            for (int i = 0; i < ChannelIds.Length; i++)
            {
                Channels[i] = Guild.GetTextChannel(ChannelIds[i]);

                if (Channels[i] is null)
                    Log.Warn($"Failed to find public channel: {ChannelIds[i]}");
                else
                    Log.Info($"Found public channel: #{Channels[i].Name}");
            }

            for (int i = 0; i < AdminChannelIds.Length; i++)
            {
                AdminChannels[i] = Guild.GetTextChannel(AdminChannelIds[i]);

                if (AdminChannels[i] is null)
                    Log.Warn($"Failed to find admin-only channel: {AdminChannelIds[i]}");
                else
                    Log.Info($"Found admin-only channel: #{AdminChannels[i].Name}");
            }

            Client.MessageReceived += OnMessageReceived;

            OnReady?.Invoke(User, Guild);

            Log.Info("Discord is ready!");

            IsReady = true;

            return Task.CompletedTask;
        }
    }
}