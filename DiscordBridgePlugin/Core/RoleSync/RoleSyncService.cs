using AzyWorks.Extensions;
using AzyWorks.IO.Binary;
using AzyWorks.System.Services;

using DiscordBridge.CustomNetwork.Tickets;

using PluginAPI.Core;
using PluginAPI.Events;

using System.Collections.Generic;
using System.IO;

namespace DiscordBridgePlugin.Core.RoleSync
{
    public class RoleSyncService : IService
    {
        public IServiceCollection Collection { get; set; }

        public RoleSyncTicketsService Tickets { get; private set; }

        public HashSet<RoleSyncConnection> Connections { get; private set; }

        public bool IsValid()
        {
            return true;
        }

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
            Tickets = Collection.AddService<RoleSyncTicketsService>();
            Tickets.OnRoleUpdated += UpdateRole;
            Tickets.OnTicketInvalidated += OnTicketInvalidated;
            Tickets.OnTicketValidated += OnTicketValidated;

            EventManager.RegisterEvents<RoleSyncEvents>(Collection);
            LoadConnections();
        }

        public void Stop()
        {
            Tickets.OnRoleUpdated -= UpdateRole;
            Tickets.OnTicketValidated -= OnTicketValidated;
            Tickets.OnTicketInvalidated -= OnTicketInvalidated;

            EventManager.UnregisterEvents<RoleSyncEvents>(Collection);
            SaveConnections();
        }

        public bool TryGetRole(string userId, out RoleSyncConnection role)
        {
            role = null;

            foreach (var connection in Connections)
            {
                if (connection.UserId == userId)
                {
                    if (connection.Server == Server.Port)
                    {
                        role = connection;
                        break;
                    }
                }
            }

            return role != null;
        }

        public void OnTicketValidated(RoleSyncTicket ticket, RoleSyncTicketValidationReason reason)
        {
            UpdateRole(ticket.Account.Id, ticket.Account.Role);

            if (Player.TryGet(ticket.Account.Id, out var player))
            {
                player.SendConsoleMessage($"Ticket ({ticket.Code}) validated: {reason}");
            }
        }

        public void OnTicketInvalidated(RoleSyncTicket ticket, RoleSyncTicketInvalidationReason reason)
        {
            if (Player.TryGet(ticket.Account.Id, out var player))
            {
                player.SendConsoleMessage($"Ticket ({ticket.Code}) invalidated: {reason}", "red");
            }
        }

        public void UpdateRole(string userId, string role)
        {
            if (TryGetRole(userId, out var connection))
            {
                connection.RoleKey = role;
                connection.UserId = userId;

                SaveConnections(true);
                return;
            }
            else
            {
                Connections.Add(new RoleSyncConnection
                {
                    RoleKey = role,
                    Server = Server.Port,
                    UserId = userId
                });

                SaveConnections(true);
            }
        }

        private void LoadConnections()
        {
            if (Connections is null)
                Connections = new HashSet<RoleSyncConnection>();
            else
                Connections.Clear();

            if (!File.Exists(LoaderService.Config.RoleSyncDatabasePath))
            {
                SaveConnections(true);
                return;
            }

            var file = new BinaryFile(LoaderService.Config.RoleSyncDatabasePath);

            file.ReadFile();

            Connections.AddRange(file.GetData<HashSet<RoleSyncConnection>>("conns"));

            Log.Info($"Loaded {Connections.Count} connections.", "Discord Bridge :: RoleSyncService");
        }

        private void SaveConnections(bool force = false)
        {
            if (Connections is null)
                Connections = new HashSet<RoleSyncConnection>();

            if (Connections.Count < 1 && !force)
                return;

            var file = new BinaryFile(LoaderService.Config.RoleSyncDatabasePath);

            file.WriteData("conns", Connections);
            file.WriteFile();

            Log.Info($"Saved {Connections.Count} connections.", "Discord Bridge :: RoleSyncService");
        }
    }
}