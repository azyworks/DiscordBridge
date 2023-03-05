using DiscordBridgeBot.Core.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBridgeBot.Core.ConsoleCommands.Commands
{
    public class RoleCommand : ConsoleCommandBase
    {
        public override string Name => "role";

        public override void Execute(string[] input)
        {
            if (input.Length < 2)
            {
                Respond($"role <server> <add/remove>");
                return;
            }

            var serverId = int.Parse(input[0]);

            if (Program.Services.TryGetService(out NetworkManagerService networkManagerService))
            {
                if (networkManagerService.TryGetServer(serverId, out var server))
                {
                    if (input[1].ToLower() == "add")
                    {
                        if (input.Length < 4)
                        {
                            Respond($"role <server> add <role> <applicable IDs> <name>");
                            return;
                        }

                        var role = input[2];
                        var idStr = input[3];
                        var idS = idStr.Split(',').Select(x => ulong.Parse(x));
                        var name = string.Join(" ", input.Skip(4));

                        server.RoleSync.UpdateRole(role, name, idS);

                        Respond($"Added role {role} ({name}) with IDs: {string.Join(", ", idS)}");
                    }
                    else
                    {
                        if (input.Length < 3)
                        {
                            Respond($"role <server> remove <role>");
                            return;
                        }

                        var role = input[2];

                        server.RoleSync.RemoveRole(role);

                        Respond($"Removed role {role}");
                    }
                }
                else
                {
                    Respond($"Server \"{serverId}\" has not connected yet.");
                }
            }
            else
            {
                Respond($"Failed to retrueve the network manager service.");
            }
        }
    }
}
