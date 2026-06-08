namespace DesktopIconManager;

public static class AppPaths
{
    public static string AppDataDirectory
    {
        get
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "DesktopIconManager");
            Directory.CreateDirectory(directory);
            return directory;
        }
    }

    public static string ProfilesPath => Path.Combine(AppDataDirectory, "profiles.json");

    public static string SettingsPath => Path.Combine(AppDataDirectory, "settings.json");

    public static string SnapshotsPath => Path.Combine(AppDataDirectory, "snapshots.json");

    public static string PendingOperationPath => Path.Combine(AppDataDirectory, "pending-operation.json");

    public static string LogPath => Path.Combine(AppDataDirectory, "desktop-icon-manager.log");

    public static string ShortcutIconBackupsPath => Path.Combine(AppDataDirectory, "shortcut-icon-backups.json");

    public static string FileOrganizeOperationsPath => Path.Combine(AppDataDirectory, "file-organize-operations.json");

    public static string DiagnosticsDirectory
    {
        get
        {
            var directory = Path.Combine(AppDataDirectory, "diagnostics");
            Directory.CreateDirectory(directory);
            return directory;
        }
    }
}
