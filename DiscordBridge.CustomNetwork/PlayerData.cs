namespace DiscordBridge.CustomNetwork
{
    public struct PlayerData
    {
        public string Username;
        public string UserId;
        public string Role;
        public string RoleName;
        public string Ip;

        public int PlayerId;

        public bool Partial;

        public PlayerData(string username, string userid, string role, string roleName, string ip, int playerId)
        {
            Username = username;
            UserId = userid;
            Role = role;
            RoleName = roleName;
            PlayerId = playerId;
            Ip = ip;

            Partial = false;
        }

        public PlayerData(string username, string userid)
        {
            Username = username;
            UserId = userid;
;
            Role = "";
            RoleName = "";
            Ip = "";

            PlayerId = -1;
            Partial = true;
        }
    }
}