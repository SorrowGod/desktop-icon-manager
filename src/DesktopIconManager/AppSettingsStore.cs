using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopIconManager;

public static class AppSettingsStore
{
    public const string DefaultUpdateManifestUrl = "https://sorrowgod.github.io/desktop-icon-manager/update.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(AppPaths.SettingsPath))
            {
                var json = File.ReadAllText(AppPaths.SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                if (settings is not null)
                {
                    Normalize(settings);
                    Save(settings);
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log("读取应用设置失败，已使用默认设置。", ex);
        }

        var defaults = AppSettings.CreateDefault();
        Save(defaults);
        return defaults;
    }

    public static void Save(AppSettings settings)
    {
        Normalize(settings);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(AppPaths.SettingsPath, json);
    }

    public static void Reset(bool preserveFirstRunGuideCompleted)
    {
        var defaults = AppSettings.CreateDefault();
        defaults.FirstRunGuideCompleted = preserveFirstRunGuideCompleted;
        Save(defaults);
    }

    public static void Normalize(AppSettings settings)
    {
        settings.DesktopOrganizeFolderNames ??= new DesktopOrganizeFolderNames();
        settings.FileOrganizeUndoRetentionCount = Math.Clamp(settings.FileOrganizeUndoRetentionCount, 1, 50);
        settings.UpdateManifestUrl = settings.UpdateManifestUrl?.Trim() ?? string.Empty;
        if (ShouldUseDefaultUpdateManifestUrl(settings.UpdateManifestUrl))
        {
            settings.UpdateManifestUrl = DefaultUpdateManifestUrl;
        }

        if (string.IsNullOrWhiteSpace(settings.DesktopOrganizeFolderNames.Documents))
        {
            settings.DesktopOrganizeFolderNames.Documents = "文档";
        }

        if (string.IsNullOrWhiteSpace(settings.DesktopOrganizeFolderNames.Images))
        {
            settings.DesktopOrganizeFolderNames.Images = "图片";
        }

        if (string.IsNullOrWhiteSpace(settings.DesktopOrganizeFolderNames.Media))
        {
            settings.DesktopOrganizeFolderNames.Media = "视频音频";
        }

        if (string.IsNullOrWhiteSpace(settings.DesktopOrganizeFolderNames.ArchivesAndInstallers))
        {
            settings.DesktopOrganizeFolderNames.ArchivesAndInstallers = "压缩包安装包";
        }
    }

    private static bool ShouldUseDefaultUpdateManifestUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (value.Contains("example.com", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var fileName = Path.GetFileName(value.Trim().Trim('"'));
        return fileName.Equals("update-manifest.example.json", StringComparison.OrdinalIgnoreCase);
    }
}
