using CommandSystem;
using CommandSystem.Commands.RemoteAdmin;
using DiscordBridgePlugin.Core.PlayerCache;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBridgePlugin.Core.Patches
{
    [HarmonyPatch(typeof(UnbanCommand), nameof(UnbanCommand.Execute))]
    public static class UnbanCommandPatch
    {
        public static bool TryGetBanType(string value, out BanHandler.BanType banType)
        {
            if (value.Contains("@") || ulong.TryParse(value, out _))
            {
                banType = BanHandler.BanType.UserId;
                return true;
            }

            if (IPAddress.TryParse(value, out _))
            {
                banType = BanHandler.BanType.IP;
                return true;
            }

            banType = BanHandler.BanType.NULL;
            return false;
        }

        public static bool Prefix(UnbanCommand __instance, ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            if (!sender.CheckPermission(PlayerPermissions.LongTermBanning, out response))
                return false;

            if (arguments.Count < 1)
            {
                response = "To execute this command provide at least 1 argument!\nUsage: unban <value>";
                return false;
            }

            var value = string.Join(" ", arguments).Trim();
            if (!TryGetBanType(value, out var type))
            {
                response = "Failed to recognize ban type!";
                return false;
            }

            if (type is BanHandler.BanType.UserId)
            {
                BanHandler.RemoveBan(value, type, true);

                PlayerCacheEvents.RequestIdCache(value, x =>
                {
                    BanHandler.RemoveBan(x.Ip, BanHandler.BanType.IP, true);
                    sender.Respond($"Removed IP ban of {x.Ip}!");
                });

                response = $"Removed UID ban of {value}.";
                return false;
            }

            if (type is BanHandler.BanType.IP)
            {
                BanHandler.RemoveBan(value, type, true);

                PlayerCacheEvents.RequestIpCache(value, x =>
                {
                    BanHandler.RemoveBan(x.Id, BanHandler.BanType.UserId, true);
                    sender.Respond($"Removed UID ban of {x.Id}!");
                });

                response = $"Removed IP ban of {value}.";
                return false;
            }

            return false;
        }
    }
}
