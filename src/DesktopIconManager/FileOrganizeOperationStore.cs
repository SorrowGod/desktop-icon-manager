using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopIconManager;

public static class FileOrganizeOperationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public static List<FileOrganizeOperation> Load()
    {
        try
        {
            if (!File.Exists(AppPaths.FileOrganizeOperationsPath))
            {
                return [];
            }

            var json = File.ReadAllText(AppPaths.FileOrganizeOperationsPath);
            var operations = JsonSerializer.Deserialize<List<FileOrganizeOperation>>(json, JsonOptions) ?? [];
            foreach (var operation in operations)
            {
                if (string.IsNullOrWhiteSpace(operation.Id))
                {
                    operation.Id = Guid.NewGuid().ToString("N");
                }

                operation.Results ??= [];
            }

            return operations;
        }
        catch (Exception ex)
        {
            AppLogger.Log("读取文件收纳记录失败。", ex);
            return [];
        }
    }

    public static void Add(FileOrganizeOperation operation, int maxOperations)
    {
        var operations = Load();
        operations.Insert(0, operation);
        operations = operations
            .OrderByDescending(item => item.CreatedAt)
            .Take(Math.Clamp(maxOperations, 1, 50))
            .ToList();
        SaveAll(operations);
    }

    public static void MarkUndone(string id)
    {
        var operations = Load();
        var operation = operations.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        if (operation is not null)
        {
            operation.IsUndone = true;
            SaveAll(operations);
        }
    }

    public static FileOrganizeOperation? GetLatestUndoable()
    {
        return Load()
            .Where(operation => !operation.IsUndoOperation && !operation.IsUndone)
            .OrderByDescending(operation => operation.CreatedAt)
            .FirstOrDefault(operation => operation.Results.Any(result => result.Success));
    }

    private static void SaveAll(List<FileOrganizeOperation> operations)
    {
        var json = JsonSerializer.Serialize(operations, JsonOptions);
        File.WriteAllText(AppPaths.FileOrganizeOperationsPath, json);
    }
}
