namespace DiscordBridgeBot.Core.ConsoleCommands
{
    public class ConsoleCommandBase
    {
        public virtual string Name { get; }

        public virtual void Execute(string[] input) { }

        public void Respond(object message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(">>> ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"[{Name}] ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{message}\n");
            Console.ResetColor();
        }
    }
}