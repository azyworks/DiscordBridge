using AzyWorks.System.Services;

using PluginAPI.Events;

namespace DiscordBridgePlugin.Core.Punishments
{
    public class PunishmentsService : IService
    {
        public IServiceCollection Collection { get; set; }

        public bool IsValid()
        {
            return true;
        }

        public void Start(IServiceCollection serviceCollection, params object[] initArgs)
        {
            EventManager.RegisterEvents<PunishmentsEvents>(Collection);
        }

        public void Stop()
        {
            EventManager.UnregisterEvents<PunishmentsEvents>(Collection);
        }
    }
}
