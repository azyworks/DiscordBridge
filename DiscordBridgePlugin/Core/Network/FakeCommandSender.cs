using AzyWorks.Networking.Client;

using DiscordBridge.CustomNetwork.RemoteAdmin;

namespace DiscordBridgePlugin.Core.Network
{
    public class FakeCommandSender : CommandSender
    {
        private string _senderName;
        private string _senderId;

        public FakeCommandSender(string senderName, string senderId)
        {
            _senderName = senderName;
            _senderId = senderId;
        }

        public override string SenderId => _senderId;
        public override string Nickname => _senderName;

        public override ulong Permissions => ulong.MaxValue;

        public override byte KickPower => byte.MaxValue;

        public override bool FullPermissions => true;

        public override void Print(string text)
        {
            NetClient.Send(new AzyWorks.Networking.NetPayload()
                .WithMessage(new RemoteAdminExecuteResponseMessage("", _senderName, _senderId, text, true)));
        }

        public override void RaReply(string text, bool success, bool logToConsole, string overrideDisplay)
        {
            NetClient.Send(new AzyWorks.Networking.NetPayload()
                .WithMessage(new RemoteAdminExecuteResponseMessage("", _senderName, _senderId, text, success)));
        }
    }
}