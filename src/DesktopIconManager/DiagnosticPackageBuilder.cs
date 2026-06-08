using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopIconManager;

public static class DiagnosticPackageBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Export(
        string? outputPath,
        AppSettings settings,
        ProfileStoreData profileStore,
        IReadOnlyList<DesktopIconInfo> icons)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profileStore);
        ArgumentNullException.ThrowIfNull(icons);

        var path = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine(AppPaths.DiagnosticsDirectory, $"DesktopIconManager-Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip")
            : outputPath;
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (File.Exists(path))
        {
            File.Delete(path);
        }

        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        AddText(archive, "version.txt", BuildVersionText());
        AddText(archive, "screens.txt", BuildScreenText());
        AddText(archive, "settings-summary.json", BuildSettingsSummary(settings));
        AddText(archive, "profiles-summary.json", BuildProfileSummary(profileStore));
        AddText(archive, "icons-summary.json", BuildIconSummary(icons, settings.DiagnosticsIncludeFullPaths));
        AddText(archive, "recent-log.txt", ReadLogTail());
        return path;
    }

    private static void AddText(ZipArchive archive, string name, string text)
    {
        var entry = archive.CreateEntry(name, CompressionLevel.Fastest);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(text);
    }

    private static string BuildVersionText()
    {
        var version = UpdateChecker.GetCurrentVersionInfo();
        return
            $"DesktopIconManager\r\n" +
            $"Version: {version.Version}\r\n" +
            $"BuildTime: {version.BuildTime:yyyy-MM-dd HH:mm:ss}\r\n" +
            $"ReleaseChannel: {version.ReleaseChannel}\r\n" +
            $"Signed: {version.IsSigned}\r\n" +
            $"OS: {Environment.OSVersion}\r\n" +
            $".NET: {Environment.Version}\r\n" +
            $"Process64Bit: {Environment.Is64BitProcess}\r\n";
    }

    private static string BuildScreenText()
    {
        var builder = new StringBuilder();
        foreach (var screen in Screen.AllScreens)
        {
            builder.AppendLine($"{screen.DeviceName}");
            builder.AppendLine($"  Primary: {screen.Primary}");
            builder.AppendLine($"  Bounds: {screen.Bounds}");
            builder.AppendLine($"  WorkingArea: {screen.WorkingArea}");
        }

        return builder.ToString();
    }

    private static string BuildSettingsSummary(AppSettings settings)
    {
        var summary = new
        {
            settings.CheckUpdatesOnStartup,
            settings.LastUpdateCheckAt,
            settings.FileOrganizeUndoRetentionCount,
            settings.DiagnosticsIncludeFullPaths,
            settings.FirstRunGuideCompleted,
            UpdateManifestConfigured = !string.IsNullOrWhiteSpace(settings.UpdateManifestUrl),
            settings.DesktopOrganizeFolderNames,
        };
        return JsonSerializer.Serialize(summary, JsonOptions);
    }

    private static string BuildProfileSummary(ProfileStoreData profileStore)
    {
        var summary = new
        {
            profileStore.DefaultProfileName,
            ProfileCount = profileStore.Profiles.Count,
            SceneCount = profileStore.Scenes.Count,
            Profiles = profileStore.Profiles.Select(profile => new
            {
                profile.Name,
                profile.LayoutMode,
                profile.SortMode,
                profile.StartupEnabled,
                profile.SafeArrangeEnabled,
                profile.ExcludeSystemIcons,
                ExcludedCount = profile.ExcludedIconKeys.Count,
                ZoneCount = profile.DesktopZones.Count,
            }),
            Scenes = profileStore.Scenes.Select(scene => new
            {
                scene.Name,
                scene.IsBuiltIn,
                scene.LayoutMode,
                scene.SortMode,
            }),
        };
        return JsonSerializer.Serialize(summary, JsonOptions);
    }

    private static string BuildIconSummary(IReadOnlyList<DesktopIconInfo> icons, bool includeFullPaths)
    {
        var summary = icons.Select(icon => new
        {
            icon.DisplayName,
            icon.Category,
            icon.Extension,
            icon.FileSizeBytes,
            icon.LastModifiedAt,
            icon.IsShortcut,
            icon.ShortcutTargetExists,
            FilePath = includeFullPaths ? icon.FilePath : RedactPath(icon.FilePath),
            ShortcutTargetPath = includeFullPaths ? icon.ShortcutTargetPath : RedactPath(icon.ShortcutTargetPath),
        });
        return JsonSerializer.Serialize(summary, JsonOptions);
    }

    private static string ReadLogTail()
    {
        try
        {
            if (!File.Exists(AppPaths.LogPath))
            {
                return "暂无日志。";
            }

            var lines = File.ReadLines(AppPaths.LogPath).TakeLast(300);
            return string.Join(Environment.NewLine, lines);
        }
        catch (Exception ex)
        {
            return $"读取日志失败：{ex.Message}";
        }
    }

    private static string? RedactPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        return $"<已脱敏>\\{Path.GetFileName(path)}";
    }
}
