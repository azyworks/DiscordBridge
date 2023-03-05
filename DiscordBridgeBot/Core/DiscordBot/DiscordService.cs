using AzyWorks.System.Services;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBridgeBot.Core.Configuration;
using DiscordBridgeBot.Core.Logging;

namespace DiscordBridgeBot.Core.DiscordBot
{
    public class DiscordService : IService
    {
        private LogService _log;

        public DiscordSocketClient Client { get; private set; }
        public CommandService Commands { get; private set; }
        public IServiceProvider CommandsServices { get; private set; }

        public SocketGuildUser User { get; private set; }
        public SocketGuild Guild { get; private set; }

        public SocketTextChannel[] AdminChannels { get; private set; }
        public SocketTextChannel[] Channels { get; private set; }

        public IServiceCollection Collection { get; set; }


        [Config("Discord.Token", "The token to use for your Discord bot.")]
        public string Token = "none";

        [Config("Discord.Prefix", "The prefix to use for commands.")]
        public string Prefix = "!";

        [Config("Discord.AdminOverride", "Whether or not to allow the Administrator permission to bypass all permission checks.")]
        public bool AdminOverride = true;

        [Config("Discord.GuildId", "The ID of your Discord guild. Required if the bot is in more than one guild.")]
        public ulong GuildId = 0;

        [Config("Discord.AdminChannelIds", "A list of admin-only text channel IDs.")]
        public ulong[] AdminChannelIds = new ulong[]
        {
            0
        };

        [Config("Discord.ChannelIds", "A list of channels where commands can be used.")]
        public ulong[] ChannelIds = new ulong[]
        {
            0
        };

        [Config("Discord.Permissions", "A list of all custom permissions.")]
        public Dictionary<ulong, DiscordPermission[]> Permissions = new Dictionary<ulong, DiscordPermission[]>()
        {
            [0] = new DiscordPermission[] { DiscordPermission.Linking }
        };

        public event Action<SocketGuildUser, SocketGuild> OnReady;

        public void Start(IServiceCollection collection, object[] initArgs)
        {
            _log = Collection.GetService<LogService>();
            Collection.GetService<ConfigManagerService>()?.ConfigHandler.RegisterConfigs(this);
        }

        public void Stop() => Disconnect();

        public void Connect()
        {
            _log.Info("Connecting to Discord ..");

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

            CommandsServices = Collection.ToProvider();

            Commands.CommandExecuted += OnCommandExecuted;
            Client.GuildAvailable += OnGuildAvailable;

            Task.Run(async () => await ConnectAsync());
        }

        public void Disconnect()
        {
            _log.Info("Disconnecting from Discord ..");

            Task.Run(async () => await DisconnectAsync());
        }

        public bool IsConsideredAdmin(SocketGuildUser user) => HasPermission(user, DiscordPermission.Administrator);

        public bool HasPermission(SocketGuildUser user, DiscordPermission permission)
        {
            if (permission is DiscordPermission.None)
                return true;
            else if (permission is DiscordPermission.Administrator
                && ((user.Guild.OwnerId == user.Id) || AdminOverride
                ? (user.GuildPermissions.Administrator || user.Roles.Any(x => x.Permissions.Administrator)) : false))
                return true;
            else if (HasPermission(user, DiscordPermission.Administrator))
                return true;
            else
            {
                if (Permissions.TryGetValue(user.Id, out var perms) && perms.Contains(permission))
                    return true;
                else
                {
                    foreach (var role in user.Roles)
                    {
                        if (Permissions.TryGetValue(role.Id, out perms) && perms.Contains(permission))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public bool TryGetMember(SocketUser user, out SocketGuildUser member)
        {
            member = Guild.GetUser(user.Id);
            return member != null;
        }

        public string GetMention(ulong id)
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

        internal async Task ConnectAsync()
        {
            await Client.StartAsync();
            await Client.LoginAsync(TokenType.Bot, Token);

            await Commands.AddModuleAsync<DiscordCommandService>(CommandsServices);

            _log.Info("Connected to Discord!");
        }
        
        internal async Task DisconnectAsync()
        {
            await Client.StopAsync();
            await Client.DisposeAsync();

            Client = null;

            _log.Info("Disconnected!");
        }

        private Task OnCommandExecuted(Optional<CommandInfo> command, ICommandContext ctx, IResult result)
        {
            if (!command.IsSpecified)
                return Task.CompletedTask;

            if (result is ExecuteResult executeResult)
            {
                if (!executeResult.IsSuccess)
                {
                    if (executeResult.Error.HasValue)
                        _log.Error($"Command execution failed: {executeResult.Error.Value}");
                    else
                        _log.Error($"Command execution failed: unknown");

                    if (!string.IsNullOrWhiteSpace(executeResult.ErrorReason))
                        _log.Error(executeResult.ErrorReason);

                    if (executeResult.Exception != null)
                        _log.Error(executeResult.Exception);
                }
                else
                {
                    _log.Debug($"Command executed: {command.Value.Name}");
                }
            }

            return Task.CompletedTask;
        }

        private async Task OnMessageReceived(SocketMessage message)
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
            await Commands.ExecuteAsync(context, argPos, CommandsServices, MultiMatchHandling.Best);
        }

        private Task OnGuildAvailable(SocketGuild guild)
        {
            if (GuildId != 0 && GuildId != guild.Id)
                return Task.CompletedTask;

            if (Guild != null)
                return Task.CompletedTask;

            Guild = guild;
            User = guild.CurrentUser;

            _log.Info($"Found Discord guild: {Guild.Name} ({Guild.Id}), searching for channels ..");

            AdminChannels = new SocketTextChannel[AdminChannelIds.Length];
            Channels = new SocketTextChannel[ChannelIds.Length];

            for (int i = 0; i < ChannelIds.Length; i++)
            {
                Channels[i] = Guild.GetTextChannel(ChannelIds[i]);

                if (Channels[i] is null)
                    _log.Warn($"Failed to find public channel: {ChannelIds[i]}");
                else
                    _log.Info($"Found public channel: #{Channels[i].Name}");
            }

            for (int i = 0; i < AdminChannelIds.Length; i++)
            {
                AdminChannels[i] = Guild.GetTextChannel(AdminChannelIds[i]);

                if (AdminChannels[i] is null)
                    _log.Warn($"Failed to find admin-only channel: {AdminChannelIds[i]}");
                else
                    _log.Info($"Found admin-only channel: #{AdminChannels[i].Name}");
            }

            Client.MessageReceived += OnMessageReceived;

            OnReady?.Invoke(User, Guild);

            _log.Info("Discord is ready!");

            return Task.CompletedTask;
        }

        public bool IsValid()
        {
            return true;
        }
    }
}