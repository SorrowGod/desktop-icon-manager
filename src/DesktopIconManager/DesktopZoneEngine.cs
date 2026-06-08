namespace DesktopIconManager;

public static class DesktopZoneEngine
{
    private static readonly string[] BuiltInZoneNames = ["常用软件", "工作文件", "媒体与压缩包", "待处理"];

    public static IReadOnlyList<ArrangedIconPosition> Calculate(
        ArrangeProfile profile,
        IReadOnlyList<DesktopIconInfo> icons,
        Rectangle workArea,
        Size spacing)
    {
        if (icons.Count == 0)
        {
            return [];
        }

        var zones = profile.DesktopZones.Count > 0
            ? profile.DesktopZones
            : CreateDefaultZones();
        var assignments = zones.ToDictionary(zone => zone, _ => new List<DesktopIconInfo>());
        var unassigned = new List<DesktopIconInfo>();

        foreach (var icon in icons)
        {
            var zone = FindZone(profile, zones, icon);
            if (zone is null)
            {
                unassigned.Add(icon);
            }
            else
            {
                assignments[zone].Add(icon);
            }
        }

        if (unassigned.Count > 0)
        {
            var fallbackZone = zones.FirstOrDefault(zone => string.Equals(zone.Name, "待处理", StringComparison.OrdinalIgnoreCase))
                ?? zones[^1];
            assignments[fallbackZone].AddRange(unassigned);
        }

        var positions = new List<ArrangedIconPosition>(icons.Count);
        foreach (var zone in zones)
        {
            var zoneIcons = assignments[zone];
            if (zoneIcons.Count == 0)
            {
                continue;
            }

            var zoneProfile = CloneForZone(profile, zone);
            var zoneRect = zone.ToRectangle(workArea);
            var ordered = DesktopIconArranger.SortIcons(zoneIcons, zone.SortMode);
            positions.AddRange(DesktopIconArranger.CalculateBasicPositions(zoneProfile, ordered, zoneRect, spacing, zone.Name));
        }

        return positions;
    }

    public static List<DesktopZone> CreateDefaultZones()
    {
        return
        [
            new DesktopZone
            {
                Name = "常用软件",
                IsBuiltIn = true,
                XPercent = 0,
                YPercent = 0,
                WidthPercent = 22,
                HeightPercent = 100,
                ColorArgb = Color.FromArgb(60, 99, 230).ToArgb(),
                LayoutMode = LayoutMode.Left,
                SortMode = SortMode.TypeThenName,
                Categories = [IconCategory.ShortcutOrApp, IconCategory.System],
            },
            new DesktopZone
            {
                Name = "工作文件",
                IsBuiltIn = true,
                XPercent = 24,
                YPercent = 0,
                WidthPercent = 38,
                HeightPercent = 60,
                ColorArgb = Color.FromArgb(0, 137, 123).ToArgb(),
                LayoutMode = LayoutMode.Top,
                SortMode = SortMode.TypeThenName,
                Categories = [IconCategory.Document, IconCategory.Folder],
            },
            new DesktopZone
            {
                Name = "媒体与压缩包",
                IsBuiltIn = true,
                XPercent = 64,
                YPercent = 0,
                WidthPercent = 36,
                HeightPercent = 45,
                ColorArgb = Color.FromArgb(194, 100, 20).ToArgb(),
                LayoutMode = LayoutMode.Top,
                SortMode = SortMode.TypeThenName,
                Categories = [IconCategory.Image, IconCategory.Media, IconCategory.Archive],
            },
            new DesktopZone
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
                Categories = [IconCategory.Other],
            },
        ];
    }

    public static bool IsBuiltInZone(DesktopZone zone)
    {
        return zone.IsBuiltIn || IsBuiltInZoneName(zone.Name);
    }

    public static bool IsBuiltInZoneName(string name)
    {
        return BuiltInZoneNames.Contains(name, StringComparer.OrdinalIgnoreCase);
    }

    public static void ReflowZones(IList<DesktopZone> zones)
    {
        if (zones.Count == 0)
        {
            return;
        }

        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(zones.Count)));
        var rows = Math.Max(1, (int)Math.Ceiling(zones.Count / (double)columns));
        const int gap = 2;
        var width = Math.Max(12, (100 - (columns - 1) * gap) / columns);
        var height = Math.Max(12, (100 - (rows - 1) * gap) / rows);

        for (var index = 0; index < zones.Count; index++)
        {
            var row = index / columns;
            var column = index % columns;
            zones[index].XPercent = Math.Clamp(column * (width + gap), 0, 100);
            zones[index].YPercent = Math.Clamp(row * (height + gap), 0, 100);
            zones[index].WidthPercent = Math.Clamp(width, 5, 100);
            zones[index].HeightPercent = Math.Clamp(height, 5, 100);
        }
    }

    private static DesktopZone? FindZone(ArrangeProfile profile, IReadOnlyList<DesktopZone> zones, DesktopIconInfo icon)
    {
        foreach (var rule in profile.Rules.Where(rule => rule.Enabled && rule.ActionKind == RuleActionKind.SendToZone).OrderBy(rule => rule.Priority))
        {
            if (DesktopIconArranger.MatchesRule(rule, icon))
            {
                var matched = zones.FirstOrDefault(zone => string.Equals(zone.Name, rule.ActionValue, StringComparison.OrdinalIgnoreCase));
                if (matched is not null)
                {
                    return matched;
                }
            }
        }

        return zones.FirstOrDefault(zone =>
            zone.IconKeys.Contains(icon.StableKey, StringComparer.OrdinalIgnoreCase) ||
            zone.Categories.Contains(icon.Category) ||
            zone.NameContains.Any(keyword =>
                !string.IsNullOrWhiteSpace(keyword) &&
                icon.DisplayName.Contains(keyword.Trim(), StringComparison.CurrentCultureIgnoreCase)));
    }

    private static ArrangeProfile CloneForZone(ArrangeProfile profile, DesktopZone zone)
    {
        return new ArrangeProfile
        {
            Name = profile.Name,
            LayoutMode = zone.LayoutMode is LayoutMode.CustomPattern or LayoutMode.DesktopZones
                ? LayoutMode.CenterCompact
                : zone.LayoutMode,
            SortMode = zone.SortMode,
            UseCurrentDesktopSpacing = false,
            LeftMargin = 8,
            TopMargin = 8,
            RightMargin = 8,
            BottomMargin = 8,
            ColumnSpacing = profile.ColumnSpacing,
            RowSpacing = profile.RowSpacing,
            CustomPattern = profile.CustomPattern,
        };
    }
}
