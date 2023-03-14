using AzyWorks;
using AzyWorks.Logging;
using AzyWorks.System.Services;

using DiscordBridgeBot.Core.Configuration;
using DiscordBridgeBot.Core.Logging;
using DiscordBridgeBot.Core.Network;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;

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
            LogStream.OnMessageLogged += (x, y, z) =>
            {
                if (!File.Exists($"{Directory.GetCurrentDirectory()}/last_log.txt"))
                    File.Create($"{Directory.GetCurrentDirectory()}/last_log.txt").Close();

                File.AppendAllText($"{Directory.GetCurrentDirectory()}/last_log.txt", $"{x} {y} {z}\n");
            };

            Log.BlacklistedLevels.Clear();
            Log.BlacklistedSources.Clear();

            Services = new ServiceCollection();
            Services.AddService<LogService>("Core :: Loader");

            LoaderLog = Services.GetService<LogService>();

            LoaderLog.Info("Welcome!");
            LoaderLog.Info("Registering default services ..");

            Services.AddService<ConfigManagerService>(ConfigMainPath, new Type[] { typeof(LogService), typeof(Program), typeof(NetworkManagerService) });
            Services.AddService<NetworkManagerService>();

            LoaderLog.Info("Services registered, starting the network!");

            Services.GetService<NetworkManagerService>().StartListening();

            if (AppDomain.CurrentDomain != null)
            {
                AppDomain.CurrentDomain.FirstChanceException += OnExceptionCaptured;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            }

            await Task.Delay(-1);
        }

        public static void OnUnhandledException(object sender, UnhandledExceptionEventArgs ev)
        {
            LoaderLog.Error("Unhandled exception!");
            LoaderLog.Error(ev.ExceptionObject);

            if (ev.IsTerminating)
                LoaderLog.Error("The bot will now terminate.");

            File.WriteAllText($"{Directory.GetCurrentDirectory()}/last_exception.txt", ev.ExceptionObject.ToString());
        }

        public static void OnExceptionCaptured(object sender, FirstChanceExceptionEventArgs ev)
        {
            if (ev.Exception is ObjectDisposedException || 
                (ev.Exception is IOException && ev.Exception.InnerException != null && ev.Exception.InnerException is ObjectDisposedException) ||
                (ev.Exception is IOException && ev.Exception.InnerException != null && ev.Exception.InnerException is SocketException socketException && socketException.ErrorCode == 125) ||
                (ev.Exception is IOException && ev.Exception.InnerException != null && ev.Exception.InnerException is SocketException exception && exception.ErrorCode == 104))
                return;

            LoaderLog.Error("Captured an exception!");
            LoaderLog.Error(ev.Exception);

            File.WriteAllText($"{Directory.GetCurrentDirectory()}/last_exception.txt", ev.Exception.ToString());
        }
    }
}