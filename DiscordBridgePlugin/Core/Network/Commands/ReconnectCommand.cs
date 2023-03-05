using CommandSystem;

using System;

namespace DiscordBridgePlugin.Core.Network.Commands
{
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class ReconnectCommand : ICommand
    {
        public string Command => "db_reconnect";
        public string Description => "Forces a plugin reconnection.";

        public string[] Aliases { get; } = Array.Empty<string>();

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            LoaderService.Loader.GetService<NetworkService>().Reconnect();

            response = "Reconnection forced.";
            return true;
        }
    }
}
