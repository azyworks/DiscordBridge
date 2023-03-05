using AzyWorks;
using AzyWorks.IO.Binary;
using AzyWorks.System.Services;

using Discord;
using Discord.WebSocket;

using DiscordBridge.CustomNetwork.RoleSync;
using DiscordBridge.CustomNetwork.Tickets;
using DiscordBridgeBot.Core.Configuration;
using DiscordBridgeBot.Core.DiscordBot;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.Network;
using DiscordBridgeBot.Core.RoleSync.Tickets;
using DiscordBridgeBot.Core.ScpSl;

namespace DiscordBridgeBot.Core.RoleSync
{
    public class RoleSyncService : IService
    {
        private LogService _log;

        public const string NoneRole = "<NONE>";

        public RoleSyncTicketService Tickets { get; private set; }

        public DiscordService Discord { get; private set; }
        public NetworkService Network { get; private set; }

        public ScpSlServer Server { get; private set; }

        public HashSet<RoleSyncCacheAccount> Accounts { get; private set; }
        public HashSet<RoleSyncRole> Roles { get; private set; }

        public IServiceCollection Collection { get; set; }

        public event Action<RoleSyncCacheAccount, RoleSyncRole> OnRoleLinked;
        public event Action<RoleSyncCacheAccount, RoleSyncRole> OnRoleUnlinked;

        public event Action<RoleSyncCacheAccount> OnAccountLinked;
        public event Action<RoleSyncCacheAccount> OnAccountUnliked;

        public void Start(IServiceCollection collection, object[] initArgs)
        {
            if (!(collection is ScpSlServer server))
            {
                Log.SendError("Core :: RoleSyncService", $"Collection {collection} is not an ScpSlServer.");
                return;
            }

            Server = server;

            _log = collection.GetService<LogService>(); 

            collection.AddService<RoleSyncTicketService>();
            collection.GetService<ConfigManagerService>().ConfigHandler.RegisterConfigs(this);

            Tickets = collection.GetService<RoleSyncTicketService>();
            Discord = collection.GetService<DiscordService>();
            Network = collection.GetService<NetworkService>();

            LoadRoles();

            Tickets.OnTicketValidated += OnTicketValidated;
            Discord.OnReady += OnDiscordReady;
        }

        public void UpdateRole(string role, string name, IEnumerable<ulong> applicableIds)
        {
            if (TryGetRole(role, out var syncRole))
            {
                syncRole.PossibleIds = applicableIds.ToHashSet();
                syncRole.Name = name;
            }
            else
            {
                syncRole = new RoleSyncRole
                {
                    Key = role,
                    PossibleIds = applicableIds.ToHashSet(),
                    Name = name,
                    LinkedUsers = new HashSet<ulong>()
                };

                Roles.Add(syncRole);
            }

            SaveRoles();
        }

        public void RemoveRole(string role)
        {
            var syncRole = Roles.FirstOrDefault(x => x.Key == role);

            if (syncRole != null)
            {
                if (Roles.Remove(syncRole))
                {
                    SaveRoles();
                }
            }
        }

        public void LinkRole(RoleSyncCacheAccount account, RoleSyncRole role)
        {
            if (role.LinkedUsers.Add(account.DiscordId))
            {
                _log.Info($"Linked {account.DiscordId} to {role.Name}");
                SaveRoles();

                Network.Client.Send(new AzyWorks.Networking.NetPayload()
                    .WithMessage(new RoleSyncRoleMessage(account.UserId, role.Key)));
            }
        }

        public void LinkAccount(string userId, SocketGuildUser user)
        {
            if (TryGetAccount(userId, out var account))
            {
                account.DiscordId = user?.Id ?? 0;
            }
            else
            {
                account = new RoleSyncCacheAccount
                {
                    DiscordId = user?.Id ?? 0,
                    UserId = userId,
                    UserName = ""
                };

                Accounts.Add(account);
            }

            SaveRoles();
        }

