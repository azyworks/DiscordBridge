using AzyWorks.Networking;
using AzyWorks.Networking.Client;
using AzyWorks.System.Services;

using DiscordBridge.CustomNetwork.RoleSync;
using DiscordBridge.CustomNetwork.Tickets;

using DiscordBridgePlugin.Core.Network;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordBridgePlugin.Core.RoleSync
{
    public class RoleSyncTicketsService : IService
    {
        public IServiceCollection Collection { get; set; }

        public Dictionary<string, RoleSyncTicket> Tickets { get; private set; }

        public NetworkService Network { get; private set; }
        public RoleSyncService RoleSync { get; private set; }

        public event Action<string, string> OnRoleUpdated;

        public event Action<RoleSyncTicket, RoleSyncTicketInvalidationReason> OnTicketInvalidated;
        public event Action<RoleSyncTicket, RoleSyncTicketValidationReason> OnTicketValidated;

        public bool IsValid()
            => true;

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
            Tickets = new Dictionary<string, RoleSyncTicket>();

            Network = Collection.GetService<NetworkService>();
            RoleSync = Collection.GetService<RoleSyncService>();

            NetClient.OnPayloadReceived += OnPayload;
        }

        public void Stop()
        {
            Tickets.Clear();

            Tickets = null;
            RoleSync = null;

            NetClient.OnPayloadReceived -= OnPayload;
        }

        public void OnPayload(NetPayload netPayload)
        {
            if (netPayload.Messages.Any(x => x is RoleSyncRoleMessage))
            {
                HandleRoleSyncRoleMessage((RoleSyncRoleMessage)netPayload.Messages.First(x => x is RoleSyncRoleMessage));
            }

            if (netPayload.Messages.Any(x => x is RoleSyncValidateTicketMessage))
            {
                HandleRoleSyncTicketValidation((RoleSyncValidateTicketMessage)netPayload.Messages.First(x => x is RoleSyncValidateTicketMessage));
            }

            if (netPayload.Messages.Any(x => x is RoleSyncInvalidateTicketMessage))
            {
                HandleRoleSyncTicketInvalidation((RoleSyncInvalidateTicketMessage)netPayload.Messages.First(x => x is RoleSyncInvalidateTicketMessage));
            }
        }

        public RoleSyncTicket Generate(ReferenceHub hub)
        {
            var ticket = new RoleSyncTicket(
                AzyWorks.System.RandomGenerator.Ticket(3),
                new RoleSyncAccount(hub.nicknameSync.Network_myNickSync, hub.characterClassManager.UserId, "<NONE>"),
                DateTime.Now,
                DateTime.Now);

            Tickets[hub.characterClassManager.UserId] = ticket;

            Log.Info($"Generated Role Sync ticket ({ticket.Code}) for {hub.nicknameSync.Network_myNickSync} ({hub.characterClassManager.UserId})");

            return ticket;
        }

        private void HandleRoleSyncRoleMessage(RoleSyncRoleMessage roleSyncRoleMessage)
        {
            OnRoleUpdated?.Invoke(roleSyncRoleMessage.UserId, roleSyncRoleMessage.NewRole);

            Log.Info($"Received a role update for {roleSyncRoleMessage.UserId}: {roleSyncRoleMessage.NewRole}");
        }

        private void HandleRoleSyncTicketValidation(RoleSyncValidateTicketMessage roleSyncValidateTicketMessage)
        {
            OnTicketValidated?.Invoke(roleSyncValidateTicketMessage.Ticket, roleSyncValidateTicketMessage.Reason);
            Tickets.Remove(roleSyncValidateTicketMessage.Ticket.Account.Id);

            Log.Info($"Ticket {roleSyncValidateTicketMessage.Ticket.Code} of user {roleSyncValidateTicketMessage.Ticket.Account.Id} validated ({roleSyncValidateTicketMessage.Reason})");
        }

        private void HandleRoleSyncTicketInvalidation(RoleSyncInvalidateTicketMessage roleSyncInvalidateTicketMessage)
        {
            OnTicketInvalidated?.Invoke(roleSyncInvalidateTicketMessage.Ticket, roleSyncInvalidateTicketMessage.Reason);
            Tickets.Remove(roleSyncInvalidateTicketMessage.Ticket.Account.Id);

            Log.Info($"Ticket {roleSyncInvalidateTicketMessage.Ticket.Code} of user {roleSyncInvalidateTicketMessage.Ticket.Account.Id} invalidated ({roleSyncInvalidateTicketMessage.Reason})");
        }
    }
}
