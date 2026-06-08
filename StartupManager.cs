namespace DesktopIconManager;

public static class StartupManager
{
    private const string ShortcutName = "桌面图标整理-自动整理.lnk";

    public static string ShortcutPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        ShortcutName);

    public static bool IsEnabled() => File.Exists(ShortcutPath);

    public static void SetEnabled(bool enabled, string profileName)
    {
        if (enabled)
        {
            CreateShortcut(profileName);
            return;
        }

        if (File.Exists(ShortcutPath))
        {
            File.Delete(ShortcutPath);
        }
    }

    private static void CreateShortcut(string profileName)
    {
        var executablePath = Application.ExecutablePath;
        var startupDirectory = Path.GetDirectoryName(ShortcutPath);
        if (!string.IsNullOrWhiteSpace(startupDirectory))
        {
            Directory.CreateDirectory(startupDirectory);
        }

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("无法创建 Windows 快捷方式。");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("无法创建 Windows 快捷方式。");
        dynamic shortcut = shell.CreateShortcut(ShortcutPath);
        shortcut.TargetPath = executablePath;
        shortcut.WorkingDirectory = AppContext.BaseDirectory;
        shortcut.Arguments = $"--auto-arrange --profile \"{profileName.Replace("\"", "\\\"")}\"";
        shortcut.IconLocation = $"{executablePath},0";
        shortcut.Description = "开机自动整理桌面图标";
        shortcut.Save();
    }
}

