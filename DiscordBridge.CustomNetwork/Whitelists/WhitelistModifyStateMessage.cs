using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.Whitelists
{
    public struct WhitelistModifyStateMessage : INetMessage
    {
        public bool NewState;

        public WhitelistModifyStateMessage(bool newState)
        {
            NewState = newState;
        }

        public void Deserialize(BinaryReader reader)
        {
            NewState = reader.ReadBoolean();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NewState);
        }
    }
}