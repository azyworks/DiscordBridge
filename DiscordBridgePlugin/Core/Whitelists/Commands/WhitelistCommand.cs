using CommandSystem;

using System;

namespace DiscordBridgePlugin.Core.Whitelists.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class WhitelistCommand : ICommand
    {
        public string Command { get; } = "whitelist";

        public string[] Aliases { get; } = Array.Empty<string>();

        public string Description { get; } = "";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var whService = LoaderService.Loader.GetService<WhitelistsService>();
            if (whService is null)
            {
                response = "The Whitelists service is not active.";
                return false;
            }

            if (arguments.Count < 1)
            {
                response = "whitelist <true/false/user ID>";
                return false;
            }

            var value = string.Join(" ", arguments);

            if (bool.TryParse(value, out var newState))
            {
                whService.IsActive = newState;
                whService.Sync();

                response = !whService.IsActive ? "Whitelist disabled." : "Whitelist enabled.";
                return true;
            }
            else
            {
                if (!whService.Whitelisted.Contains(value))
                {
                    whService.Whitelisted.Add(value);
                    whService.Save();
                    whService.Sync();

                    response = $"Whitelisted {value}";
                    return true;
                }
                else
                {
                    whService.Whitelisted.Remove(value);
                    whService.Save();
                    whService.Sync();

                    response = $"Removed whitelist for {value}";
                    return true;
                }
            }
        }
    }
}
