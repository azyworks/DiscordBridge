using AzyWorks;
using AzyWorks.System.Services;

using DiscordBridgeBot.Core.Configuration;

namespace DiscordBridgeBot.Core.Logging
{
    public class LogService : IService
    {
        public string Name;

        [Config("LogService.Debug", "Whether or not to show lengthy debug messages.")]
        public static bool DebugTogle;

        public IServiceCollection Collection { get; set; }

        public void Start(IServiceCollection collection, object[] initArgs)
        {
            Name = (string)initArgs[0];
        }

        public void Info(object message) => Log.SendInfo(Name, message);
        public void Warn(object message) => Log.SendWarn(Name, message);    
        public void Error(object message) => Log.SendError(Name, message);
        public void Debug(object message)
        {
            if (!DebugTogle)
                return;

            Log.SendDebug(Name, message);
        }

        public bool IsValid()
        {
            return true;
        }

        public void Stop()
        {

        }
    }
}