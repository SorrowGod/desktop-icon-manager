using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopIconManager;

public static class PendingOperationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public static PendingArrangeOperation? Load()
    {
        try
        {
            if (!File.Exists(AppPaths.PendingOperationPath))
            {
                return null;
            }

            var json = File.ReadAllText(AppPaths.PendingOperationPath);
            return JsonSerializer.Deserialize<PendingArrangeOperation>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            AppLogger.Log("读取未完成整理记录失败。", ex);
            return null;
        }
    }

    public static void SaveIfMissing(PendingArrangeOperation operation)
    {
        try
        {
            if (File.Exists(AppPaths.PendingOperationPath))
            {
                return;
            }

            var json = JsonSerializer.Serialize(operation, JsonOptions);
            File.WriteAllText(AppPaths.PendingOperationPath, json);
        }
        catch (Exception ex)
        {
            AppLogger.Log("保存未完成整理记录失败。", ex);
        }
    }

    public static void Clear()
    {
        try
        {
            if (File.Exists(AppPaths.PendingOperationPath))
            {
                File.Delete(AppPaths.PendingOperationPath);
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log("清理未完成整理记录失败。", ex);
        }
    }
}
