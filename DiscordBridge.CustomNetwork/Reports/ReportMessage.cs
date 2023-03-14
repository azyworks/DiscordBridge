using AzyWorks.Networking;

using System.IO;

namespace DiscordBridge.CustomNetwork.Reports
{
    public struct ReportMessage : INetMessage
    {
        public string ReporterName;
        public string ReportedName;

        public string ReporterId;
        public string ReportedId;

        public string ReporterIp;
        public string ReportedIp;

        public string ReporterRole;
        public string ReportedRole;

        public string ReporterRoom;
        public string ReportedRoom;

        public string Reason;

        public int ReporterPlayerId;
        public int ReportedPlayerId;

        public bool IsCheaterReport;

        public ReportMessage(
            string reporterName, 
            string reportedName, 

            string reporterId, 
            string reportedId, 

            string reporterIp, 
            string reportedIp, 

            string reporterRole, 
            string reportedRole, 

            string reporterRoom, 
            string reportedRoom, 
            
            int reportedPlayerId, 
            int reporterPlayerId,
            
            string reason, 
            
            bool isCheaterReport)
        {
            ReporterName = reporterName ?? "Unknown";
            ReportedName = reportedName ?? "Unknown";

            ReporterId = reporterId ?? "Unknown";
            ReportedId = reportedId ?? "Unknown";

            ReporterIp = reporterIp ?? "Unknown";
            ReportedIp = reportedIp ?? "Unknown";

            ReporterRole = reporterRole ?? "Unknown";
            ReportedRole = reportedRole ?? "Unknown";

            ReporterRoom = reporterRoom ?? "Unknown";
            ReportedRoom = reportedRoom ?? "Unknown";

            ReportedPlayerId = reportedPlayerId;
            ReporterPlayerId = reporterPlayerId;

            Reason = reason ?? "Unknown";

            IsCheaterReport = isCheaterReport;
        }

        public void Deserialize(BinaryReader reader)
        {
            ReporterName = reader.ReadString();
            ReportedName = reader.ReadString();

            ReporterId = reader.ReadString();
            ReportedId = reader.ReadString();

            ReporterIp = reader.ReadString();
            ReportedIp = reader.ReadString();

            ReporterRole = reader.ReadString();
            ReportedRole = reader.ReadString();

            ReporterRoom = reader.ReadString();
            ReportedRoom = reader.ReadString();

            ReportedPlayerId = reader.ReadInt32();
            ReporterPlayerId = reader.ReadInt32();

            Reason = reader.ReadString();

            IsCheaterReport = reader.ReadBoolean();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ReporterName);
            writer.Write(ReportedName);

            writer.Write(ReporterId);
            writer.Write(ReportedId);

            writer.Write(ReporterIp);
            writer.Write(ReportedIp);

            writer.Write(ReporterRole);
            writer.Write(ReportedRole);

            writer.Write(ReporterRoom);
            writer.Write(ReportedRoom);

            writer.Write(ReportedPlayerId);
            writer.Write(ReporterPlayerId);

            writer.Write(Reason);

            writer.Write(IsCheaterReport);
        }
    }
}