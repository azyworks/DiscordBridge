using DiscordBridgeBot.Core.Network;

namespace DiscordBridgeBot.Core.ConsoleCommands.Commands
{
    public class LinkCommand : ConsoleCommandBase
    {
        public override string Name => "link";

        public override void Execute(string[] input)
        {
            if (input.Length != 3)
            {
                Respond("Usage: link <userId> <roleId> <serverPort>");
                return;
            }

            var id = input[0];
            var role = input[1];
            var server = int.Parse(input[2]);

            if (Program.Services.TryGetService(out NetworkManagerService networkManagerService))
            {
                if (networkManagerService.TryGetServer(server, out var serverService))
                {
                    if (!serverService.RoleSync.TryGetAccount(id, out var account))
                        serverService.RoleSync.LinkAccount(id, null);

                    if (serverService.RoleSync.TryGetRole(role, out var roleSync))
                    {
                        serverService.RoleSync.TryGetAccount(id, out account);
                        serverService.RoleSync.LinkRole(account, roleSync);

                        Respond("Role linked!");
                    }
                    else
                    {
                        Respond($"Role \"{role}\" does not exist.");
                    }
                }
                else
                {
                    Respond($"Server \"{server}\" has not connected yet.");
                }
            }
            else
            {
                Respond($"Failed to retrieve the network manager service.");
            }
        }
    }
}
