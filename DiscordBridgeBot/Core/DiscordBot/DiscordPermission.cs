namespace DiscordBridgeBot.Core.DiscordBot
{
    public enum DiscordPermission
    {
        None,

        Administrator,

        Linking,
        RoleSyncManagement,

        PlayerCacheAccess,

        WhitelistManagement,

        SshExecute,
        RemoteAdminExecute,

        BanManagement,
        BanEditing,

        UploadSchematics,
        UploadNwApiPlugins,
        UploadExiledPlugins,

        ServerStart,
        ServerStop,
        ServerRestart
    }
}