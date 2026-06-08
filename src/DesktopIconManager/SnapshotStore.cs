using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopIconManager;

public static class SnapshotStore
{
    private const int DefaultMaxSnapshots = 10;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public static List<LayoutSnapshot> Load()
    {
        try
        {
            if (!File.Exists(AppPaths.SnapshotsPath))
            {
                return [];
            }

            var json = File.ReadAllText(AppPaths.SnapshotsPath);
            var snapshots = JsonSerializer.Deserialize<List<LayoutSnapshot>>(json, JsonOptions) ?? [];
            foreach (var snapshot in snapshots)
            {
                if (string.IsNullOrWhiteSpace(snapshot.Id))
                {
                    snapshot.Id = Guid.NewGuid().ToString("N");
                }
            }

            return snapshots;
        }
        catch (Exception ex)
        {
            AppLogger.Log("读取布局快照失败。", ex);
            return [];
        }
    }

    public static LayoutSnapshot? GetLatest()
    {
        return Load()
            .OrderByDescending(snapshot => snapshot.CreatedAt)
            .FirstOrDefault();
    }

    public static LayoutSnapshot? GetById(string id)
    {
        return Load().FirstOrDefault(snapshot => string.Equals(snapshot.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static void Add(LayoutSnapshot snapshot, int maxSnapshots = DefaultMaxSnapshots)
    {
        var snapshots = Load();
        if (string.IsNullOrWhiteSpace(snapshot.Id))
        {
            snapshot.Id = Guid.NewGuid().ToString("N");
        }

        snapshots.Insert(0, snapshot);
        snapshots = snapshots
            .OrderByDescending(item => item.CreatedAt)
            .Take(Math.Clamp(maxSnapshots, 1, 50))
            .ToList();

        SaveAll(snapshots);
    }

    public static void Delete(string id)
    {
        var snapshots = Load()
            .Where(snapshot => !string.Equals(snapshot.Id, id, StringComparison.OrdinalIgnoreCase))
            .ToList();
        SaveAll(snapshots);
    }

    public static void Clear()
    {
        SaveAll([]);
    }

    private static void SaveAll(List<LayoutSnapshot> snapshots)
    {
        var json = JsonSerializer.Serialize(snapshots, JsonOptions);
        File.WriteAllText(AppPaths.SnapshotsPath, json);
    }
}
