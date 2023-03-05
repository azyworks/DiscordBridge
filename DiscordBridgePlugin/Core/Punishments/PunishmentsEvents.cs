using PluginAPI.Enums;
using PluginAPI.Core.Attributes;

using AzyWorks.Networking.Client;
using AzyWorks.Networking;

using DiscordBridge.CustomNetwork.Punishments;
using DiscordBridge.CustomNetwork;

using AzyWorks.Extensions;

using NorthwoodLib.Pools;

namespace DiscordBridgePlugin.Core.Punishments
{
    public class PunishmentsEvents
    {
        public const int SteamIdLength = 17;
        public const int DiscordIdLength = 18;

        public PunishmentsService PunishmentsService { get; }

        public PunishmentsEvents()
        {
            PunishmentsService = LoaderService.Loader.GetService<PunishmentsService>();
        }

        [PluginEvent(ServerEventType.BanIssued)]
        public void OnBanIssued(BanDetails banDetails, BanHandler.BanType banType)
        {
            if (banType is BanHandler.BanType.IP || banType is BanHandler.BanType.NULL)
                return;

            NetClient.Send(new NetPayload()
                .WithMessage(new PlayerBannedMessage(
                    new PlayerData(ExtractIssuerName(banDetails), ExtractIssuerId(banDetails)),
                    new PlayerData(banDetails.OriginalName, banDetails.Id),

                    banDetails.Reason,

                    new System.DateTime(banDetails.IssuanceTime),
                    new System.DateTime(banDetails.Expires))));
        }

        private string ExtractIssuerName(BanDetails banDetails)
        {
            if (banDetails.Issuer == "SERVER CONSOLE")
                return "Dedicated Server";

            return banDetails.Issuer.CutToIndex(banDetails.Issuer.LastIndexOf('(') - 1);
        }

        private string ExtractIssuerId(BanDetails banDetails)
        {
            if (banDetails.Issuer == "SERVER CONSOLE")
                return "ID_Host";

            var issuer = banDetails.Issuer;
            var firstBracketIndex = issuer.LastIndexOf('(');

            firstBracketIndex++;

            int length = SteamIdLength;

            if (issuer.EndsWith("@discord)"))
                length = DiscordIdLength;

            var clean = StringBuilderPool.Shared.Rent();

            for (int i = 0; i < length; i++)
            {
                clean[i] = issuer[firstBracketIndex];
                firstBracketIndex++;
            }

            var cleanStr = clean.ToString();

            if (cleanStr.Length == SteamIdLength)
                cleanStr += "@steam";
            else
                cleanStr += "@discord";

            StringBuilderPool.Shared.Return(clean);

            return cleanStr;
        }
    }
}