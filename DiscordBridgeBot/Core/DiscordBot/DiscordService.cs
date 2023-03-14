using AzyWorks.System.Services;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBridgeBot.Core.Configuration;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.Punishments;
using DiscordBridgeBot.Core.ScpSlLogs;

namespace DiscordBridgeBot.Core.DiscordBot
{
    public class DiscordService : IService
    {
        private LogService _log;

        public DiscordComponents Components { get; } = new DiscordComponents();
        public DiscordSocketClient Client { get; private set; }
        public CommandService Commands { get; private set; }
        public IServiceProvider CommandsServices { get; private set; }

        public SocketGuildUser User { get; private set; }
        public SocketGuild Guild { get; private set; }

        public SocketTextChannel[] AdminChannels { get; private set; }
        public SocketTextChannel[] Channels { get; private set; }

        public IServiceCollection Collection { get; set; }

        public bool IsReady { get; private set; }

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

        [Config("BanLog.ShowIPs", "Whether or not to show IP addresses in admin-only channels.")]
        public bool ShowIpAddressInAdminOnly { get; set; } = true;

        [Config("Discord.Permissions", "A list of all custom permissions.")]
        public Dictionary<ulong, DiscordPermission[]> Permissions = new Dictionary<ulong, DiscordPermission[]>()
        {
            [0] = new DiscordPermission[] { DiscordPermission.Linking }
        };

        [Config("BanLog.ChannelIds", "A list of channel IDs for the ban log.")]
        public Dictionary<BanLogChannelType, List<ulong>> ConfigChannelIds { get; set; } = new Dictionary<BanLogChannelType, List<ulong>>()
        {
            [BanLogChannelType.AdminOnly] = new List<ulong>() { 0, 1 },
            [BanLogChannelType.Public] = new List<ulong>() { 2, 3 }
        };

        [Config("BanLog.RevokeChannelId", "The ID of the channel to send ban revoke requests to.")]
        public ulong RevokeRequestsChannelId { get; set; } = 0;

        [Config("RolePlay.RoleRequestsChannelId", "The channel to send role requests into.")]
        public ulong RoleRequestsChannelId { get; set; } = 0;

        [Config("RolePlay.RoleRequestsAllowedChannels", "The list of channels that can be used to submit role requests.")]
        public ulong[] RoleRequestsAllowedChannelIds { get; set; } = new ulong[]
        {
            0, 
            1
        };

        [Config("Reports.Channels", "A list of channels to send in-game reports to.")]
        public Dictionary<ulong, ulong[]> ReportChannels { get; set; } = new Dictionary<ulong, ulong[]>()
        {
            [0] = new ulong[] { 0, 1 },
            [1] = new ulong[] { 2, 3 }
        };

        public event Action<SocketGuildUser, SocketGuild> OnReady;

        public void Start(IServiceCollection collection, object[] initArgs)
        {
            _log = Collection.GetService<LogService>();

            Collection.GetService<ConfigManagerService>()?.ConfigHandler.RegisterConfigs(this);
        }

        public void Stop()
        {
            Collection.RemoveService<PunishmentsService>();

            Disconnect();
        }

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
            Client.SelectMenuExecuted += OnSelectMenuExecuted;
            Client.ButtonExecuted += OnButtonExecuted;

            Components.AddHandlers(this);

            Task.Run(async () => await ConnectAsync());
        }

        private async Task OnSelectMenuExecuted(SocketMessageComponent component)
        {

        }
        
        private async Task OnButtonExecuted(SocketMessageComponent component)
        {

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

        public bool TryGetMember(SocketUser user, out SocketGuildUser member)
        {
            member = Guild.GetUser(user.Id);
            return member != null;
        }

        public bool TryGetMember(ulong memberId, out SocketGuildUser member)
        {
            member = Guild.GetUser(memberId);
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
            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();

            await Commands.AddModuleAsync<DiscordCommandService>(CommandsServices);

            _log.Info("Connected to Discord!");
        }
        
        internal async Task DisconnectAsync()
        {
            await Client.LogoutAsync();
            await Client.StopAsync();
            await Client.DisposeAsync();

            Components.RemoveHandlers();

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

        private async Task OnInteraction()
        {

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

            IsReady = true;

            return Task.CompletedTask;
        }

        public bool IsValid()
        {
            return true;
        }
    }
}