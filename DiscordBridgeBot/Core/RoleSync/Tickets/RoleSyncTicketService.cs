using AzyWorks.Services;

using DiscordBridge.CustomNetwork.PluginMessages.Tickets;
using DiscordBridge.CustomNetwork.RoleSync;
using DiscordBridge.CustomNetwork.ServerMessages.Tickets;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.Network;

namespace DiscordBridgeBot.Core.RoleSync.Tickets
{
    public class RoleSyncTicketService : ServiceBase
    {
        private LogService _log;

        public HashSet<RoleSyncTicket> ActiveTickets { get; } = new HashSet<RoleSyncTicket>();
        public HashSet<RoleSyncTicket> TimedOutTickets { get; } = new HashSet<RoleSyncTicket>();

        public NetworkService Network { get; private set; }
        public RoleSyncService RoleSync { get; private set; }

        public event Action<RoleSyncTicket, RoleSyncTicketInvalidationReason> OnTicketInvalidated;
        public event Action<RoleSyncTicket, ulong, RoleSyncTicketValidationReason> OnTicketValidated;

        public event Action<RoleSyncTicket> OnTicketReceived;
        public event Action<RoleSyncTicket> OnTicketDeleted;

        public override void Setup(object[] args)
        {
            Task.Run(async () => await CleanTicketsAsync());
            Task.Run(async () => await DeteorateTicketsAsync());

            RoleSync = Collection.GetService<RoleSyncService>();
            Network = Collection.GetService<NetworkService>();
            Network.Client.AddCallback<RoleSyncTicketRequestMessage>(HandleRoleSyncTicketRequest);

            _log = Collection.GetService<LogService>();
        }

        public override void Destroy()
        {
          
        }

        public void InvalidateTicket(RoleSyncTicket ticket, RoleSyncTicketInvalidationReason reason = RoleSyncTicketInvalidationReason.TimedOut)
        {
            lock (ActiveTickets)
            {
                ActiveTickets.Remove(ticket);

                lock (TimedOutTickets)
                {
                    if (reason is RoleSyncTicketInvalidationReason.TimedOut)
                    {
                        ticket.ChangedAt = DateTime.Now;

                        TimedOutTickets.Add(ticket);
                    }
                }
            }

            Network.Client.Send(new AzyWorks.Networking.NetPayload()
                .WithMessage(new RoleSyncInvalidateTicketMessage(ticket, reason)));

            OnTicketInvalidated?.Invoke(ticket, reason);

            _log.Info($"Invalidated role sync ticket {ticket.Code}: {reason}");
        }

        public void ValidateTicket(RoleSyncTicket ticket, ulong validatedBy, RoleSyncTicketValidationReason reason = RoleSyncTicketValidationReason.UserVerified)
        {
            lock (ActiveTickets)
            {
                lock (TimedOutTickets)
                {
                    ActiveTickets.Remove(ticket);
                    TimedOutTickets.Remove(ticket);
                }
            }

            Network.Client.Send(new AzyWorks.Networking.NetPayload()
                .WithMessage(new RoleSyncValidateTicketMessage(ticket, reason)));

            OnTicketValidated?.Invoke(ticket, validatedBy, reason);

            _log.Info($"Validated role sync ticket {ticket.Code}: {reason} by {validatedBy}");
        }

        private void HandleRoleSyncTicketRequest(RoleSyncTicketRequestMessage roleSyncTicketRequestMessage)
        {
            ActiveTickets.Add(roleSyncTicketRequestMessage.Ticket);

            OnTicketReceived?.Invoke(roleSyncTicketRequestMessage.Ticket);

            _log.Info($"Received a role sync ticket for {roleSyncTicketRequestMessage.Ticket.Account.Id}: {roleSyncTicketRequestMessage.Ticket.Code}");
        }

        private async Task DeteorateTicketsAsync()
        {
            while (true)
            {
                await Task.Delay(1000);

                foreach (var ticket in ActiveTickets)
                {
                    if ((DateTime.Now - ticket.CreatedAt).Minutes > 5)
                    {
                        InvalidateTicket(ticket);
                    }
                }
            }
        }

        private async Task CleanTicketsAsync()
        {
            var toRemove = new HashSet<RoleSyncTicket>();

            while (true)
            {
                await Task.Delay(1000);

                foreach (var ticket in TimedOutTickets)
                {
                    if ((DateTime.Now - ticket.ChangedAt).Minutes > 3)
                    {
                        toRemove.Add(ticket);
                    }
                }

                lock (TimedOutTickets)
                {
                    foreach (var toRemoveTicket in toRemove)
                    {
                        TimedOutTickets.RemoveWhere(x => x.Code == toRemoveTicket.Code);

                        OnTicketDeleted?.Invoke(toRemoveTicket);
                    }
                }

                toRemove.Clear();
            }
        }
    }
}
