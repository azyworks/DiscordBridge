using AzyWorks.Logging;
using AzyWorks.System.Services;

using DiscordBridgeBot.Core.Configuration;
using DiscordBridgeBot.Core.ConsoleCommands;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.Network;

namespace DiscordBridgeBot.Core
{
    public static class Program
    {
        public static readonly string ConfigFolder = $"{Directory.GetCurrentDirectory()}/Configuration";
        public static readonly string ConfigServersFolder = $"{Directory.GetCurrentDirectory()}/Configuration/Servers";
        public static readonly string ConfigMainPath = $"{ConfigFolder}/Main.ini";

        public static LogService LoaderLog;
        public static IServiceCollection Services;

        static Program()
        {
            if (!Directory.Exists(ConfigFolder)) Directory.CreateDirectory(ConfigFolder);
            if (!Directory.Exists(ConfigServersFolder)) Directory.CreateDirectory(ConfigServersFolder);
        }

        public static async Task Main(string[] args)
        {
            LogStream.LogToConsole();

            Services = new ServiceCollection();
            Services.AddService<LogService>("Core::Loader");

            LoaderLog = Services.GetService<LogService>();

            LoaderLog.Info("Welcome!");
            LoaderLog.Info("Registering default services ..");

            Services.AddService<ConfigManagerService>(ConfigMainPath, new Type[] { typeof(LogService), typeof(Program), typeof(NetworkManagerService) });
            Services.AddService<NetworkManagerService>();
            Services.AddService<ConsoleCommandsService>();

            LoaderLog.Info("Services registered, starting the network!");

            Services.GetService<NetworkManagerService>().StartListening();

            await Task.Delay(-1);
        }
    }
}