        public void LinkAccount(string userId, ulong discordId)
        {
            if (TryGetAccount(userId, out var account))
            {
                account.DiscordId = discordId;
            }
            else
            {
                account = new RoleSyncCacheAccount
                {
                    DiscordId = discordId,
                    UserId = userId,
                    UserName = ""
                };

                Accounts.Add(account);
            }

            SaveRoles();
        }

        public void UnlinkRole(RoleSyncCacheAccount account, RoleSyncRole role)
        {
            if (role.LinkedUsers.Remove(account.DiscordId))
            {
                _log.Info($"Unlinked {account.DiscordId} from {role.Name}");
                SaveRoles();

                Network.Client.Send(new AzyWorks.Networking.NetPayload()
                    .WithMessage(new RoleSyncRoleMessage(account.UserId, NoneRole)));
            }
        }

        public void UnlinkAccount(RoleSyncCacheAccount account)
        {
            if (Accounts.Remove(account) || Roles.Any(x => x.LinkedUsers.Remove(account.DiscordId)))
            {
                SaveRoles();

                Network.Client.Send(new AzyWorks.Networking.NetPayload()
                    .WithMessage(new RoleSyncRoleMessage(account.UserId, NoneRole)));

                _log.Info($"Unlinked account: {account.DiscordId}");
            }
        }

        public ulong GetBoundId(ulong[] applicableIds, SocketGuildUser user)
        {
            if (applicableIds.Contains(user.Id))
                return user.Id;

            foreach (var role in user.Roles)
            {
                if (applicableIds.Contains(role.Id))
                    return role.Id;
            }

            return 0;
        }

        public bool TryGetRole(string roleId, out RoleSyncRole role)
        {
            role = Roles.FirstOrDefault(x => x.Name.ToLower() == roleId.ToLower() || x.Key.ToLower() == roleId.ToLower());
            return role != null;
        }

        public bool TryGetAccount(ulong discordId, out RoleSyncCacheAccount account)
        {
            account = Accounts.FirstOrDefault(x => x.DiscordId == discordId);
            return account != null;
        }

        public bool TryGetAccount(string userId, out RoleSyncCacheAccount account)
        {
            account = Accounts.FirstOrDefault(x => x.UserId == userId);
            return account != null;
        }

        public bool TryGetLinkableRoles(ulong discordId, out HashSet<RoleSyncRole> roles)
        {
            roles = new HashSet<RoleSyncRole>();

            var account = Accounts.FirstOrDefault(x => x.DiscordId == discordId);

            if (account is null)
                return false;

            var member = Discord.Guild.GetUser(discordId);

            if (member is null)
                return false;

            foreach (var role in Roles)
            {
                if (IsRoleAllowed(member, role))
                {
                    roles.Add(role);
                }
            }

            return roles.Any();
        }

        public bool IsRoleAllowed(SocketGuildUser user, RoleSyncRole role)
        {
            if (role.PossibleIds.Contains(user.Id))
                return true;

            if (role.PossibleIds.Any(y => user.Roles.Any(x => x.Id == y)))
                return true;

            return false;
        }

        public bool TryGetLinkedRole(string userId, out RoleSyncRole role)
        {
            if (!TryGetAccount(userId, out var account))
            {
                role = null;
                return false;
            }

            role = Roles.FirstOrDefault(x => x.LinkedUsers.Contains(account.DiscordId));
            return role != null;
        }

        public bool TryGetLinkedRole(ulong discordId, out RoleSyncRole role)
        {
            if (!TryGetAccount(discordId, out var account))
            {
                role = null;
                return false;
            }

            role = Roles.FirstOrDefault(x => x.LinkedUsers.Contains(account.DiscordId));
            return role != null;
        }

