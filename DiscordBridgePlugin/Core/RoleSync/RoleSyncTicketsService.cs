using AzyWorks.Networking.Client;
using AzyWorks.System.Services;

using DiscordBridge.CustomNetwork.RoleSync;
using DiscordBridge.CustomNetwork.Tickets;

using DiscordBridgePlugin.Core.Network;

using System;
using System.Collections.Generic;

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

            NetClient.AddCallback<RoleSyncRoleMessage>(HandleRoleSyncRoleMessage);
            NetClient.AddCallback<RoleSyncValidateTicketMessage>(HandleRoleSyncTicketValidation);
            NetClient.AddCallback<RoleSyncInvalidateTicketMessage>(HandleRoleSyncTicketInvalidation);
        }

        public void Stop()
        {
            Tickets.Clear();

            Tickets = null;
            RoleSync = null;
        }

        public RoleSyncTicket Generate(ReferenceHub hub)
        {
            var ticket = new RoleSyncTicket(
                AzyWorks.System.RandomGenerator.Ticket(3),
                new RoleSyncAccount(hub.nicknameSync.Network_myNickSync, hub.characterClassManager.UserId, "<NONE>"),
                DateTime.Now,
                DateTime.Now);

            Tickets[hub.characterClassManager.UserId] = ticket;

            return ticket;
        }

        private void HandleRoleSyncRoleMessage(RoleSyncRoleMessage roleSyncRoleMessage)
        {
            OnRoleUpdated?.Invoke(roleSyncRoleMessage.UserId, roleSyncRoleMessage.NewRole);
        }

        private void HandleRoleSyncTicketValidation(RoleSyncValidateTicketMessage roleSyncValidateTicketMessage)
        {
            OnTicketValidated?.Invoke(roleSyncValidateTicketMessage.Ticket, roleSyncValidateTicketMessage.Reason);
            Tickets.Remove(roleSyncValidateTicketMessage.Ticket.Account.Id);
        }

        private void HandleRoleSyncTicketInvalidation(RoleSyncInvalidateTicketMessage roleSyncInvalidateTicketMessage)
        {
            OnTicketInvalidated?.Invoke(roleSyncInvalidateTicketMessage.Ticket, roleSyncInvalidateTicketMessage.Reason);
            Tickets.Remove(roleSyncInvalidateTicketMessage.Ticket.Account.Id);
        }
    }
}
