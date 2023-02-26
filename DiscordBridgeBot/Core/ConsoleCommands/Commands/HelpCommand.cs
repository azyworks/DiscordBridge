
namespace DiscordBridgeBot.Core.ConsoleCommands.Commands
{
    public class HelpCommand : ConsoleCommandBase
    {
        public override string Name => "help";
        public override void Execute(string[] input)
        {
            Respond("Hello!");
        }
    }
}