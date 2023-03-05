using AzyWorks.Networking.Client;

using CommandSystem;

using DiscordBridge.CustomNetwork.Tickets;

using PluginAPI.Core;

using System;

namespace DiscordBridgePlugin.Core.RoleSync.Commands
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class LinkCommand : ICommand
    {
        public string Command => "link";
        public string Description => "Generates a ticket.";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!Player.TryGet(sender, out var player))
            {
                response = "Player::TryGet";
                return false;
            }

            var ticket = LoaderService.Loader.GetService<RoleSyncTicketsService>().Generate(player.ReferenceHub);

            NetClient.Send(new AzyWorks.Networking.NetPayload()
                .WithMessage(new RoleSyncTicketRequestMessage(ticket)));

            response = $"Ticket s ID \"{ticket.Code}\" generován. Použij ho příkazem \"link {ticket.Code}\" v příslušném kanále.";
            return true;
        }
    }
}