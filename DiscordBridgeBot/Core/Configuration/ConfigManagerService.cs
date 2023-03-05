using AzyWorks.Configuration;
using AzyWorks.Configuration.Converters.Yaml;
using AzyWorks.System.Services;

using DiscordBridgeBot.Core.Logging;

namespace DiscordBridgeBot.Core.Configuration
{
    public class ConfigManagerService : IService
    {
        private LogService _log;
        private ConfigHandler _configHandler;
        private bool _typesRegistered;

        public string Path;
        public Type[] Types;

        public ConfigHandler ConfigHandler { get => _configHandler; }

        public IServiceCollection Collection { get; set; }

        public void Start(IServiceCollection collection, object[] initArgs)
        {
            Path = (string)initArgs[0];
            Types = (Type[])initArgs[1];

            _configHandler = new ConfigHandler(new YamlConfigConverter());
            _log = Collection.GetService<LogService>();

            ReloadAll();
        }

        public void Stop()
        {
            _configHandler?.SaveToFile(Path);
            _log?.Info("Configuration files saved.");

            _typesRegistered = false;
            _configHandler = null;
            _log = null;

            Path = null;
            Types = null;
        }

        public void ReloadAll()
        {
            _log.Info("Reloading configuration files ..");

            if (!_typesRegistered && Types != null)
            {
                for (int i = 0; i < Types.Length; i++)
                    _configHandler.RegisterConfigs(Types[i]);

                _typesRegistered = true;
                _log.Debug($"Registered config handlers.");
            }

            if (!File.Exists(Path))
                _configHandler.SaveToFile(Path);
            else
            {
                _configHandler.LoadFromFile(Path);
                _configHandler.SaveToFile(Path);
            }

            _log.Info("Configuration files reloaded!");
        }

        public bool IsValid()
        {
            return true;
        }
    }
}