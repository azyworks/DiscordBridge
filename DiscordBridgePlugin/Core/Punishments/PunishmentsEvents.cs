using PluginAPI.Enums;
using PluginAPI.Core.Attributes;

using AzyWorks.Networking.Client;
using AzyWorks.Networking;

using DiscordBridge.CustomNetwork.Punishments;

using PluginAPI.Core;
using DiscordBridge.CustomNetwork.Reports;

namespace DiscordBridgePlugin.Core.Punishments
{
    public class PunishmentsEvents
    {
        public void ReportPlayer(Player player, Player target, string reason, bool isCheater)
        {
            NetClient.Send(new NetPayload()
                .WithMessage(new ReportMessage(
                    player.Nickname,
                    target.Nickname,

                    player.UserId,
                    target.UserId,

                    player.IpAddress,
                    target.IpAddress,

                    player.ReferenceHub.roleManager.CurrentRole.RoleName ?? player.Role.ToString(),
                    target.ReferenceHub.roleManager.CurrentRole.RoleName ?? player.Role.ToString(),

                    (player.Room?.Name ?? MapGeneration.RoomName.Unnamed).ToString(),
                    (target.Room?.Name ?? MapGeneration.RoomName.Unnamed).ToString(),

                    target.PlayerId,
                    player.PlayerId,

                    reason,

                    isCheater)));
        }

        [PluginEvent(ServerEventType.PlayerCheaterReport)]
        public void OnCheaterReported(Player player, Player target, string reason)
        {
            ReportPlayer(player, target, reason, true);
        }

        [PluginEvent(ServerEventType.PlayerReport)]
        public void OnReported(Player player, Player target, string reason)
        {
            ReportPlayer(player, target, reason, false);
        }

        [PluginEvent(ServerEventType.BanIssued)]
        public void OnBanIssued(BanDetails banDetails, BanHandler.BanType banType)
        {
            if (banType is BanHandler.BanType.IP || banType is BanHandler.BanType.NULL)
                return;

            var id = ExtractIssuerId(banDetails);
            var name = ExtractIssuerName(banDetails, id);

            Log.Info($"Extracted ID {id} from {banDetails.Issuer}");
            Log.Info($"Extracted Name {name} from {banDetails.Issuer}");

            NetClient.Send(new NetPayload()
                .WithMessages(new PunishmentIssuedMessage(
                    id,
                    name,

                    banDetails.OriginalName,
                    banDetails.Id,

                    "UNKNOWN",

                    banDetails.Reason,

                    new System.DateTime(banDetails.IssuanceTime),
                    new System.DateTime(banDetails.Expires),

                    PunishmentType.Ban)));

            Log.Info($"Sent punishment log ({id};{name};{banDetails.Id};{banDetails.OriginalName};{banDetails.Reason};{banDetails.IssuanceTime};{banDetails.Expires})");
        }

        private string ExtractIssuerName(BanDetails banDetails, string id)
        {
            if (banDetails.Issuer == "SERVER CONSOLE")
                return "Dedicated Server";

            return banDetails.Issuer.Replace($"({id})", "");
        }

        private string ExtractIssuerId(BanDetails banDetails)
        {
            if (banDetails.Issuer == "SERVER CONSOLE")
                return "ID_Host";

            var firstBracketIndex = banDetails.Issuer.LastIndexOf('(');
            var secondBracketIndex = banDetails.Issuer.LastIndexOf(')');

            if (firstBracketIndex is -1 || secondBracketIndex is -1)
                return "ID_Unknown";

            return banDetails.Issuer
                .Substring(firstBracketIndex, secondBracketIndex - firstBracketIndex)
                .Replace("(", "")
                .Replace(")", "");
        }
    }
}