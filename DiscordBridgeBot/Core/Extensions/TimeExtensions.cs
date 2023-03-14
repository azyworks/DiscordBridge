using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBridgeBot.Core.Extensions
{
    public static class TimeExtensions
    {
        public static long ToSeconds(this string time, int defaultFactor = 1)
        {
            if (long.TryParse(time, out var result))
            {
                return result * defaultFactor;
            }

            if (time.Length < 2)
            {
                throw new Exception($"{result} is not a valid time.");
            }

            if (!long.TryParse(time.Substring(0, time.Length - 1), out result))
            {
                throw new Exception($"{result} is not a valid time.");
            }

            switch (time[time.Length - 1])
            {
                case 'S':
                case 's':
                    return result;
                case 'm':
                    return result * 60;
                case 'H':
                case 'h':
                    return result * 3600;
                case 'D':
                case 'd':
                    return result * 86400;
                case 'M':
                    return result * 2592000;
                case 'Y':
                case 'y':
                    return result * 31536000;
                default:
                    throw new Exception($"{result} is not a valid time.");
            }
        }
    }
}