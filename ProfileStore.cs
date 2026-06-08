using System.Drawing;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DesktopIconManager;

public static class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public static ProfileStoreData Load()
    {
        try
        {
            if (File.Exists(AppPaths.ProfilesPath))
            {
                var json = File.ReadAllText(AppPaths.ProfilesPath);
                var data = JsonSerializer.Deserialize<ProfileStoreData>(json, JsonOptions);
                if (data is not null)
                {
                    Normalize(data);
                    Save(data);
                    return data;
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log("读取方案失败，已使用默认方案。", ex);
        }

        var defaultData = new ProfileStoreData
        {
            DefaultProfileName = "默认方案",
            Profiles = [CreateDefaultProfile()],
            Scenes = CreateDefaultScenes(),
        };
        Save(defaultData);
        return defaultData;
    }

    public static void Save(ProfileStoreData data)
    {
        Normalize(data);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(AppPaths.ProfilesPath, json);
    }

    public static void ExportProfile(ArrangeProfile profile, string path)
    {
        var json = JsonSerializer.Serialize(profile, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static ArrangeProfile ImportProfile(string path)
    {
        var json = File.ReadAllText(path);
        var profile = JsonSerializer.Deserialize<ArrangeProfile>(json, JsonOptions)
            ?? throw new InvalidOperationException("方案文件格式不正确。");
        NormalizeProfile(profile);
        return profile;
    }

    public static ArrangeProfile CreateDefaultProfile(string name = "默认方案") => new()
    {
        Name = name,
        LayoutMode = LayoutMode.CenterCompact,
        SortMode = SortMode.TypeThenName,
        UseCurrentDesktopSpacing = true,
        LeftMargin = 16,
        TopMargin = 16,
        RightMargin = 16,
        BottomMargin = 16,
        ColumnSpacing = 112,
        RowSpacing = 96,
        StartupEnabled = false,
        ExcludedIconKeys = [],
        ExcludedCategories = [],
        ExcludedNamePatterns = [],
        PinnedIconPositions = [],
        Rules = [],
        Tags = [],
        CustomPattern = new CustomPatternDefinition(),
        DesktopZones = DesktopZoneEngine.CreateDefaultZones(),
        SafeArrangeEnabled = true,
        ExcludeSystemIcons = false,
        SnapshotRetentionCount = 10,
        SceneName = "默认场景",
    };

    public static ArrangeProfile CloneProfile(ArrangeProfile profile, string name)
    {
        return new ArrangeProfile
        {
            Name = name,
            LayoutMode = profile.LayoutMode,
            SortMode = profile.SortMode,
            TargetScreenDeviceName = profile.TargetScreenDeviceName,
            UseCurrentDesktopSpacing = profile.UseCurrentDesktopSpacing,
            LeftMargin = profile.LeftMargin,
            TopMargin = profile.TopMargin,
            RightMargin = profile.RightMargin,
            BottomMargin = profile.BottomMargin,
            ColumnSpacing = profile.ColumnSpacing,
            RowSpacing = profile.RowSpacing,
            StartupEnabled = false,
            SafeArrangeEnabled = profile.SafeArrangeEnabled,
            ExcludeSystemIcons = profile.ExcludeSystemIcons,
            SnapshotRetentionCount = profile.SnapshotRetentionCount,
            SceneName = profile.SceneName,
            ExcludedIconKeys = [.. profile.ExcludedIconKeys],
            ExcludedCategories = [.. profile.ExcludedCategories],
            ExcludedNamePatterns = [.. profile.ExcludedNamePatterns],
            PinnedIconPositions = profile.PinnedIconPositions.Select(item => new PinnedIconPosition
            {
                StableKey = item.StableKey,
                X = item.X,
                Y = item.Y,
            }).ToList(),
            Rules = profile.Rules.Select(item => new IconRule
            {
                Name = item.Name,
                MatchKind = item.MatchKind,
                MatchValue = item.MatchValue,
                ActionKind = item.ActionKind,
                ActionValue = item.ActionValue,
                Priority = item.Priority,
                Enabled = item.Enabled,
            }).ToList(),
            Tags = profile.Tags.Select(item => new IconTag
            {
                StableKey = item.StableKey,
                Name = item.Name,
                ColorArgb = item.ColorArgb,
            }).ToList(),
            CustomPattern = ClonePattern(profile.CustomPattern),
            DesktopZones = profile.DesktopZones.Select(CloneZone).ToList(),
        };
    }

    private static void Normalize(ProfileStoreData data)
    {
        data.Profiles ??= [];
        data.Scenes ??= [];
        if (data.Profiles.Count == 0)
        {
            data.Profiles.Add(CreateDefaultProfile());
        }

        if (data.Scenes.Count == 0)
        {
            data.Scenes.AddRange(CreateDefaultScenes());
        }
        else
        {
            EnsureBuiltInScenes(data.Scenes);
        }
        foreach (var scene in data.Scenes)
        {
            NormalizeScene(scene);
        }
        SortScenes(data.Scenes);

        foreach (var profile in data.Profiles)
        {
            NormalizeProfile(profile);
        }

        if (string.IsNullOrWhiteSpace(data.DefaultProfileName) ||
            data.Profiles.All(profile => !string.Equals(profile.Name, data.DefaultProfileName, StringComparison.OrdinalIgnoreCase)))
        {
            data.DefaultProfileName = data.Profiles[0].Name;
        }
    }

    public static List<DesktopScene> CreateDefaultScenes()
    {
        return
        [
            new DesktopScene
            {
                Name = "工作模式",
                IsBuiltIn = true,
                LayoutMode = LayoutMode.DesktopZones,
                SortMode = SortMode.TypeThenName,
                UseDesktopZones = true,
                DesktopZones = DesktopZoneEngine.CreateDefaultZones(),
            },
            new DesktopScene
            {
                Name = "学习模式",
                IsBuiltIn = true,
                LayoutMode = LayoutMode.Left,
                SortMode = SortMode.TypeThenName,
                ExcludeSystemIcons = true,
            },
            new DesktopScene
            {
                Name = "游戏模式",
                IsBuiltIn = true,
                LayoutMode = LayoutMode.Right,
                SortMode = SortMode.NameAscending,
                ExcludeSystemIcons = true,
            },
            new DesktopScene
            {
                Name = "演示模式",
                IsBuiltIn = true,
                LayoutMode = LayoutMode.Right,
                SortMode = SortMode.TypeThenName,
                ExcludeSystemIcons = true,
            },
            new DesktopScene
            {
                Name = "极简模式",
                IsBuiltIn = true,
                LayoutMode = LayoutMode.CenterCompact,
                SortMode = SortMode.CurrentOrder,
                ExcludeSystemIcons = true,
            },
        ];
    }

    public static bool IsBuiltInSceneName(string name)
    {
        return CreateDefaultScenes().Any(scene => string.Equals(scene.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureBuiltInScenes(List<DesktopScene> scenes)
    {
        var defaults = CreateDefaultScenes();
        foreach (var defaultScene in defaults)
        {
            var existing = scenes.FirstOrDefault(scene => string.Equals(scene.Name, defaultScene.Name, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                scenes.Add(defaultScene);
                continue;
            }

            existing.IsBuiltIn = true;
            existing.ProfileName = string.IsNullOrWhiteSpace(existing.ProfileName) ? defaultScene.ProfileName : existing.ProfileName;
            existing.CustomPattern ??= ClonePattern(defaultScene.CustomPattern);
            existing.CustomPattern.ManualPoints ??= [];
            existing.DesktopZones ??= [];
            existing.ExcludedIconKeys ??= [];
            if (existing.UseDesktopZones && existing.DesktopZones.Count == 0)
            {
                existing.DesktopZones = defaultScene.DesktopZones.Select(CloneZone).ToList();
            }
        }
    }

    private static void NormalizeScene(DesktopScene scene)
    {
        if (string.IsNullOrWhiteSpace(scene.Name))
        {
            scene.Name = "未命名场景";
        }

        scene.ProfileName = string.IsNullOrWhiteSpace(scene.ProfileName) ? "默认方案" : scene.ProfileName;
        scene.CustomPattern ??= new CustomPatternDefinition();
        scene.CustomPattern.ManualPoints ??= [];
        scene.DesktopZones ??= [];
        scene.ExcludedIconKeys ??= [];
        if (scene.UseDesktopZones && scene.DesktopZones.Count == 0)
        {
            scene.DesktopZones = DesktopZoneEngine.CreateDefaultZones();
        }
        else if (scene.DesktopZones.Count > 0)
        {
            NormalizeZones(scene.DesktopZones);
        }
    }

    private static void SortScenes(List<DesktopScene> scenes)
    {
        var builtInOrder = CreateDefaultScenes()
            .Select((scene, index) => new { scene.Name, Index = index })
            .ToDictionary(item => item.Name, item => item.Index, StringComparer.OrdinalIgnoreCase);

        scenes.Sort((left, right) =>
        {
            var leftBuiltIn = builtInOrder.TryGetValue(left.Name, out var leftIndex);
            var rightBuiltIn = builtInOrder.TryGetValue(right.Name, out var rightIndex);
            if (leftBuiltIn && rightBuiltIn)
            {
                return leftIndex.CompareTo(rightIndex);
            }

            if (leftBuiltIn)
            {
                return -1;
            }

            if (rightBuiltIn)
            {
                return 1;
            }

            return string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase);
        });
    }

    private static void NormalizeProfile(ArrangeProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
        {
            profile.Name = "默认方案";
        }

        profile.ExcludedIconKeys ??= [];
        profile.ExcludedCategories ??= [];
        profile.ExcludedNamePatterns ??= [];
        profile.PinnedIconPositions ??= [];
        profile.Rules ??= [];
        profile.Tags ??= [];
        profile.CustomPattern ??= new CustomPatternDefinition();
        profile.CustomPattern.ManualPoints ??= [];
        if (profile.CustomPattern.PointSpacing <= 1)
        {
            profile.CustomPattern.PointSpacing = 75;
        }
        if (profile.CustomPattern.PatternKind == PatternKind.ManualPoints && profile.CustomPattern.ManualPoints.Count == 0)
        {
            profile.CustomPattern.PatternKind = PatternKind.Circle;
        }
        profile.DesktopZones ??= [];
        if (profile.DesktopZones.Count == 0)
        {
            profile.DesktopZones = DesktopZoneEngine.CreateDefaultZones();
        }
        else
        {
            NormalizeZones(profile.DesktopZones);
        }

        if (profile.SnapshotRetentionCount <= 0)
        {
            profile.SnapshotRetentionCount = 10;
        }

        if (string.IsNullOrWhiteSpace(profile.SceneName))
        {
            profile.SceneName = "默认场景";
        }
    }

    private static void NormalizeZones(List<DesktopZone> zones)
    {
        var mediaZone = zones.FirstOrDefault(zone => string.Equals(zone.Name, "媒体与压缩包", StringComparison.OrdinalIgnoreCase));
        var legacyMediaContainedOther = mediaZone is not null && mediaZone.Categories.Contains(IconCategory.Other);
        if (legacyMediaContainedOther)
        {
            mediaZone!.Categories.RemoveAll(category => category == IconCategory.Other);
        }

        var pendingZone = zones.FirstOrDefault(zone => string.Equals(zone.Name, "待处理", StringComparison.OrdinalIgnoreCase));
        if (pendingZone is null)
        {
            pendingZone = new DesktopZone
            {
                Name = "待处理",
                IsBuiltIn = true,
                XPercent = 24,
                YPercent = 64,
                WidthPercent = 76,
                HeightPercent = 36,
                ColorArgb = Color.FromArgb(115, 76, 170).ToArgb(),
                LayoutMode = LayoutMode.Top,
                SortMode = SortMode.CurrentOrder,
            };
            zones.Add(pendingZone);
        }

        if (!pendingZone.Categories.Contains(IconCategory.Other))
        {
            pendingZone.Categories.Add(IconCategory.Other);
        }

        foreach (var zone in zones.Where(DesktopZoneEngine.IsBuiltInZone))
        {
            zone.IsBuiltIn = true;
        }

        if (legacyMediaContainedOther && LooksLikeLegacyDefaultZones(zones))
        {
            zones.Clear();
            zones.AddRange(DesktopZoneEngine.CreateDefaultZones());
        }
    }

    private static bool LooksLikeLegacyDefaultZones(IReadOnlyList<DesktopZone> zones)
    {
        var names = zones.Select(zone => zone.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return names.SetEquals(["常用软件", "工作文件", "媒体与压缩包", "待处理"]);
    }

    private static CustomPatternDefinition ClonePattern(CustomPatternDefinition pattern)
    {
        return new CustomPatternDefinition
        {
            PatternKind = pattern.PatternKind,
            FillMode = pattern.FillMode,
            CenterXPercent = pattern.CenterXPercent,
            CenterYPercent = pattern.CenterYPercent,
            WidthPercent = pattern.WidthPercent,
            HeightPercent = pattern.HeightPercent,
            RotationDegrees = pattern.RotationDegrees,
            PointSpacing = pattern.PointSpacing <= 1 ? 75 : pattern.PointSpacing,
            SnapToGrid = pattern.SnapToGrid,
            OverflowToGrid = pattern.OverflowToGrid,
            Text = pattern.Text,
            ImageMaskPath = pattern.ImageMaskPath,
            ManualPoints = pattern.ManualPoints.Select(point => new PatternPoint
            {
                XPercent = point.XPercent,
                YPercent = point.YPercent,
            }).ToList(),
        };
    }

    private static DesktopZone CloneZone(DesktopZone zone)
    {
        return new DesktopZone
        {
            Name = zone.Name,
            IsBuiltIn = zone.IsBuiltIn,
            XPercent = zone.XPercent,
            YPercent = zone.YPercent,
            WidthPercent = zone.WidthPercent,
            HeightPercent = zone.HeightPercent,
            ColorArgb = zone.ColorArgb,
            LayoutMode = zone.LayoutMode,
            SortMode = zone.SortMode,
            Categories = [.. zone.Categories],
            NameContains = [.. zone.NameContains],
            IconKeys = [.. zone.IconKeys],
        };
    }
}
