namespace DesktopIconManager;

public static class DesktopHealthAnalyzer
{
    public static DesktopHealthReport Analyze(IReadOnlyList<DesktopIconInfo> icons)
    {
        var report = new DesktopHealthReport
        {
            IconCount = icons.Count,
            CategoryCounts = icons
                .GroupBy(icon => icon.Category)
                .ToDictionary(group => group.Key, group => group.Count()),
            RecentFiles = icons
                .Where(icon => icon.FilePath is not null && icon.LastModifiedAt is not null)
                .OrderByDescending(icon => icon.LastModifiedAt)
                .Take(8)
                .Select(ToHealthItem)
                .ToList(),
            LargeFiles = icons
                .Where(icon => icon.FilePath is not null && icon.FileSizeBytes.GetValueOrDefault() > 20 * 1024 * 1024)
                .OrderByDescending(icon => icon.FileSizeBytes)
                .Take(8)
                .Select(ToHealthItem)
                .ToList(),
            BrokenShortcuts = icons
                .Where(icon => icon.IsShortcut && icon.ShortcutTargetExists == false)
                .Select(ToHealthItem)
                .ToList(),
        };

        report.Suggestions = BuildSuggestions(report);
        return report;
    }

    private static DesktopHealthItem ToHealthItem(DesktopIconInfo icon)
    {
        return new DesktopHealthItem
        {
            Name = icon.DisplayName,
            Path = icon.FilePath ?? string.Empty,
            Category = icon.Category,
            SizeBytes = icon.FileSizeBytes ?? 0,
            LastModifiedAt = icon.LastModifiedAt,
            Details = icon.IsShortcut && icon.ShortcutTargetExists == false
                ? "快捷方式目标不存在"
                : FormatSize(icon.FileSizeBytes ?? 0),
        };
    }

    private static List<string> BuildSuggestions(DesktopHealthReport report)
    {
        var suggestions = new List<string>();
        if (report.IconCount >= 60)
        {
            suggestions.Add($"桌面共有 {report.IconCount} 个图标，建议使用“桌面分区”或“中间一团”保持可扫描。");
        }

        if (report.CategoryCounts.TryGetValue(IconCategory.Document, out var documents) && documents >= 8)
        {
            suggestions.Add($"检测到 {documents} 个文档类图标，可考虑建立“工作文件”分区。");
        }

        if (report.CategoryCounts.TryGetValue(IconCategory.Archive, out var archives) && archives > 0)
        {
            suggestions.Add($"检测到 {archives} 个压缩包。可到“文件收纳”页预览并手动勾选收纳。");
        }

        if (report.LargeFiles.Count > 0)
        {
            suggestions.Add($"检测到 {report.LargeFiles.Count} 个较大桌面文件。文件收纳必须二次确认后才会移动真实文件。");
        }

        if (report.BrokenShortcuts.Count > 0)
        {
            suggestions.Add($"检测到 {report.BrokenShortcuts.Count} 个疑似失效快捷方式。当前版本只列出，不会自动删除。");
        }

        if (suggestions.Count == 0)
        {
            suggestions.Add("桌面状态较清爽，可以只使用布局整理和快照功能。");
        }

        return suggestions;
    }

    public static string FormatSize(long size)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        var value = (double)size;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }
}
