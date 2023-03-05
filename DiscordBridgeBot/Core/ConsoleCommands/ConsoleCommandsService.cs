using AzyWorks.System;
using AzyWorks.System.Services;

using DiscordBridgeBot.Core.Logging;

namespace DiscordBridgeBot.Core.ConsoleCommands
{
    public class ConsoleCommandsService : IService
    {
        private LogService _log;
        private Thread _captureThread;
        private HashSet<ConsoleCommandBase> _commands;

        public event Action<string> OnCommandCaptured;

        public const string CommandsNamespace = "DiscordBridgeBot.Core.ConsoleCommands.Commands";

        public IServiceCollection Collection { get; set; }

        public void Start(IServiceCollection collection, object[] initArgs)
        {
            _log = Collection.GetService<LogService>();
            _commands = new HashSet<ConsoleCommandBase>();

            CollectCommands();

            OnCommandCaptured += OnCommandCapturedHandler;

            StartCapture();
        }

        public void StartCapture()
        {
            _captureThread = new Thread(CaptureThread);
            _captureThread.Start();

            _log.Info("Started console commands capture - you can now use commands.");
        }

        private void OnCommandCapturedHandler(string input)
        {
            var args = input.Split(' ');
            var cmd = args[0].ToLower();
            var cmdHandler = _commands.FirstOrDefault(x => x.Name.ToLower() == cmd);

            if (cmdHandler is null)
            {
                _log.Error($"Failed to find a command by name {cmd}!");
                return;
            }

            cmdHandler.Execute(args.Skip(1).ToArray());
        }

        private void CollectCommands()
        {
            _log.Debug("Collecting console commands ..");

            foreach (var type in typeof(ConsoleCommandsService).Assembly.GetTypes())
            {
                if (type.Namespace != CommandsNamespace)
                    continue;

                var instance = Reflection.Instantiate<ConsoleCommandBase>(type);

                if (instance is null)
                    continue;

                _commands.Add(instance);
                _log.Debug($"Registered command: {instance}");
            }

            _log.Debug($"Registered {_commands.Count} commands.");
        }

        private void CaptureThread()
        {
            while (true)
            {
                var input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                    continue;

                OnCommandCaptured?.Invoke(input);
            }
        }

        public bool IsValid()
        {
            return true;
        }

        public void Stop()
        {
            _captureThread = null;
            _commands?.Clear();
            _commands = null;
        }
    }
}