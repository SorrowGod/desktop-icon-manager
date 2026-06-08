namespace DesktopIconManager;

public static class ShortcutIconManager
{
    public static ShortcutDetails? TryRead(string? shortcutPath)
    {
        if (string.IsNullOrWhiteSpace(shortcutPath) ||
            !File.Exists(shortcutPath) ||
            !string.Equals(Path.GetExtension(shortcutPath), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType is null)
            {
                return null;
            }

            dynamic shell = Activator.CreateInstance(shellType)!;
            dynamic shortcut = shell.CreateShortcut(shortcutPath);
            string targetPath = shortcut.TargetPath;
            string iconLocation = shortcut.IconLocation;
            return new ShortcutDetails(
                shortcutPath,
                targetPath,
                File.Exists(targetPath) || Directory.Exists(targetPath),
                iconLocation);
        }
        catch
        {
            return null;
        }
    }

    public static ShortcutIconBackup? CreateBackup(string shortcutPath)
    {
        var details = TryRead(shortcutPath);
        if (details is null)
        {
            return null;
        }

        var (path, index) = ParseIconLocation(details.IconLocation);
        return new ShortcutIconBackup
        {
            ShortcutPath = shortcutPath,
            OriginalIconPath = path,
            OriginalIconIndex = index,
            CreatedAt = DateTime.Now,
        };
    }

    private static (string Path, int Index) ParseIconLocation(string iconLocation)
    {
        if (string.IsNullOrWhiteSpace(iconLocation))
        {
            return (string.Empty, 0);
        }

        var parts = iconLocation.Split(',');
        if (parts.Length == 1)
        {
            return (parts[0], 0);
        }

        return (parts[0], int.TryParse(parts[^1], out var index) ? index : 0);
    }
}

public sealed record ShortcutDetails(
    string ShortcutPath,
    string TargetPath,
    bool TargetExists,
    string IconLocation);
