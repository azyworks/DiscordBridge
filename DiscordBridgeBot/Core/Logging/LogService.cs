using AzyWorks;
using AzyWorks.Services;

using DiscordBridgeBot.Core.Configuration;

namespace DiscordBridgeBot.Core.Logging
{
    public class LogService : ServiceBase
    {
        public string Name;

        [Config("LogService.Debug", "Whether or not to show lengthy debug messages.")]
        public static bool DebugTogle;

        public override void Setup(object[] args)
        {
            Name = (string)args[0];
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
    }
}