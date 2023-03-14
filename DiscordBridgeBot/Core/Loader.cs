using AzyWorks.Logging;
using AzyWorks.System.Services;

using DiscordBridgeBot.Core.Configuration;
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
            Services.AddService<LogService>("Core :: Loader");

            LoaderLog = Services.GetService<LogService>();

            AddSafeHandlers();

            LoaderLog.Info("Welcome!");
            LoaderLog.Info("Registering default services ..");

            Services.AddService<ConfigManagerService>(ConfigMainPath, new Type[] { typeof(LogService), typeof(Program), typeof(NetworkManagerService) });
            Services.AddService<NetworkManagerService>().StartListening();

            LoaderLog.Info("Services registered, starting the network!");

            await Task.Delay(-1);
        }

        private static void AddSafeHandlers()
        {
            Console.CancelKeyPress += ConsolePressHandler;

            if (AppDomain.CurrentDomain != null)
            {
                AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            }
        }

        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            LoaderLog.Error($"Unhandled exception: {e}");

            if (e.IsTerminating)
            {
                File.WriteAllText($"{Directory.GetCurrentDirectory()}/exception.txt", e.ToString());
            }     
        }

        private static void ConsolePressHandler(object sender, ConsoleCancelEventArgs ev)
        {
            LoaderLog.Info("Exiting ..");
        }
    }
}