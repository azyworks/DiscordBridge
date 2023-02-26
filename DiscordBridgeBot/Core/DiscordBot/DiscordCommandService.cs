using AzyWorks.Services;

using DiscordBridgeBot.Core.Logging;

namespace DiscordBridgeBot.Core.DiscordBot
{
    public class DiscordCommandService 
    {
        private LogService _log;

        public ServiceCollectionBase Services { get; private set; }
        public DiscordService Discord { get; private set; }

        public DiscordCommandService(ServiceCollectionBase serviceCollectionBase)
        {
            Services = serviceCollectionBase;
            Discord = serviceCollectionBase.GetService<DiscordService>();
        }
    }
}