        private void OnTicketValidated(RoleSyncTicket ticket, ulong validatorId, RoleSyncTicketValidationReason reason)
        {
            LinkAccount(ticket.Account.Id, validatorId);

            if (TryGetLinkableRoles(validatorId, out var roles))
            {
                var role = roles.First();

                Network.Client.Send(new AzyWorks.Networking.NetPayload()
                    .WithMessage(new RoleSyncRoleMessage(ticket.Account.Id, role.Key)));

                if (TryGetAccount(ticket.Account.Id, out var account))
                    LinkRole(account, role);

                ticket.Account.Role = role.Key;

                _log.Info($"Auto-linked role {role.Name} to user {ticket.Account.Id}");
            }
        }

        private void OnRoleRemoved(SocketGuildUser user, SocketRole role)
        {
            if (TryGetLinkedRole(user.Id, out var syncRole) && TryGetAccount(user.Id, out var account))
            {
                if (!IsRoleAllowed(user, syncRole))
                {
                    UnlinkRole(account, syncRole);
                }
            }
        }

        private void OnRoleAdded(SocketGuildUser user, SocketRole role)
        {
            if (!TryGetLinkedRole(user.Id, out _) && TryGetAccount(user.Id, out var account))
            {
                if (TryGetLinkableRoles(user.Id, out var roles))
                {
                    var syncRole = roles.First();

                    LinkRole(account, syncRole);
                }
            }
        }

        private void OnDiscordReady(SocketGuildUser user, SocketGuild guild)
        {
            Discord.Client.GuildMemberUpdated += OnMemberUpdated;
            Discord.Client.UserLeft += OnMemberLeft;
        }

        private Task OnMemberLeft(SocketGuild guild, SocketUser user)
        {
            if (guild.Id != Discord.Guild.Id)
                return Task.CompletedTask;

            if (TryGetAccount(user.Id, out var account))
            {
                UnlinkAccount(account);
            }

            return Task.CompletedTask;
        }

        private Task OnMemberUpdated(Cacheable<SocketGuildUser, ulong> previous, SocketGuildUser current)
        {
            if (!previous.HasValue)
                return Task.CompletedTask;

            if (current.Guild.Id != Discord.Guild.Id)
                return Task.CompletedTask;

            foreach (var role in current.Roles)
            {
                if (!previous.Value.Roles.Any(x => x.Id == role.Id))
                {
                    OnRoleAdded(current, role);
                }
            }

            foreach (var role in previous.Value.Roles)
            {
                if (!current.Roles.Any(x => x.Id == role.Id))
                {
                    OnRoleRemoved(current, role);
                }
            }

            return Task.CompletedTask;
        }

        public void SaveRoles()
        {
            var cache = new BinaryFile($"{Server.ServerPath}/RoleSyncCache");

            if (Accounts is null)
                Accounts = new HashSet<RoleSyncCacheAccount>();

            if (Roles is null)
                Roles = new HashSet<RoleSyncRole>();

            cache.WriteData("accounts", Accounts);
            cache.WriteData("roles", Roles);
            cache.WriteFile();

            _log.Info($"Saved {Accounts.Count} accounts.");
            _log.Info($"Saved {Roles.Count} roles.");
        }

        public void LoadRoles()
        {
            if (!File.Exists($"{Server.ServerPath}/RoleSyncCache"))
            {
                SaveRoles();
                return;
            }

            var cache = new BinaryFile($"{Server.ServerPath}/RoleSyncCache");

            cache.ReadFile();

            Accounts = cache.GetData<HashSet<RoleSyncCacheAccount>>("accounts");
            Roles = cache.GetData<HashSet<RoleSyncRole>>("roles");

            _log.Info($"Loaded {Accounts.Count} accounts.");
            _log.Info($"Loaded {Roles.Count} roles.");
        }

        public bool IsValid()
        {
            return true;
        }

        public void Stop()
        {
            SaveRoles();

            _log = null;

            Accounts = null;
            Roles = null;
        }
    }
}