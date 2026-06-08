using System.Text.Json.Serialization;

namespace DesktopIconManager;

public enum LayoutMode
{
    Left,
    Right,
    Top,
    Bottom,
    CenterCompact,
    CustomPattern,
    DesktopZones,
}

public enum SortMode
{
    TypeThenName,
    NameAscending,
    CurrentOrder,
    LastModifiedTime,
    FileSize,
    Extension,
    UsageFrequency,
    ManualPriority,
}

public enum IconCategory
{
    Folder,
    ShortcutOrApp,
    Document,
    Image,
    Media,
    Archive,
    System,
    Other,
}

public enum PatternKind
{
    Circle,
    Semicircle,
    Heart,
    Star,
    Wave,
    Diagonal,
    SquareGrid,
    Ring,
    VShape,
    XShape,
    Text,
    ImageMask,
    ManualPoints,
}

public enum PatternFillMode
{
    LeftToRight,
    TopToBottom,
    CenterOut,
    Clockwise,
}

public enum RuleMatchKind
{
    NameContains,
    Category,
    StableKey,
}

public enum RuleActionKind
{
    Exclude,
    SendToZone,
    Pin,
    Prioritize,
}

public sealed class DesktopIconInfo
{
    public int Index { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public IconCategory Category { get; init; } = IconCategory.Other;

    public Point Position { get; init; }

    public string StableKey { get; init; } = string.Empty;

    public string? FilePath { get; init; }

    public string? Extension { get; init; }

    public DateTime? LastModifiedAt { get; init; }

    public long? FileSizeBytes { get; init; }

    public bool IsShortcut { get; init; }

    public string? ShortcutTargetPath { get; init; }

    public bool? ShortcutTargetExists { get; init; }
}

public sealed class ArrangeProfile
{
    public string Name { get; set; } = "默认方案";

    public LayoutMode LayoutMode { get; set; } = LayoutMode.CenterCompact;

    public SortMode SortMode { get; set; } = SortMode.TypeThenName;

    public string? TargetScreenDeviceName { get; set; }

    public bool UseCurrentDesktopSpacing { get; set; } = true;

    public int LeftMargin { get; set; } = 16;

    public int TopMargin { get; set; } = 16;

    public int RightMargin { get; set; } = 16;

    public int BottomMargin { get; set; } = 16;

    public int ColumnSpacing { get; set; } = 112;

    public int RowSpacing { get; set; } = 96;

    public bool StartupEnabled { get; set; }

    public bool SafeArrangeEnabled { get; set; } = true;

    public bool ExcludeSystemIcons { get; set; }

    public int SnapshotRetentionCount { get; set; } = 10;

    public string SceneName { get; set; } = "默认场景";

    public List<string> ExcludedIconKeys { get; set; } = [];

    public List<IconCategory> ExcludedCategories { get; set; } = [];

    public List<string> ExcludedNamePatterns { get; set; } = [];

    public List<PinnedIconPosition> PinnedIconPositions { get; set; } = [];

    public List<IconRule> Rules { get; set; } = [];

    public List<IconTag> Tags { get; set; } = [];

    public CustomPatternDefinition CustomPattern { get; set; } = new();

    public List<DesktopZone> DesktopZones { get; set; } = [];

    [JsonIgnore]
    public string DisplayName => Name;

    public override string ToString() => Name;
}

public sealed class CustomPatternDefinition
{
    public PatternKind PatternKind { get; set; } = PatternKind.Circle;

    public PatternFillMode FillMode { get; set; } = PatternFillMode.LeftToRight;

    public int CenterXPercent { get; set; } = 50;

    public int CenterYPercent { get; set; } = 50;

    public int WidthPercent { get; set; } = 62;

    public int HeightPercent { get; set; } = 48;

    public int RotationDegrees { get; set; }

    public int PointSpacing { get; set; } = 75;

    public bool SnapToGrid { get; set; } = true;

    public bool OverflowToGrid { get; set; } = true;

    public string Text { get; set; } = "WORK";

    public string? ImageMaskPath { get; set; }

    public List<PatternPoint> ManualPoints { get; set; } = [];
}

public sealed class PatternPoint
{
    public int XPercent { get; set; }

    public int YPercent { get; set; }
}

public sealed class DesktopZone
{
    public string Name { get; set; } = "新区块";

    public bool IsBuiltIn { get; set; }

    public int XPercent { get; set; } = 0;

    public int YPercent { get; set; } = 0;

    public int WidthPercent { get; set; } = 35;

    public int HeightPercent { get; set; } = 50;

    public int ColorArgb { get; set; } = Color.FromArgb(77, 120, 255).ToArgb();

    public LayoutMode LayoutMode { get; set; } = LayoutMode.Left;

    public SortMode SortMode { get; set; } = SortMode.TypeThenName;

    public List<IconCategory> Categories { get; set; } = [];

    public List<string> NameContains { get; set; } = [];

    public List<string> IconKeys { get; set; } = [];

    public Rectangle ToRectangle(Rectangle workArea)
    {
        var width = Math.Max(48, workArea.Width * Math.Clamp(WidthPercent, 5, 100) / 100);
        var height = Math.Max(48, workArea.Height * Math.Clamp(HeightPercent, 5, 100) / 100);
        var maxX = Math.Max(workArea.Left, workArea.Right - width);
        var maxY = Math.Max(workArea.Top, workArea.Bottom - height);
        var x = Math.Clamp(workArea.Left + (workArea.Width * Math.Clamp(XPercent, 0, 100) / 100), workArea.Left, maxX);
        var y = Math.Clamp(workArea.Top + (workArea.Height * Math.Clamp(YPercent, 0, 100) / 100), workArea.Top, maxY);
        return new Rectangle(x, y, width, height);
    }
}

public sealed class DesktopScene
{
    public string Name { get; set; } = "默认场景";

    public bool IsBuiltIn { get; set; }

    public string ProfileName { get; set; } = "默认方案";

    public LayoutMode LayoutMode { get; set; } = LayoutMode.CenterCompact;

    public SortMode SortMode { get; set; } = SortMode.TypeThenName;

    public bool ExcludeSystemIcons { get; set; }

    public bool UseDesktopZones { get; set; }

    public CustomPatternDefinition CustomPattern { get; set; } = new();

    public List<DesktopZone> DesktopZones { get; set; } = [];

    public List<string> ExcludedIconKeys { get; set; } = [];

    public override string ToString() => Name;
}

public sealed class IconRule
{
    public string Name { get; set; } = "新规则";

    public RuleMatchKind MatchKind { get; set; } = RuleMatchKind.NameContains;

    public string MatchValue { get; set; } = string.Empty;

    public RuleActionKind ActionKind { get; set; } = RuleActionKind.Exclude;

    public string ActionValue { get; set; } = string.Empty;

    public int Priority { get; set; } = 100;

    public bool Enabled { get; set; } = true;
}

public sealed class PinnedIconPosition
{
    public string StableKey { get; set; } = string.Empty;

    public int X { get; set; }

    public int Y { get; set; }

    public Point ToPoint() => new(X, Y);
}

public sealed class IconTag
{
    public string StableKey { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int ColorArgb { get; set; } = Color.FromArgb(255, 193, 7).ToArgb();
}

public sealed class ShortcutIconBackup
{
    public string ShortcutPath { get; set; } = string.Empty;

    public string OriginalIconPath { get; set; } = string.Empty;

    public int OriginalIconIndex { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public sealed class ArrangeLayout
{
    public required ArrangeProfile Profile { get; init; }

    public required Rectangle WorkArea { get; init; }

    public required Size Spacing { get; init; }

    public required IReadOnlyList<ArrangedIconPosition> Positions { get; init; }

    public required int ExcludedCount { get; init; }

    public string LayoutSummary { get; init; } = string.Empty;
}

public sealed class ArrangedIconPosition
{
    public required DesktopIconInfo Icon { get; init; }

    public required Point TargetPosition { get; init; }

    public string? ZoneName { get; init; }
}

public sealed class ArrangePreview
{
    public required int IconCount { get; init; }

    public required int MovableCount { get; init; }

    public required int ExcludedCount { get; init; }

    public required LayoutMode LayoutMode { get; init; }

    public required SortMode SortMode { get; init; }

    public required string TargetScreenName { get; init; }

    public required bool SafeArrangeEnabled { get; init; }
}

public sealed class ArrangeApplyResult
{
    public int RequestedCount { get; set; }

    public int VerifiedAtTargetCount { get; set; }

    public int FailedCount { get; set; }

    public bool AutoArrangeWasEnabled { get; set; }

    public bool AutoArrangeDisabled { get; set; }

    public string AutoArrangeDisableError { get; set; } = string.Empty;

    public int AttemptCount { get; set; } = 1;

    public List<string> FailedIconDetails { get; set; } = [];

    [JsonIgnore]
    public bool IsFullyApplied => FailedCount == 0;
}

public sealed class LayoutSnapshot
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string ProfileName { get; set; } = string.Empty;

    public LayoutMode LayoutMode { get; set; } = LayoutMode.CenterCompact;

    public SortMode SortMode { get; set; } = SortMode.TypeThenName;

    public string TargetScreenName { get; set; } = string.Empty;

    public int MovedCount { get; set; }

    public int ExcludedCount { get; set; }

    public string AppVersion { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;

    public List<IconPositionSnapshot> Icons { get; set; } = [];
}

public sealed class IconPositionSnapshot
{
    public string StableKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public int Index { get; set; }

    public int X { get; set; }

    public int Y { get; set; }

    public Point ToPoint() => new(X, Y);
}

public sealed class ProfileStoreData
{
    public string DefaultProfileName { get; set; } = "默认方案";

    public List<ArrangeProfile> Profiles { get; set; } = [];

    public List<DesktopScene> Scenes { get; set; } = [];
}

public sealed class PendingArrangeOperation
{
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string Reason { get; set; } = "整理可能未完成";

    public LayoutSnapshot Snapshot { get; set; } = new();
}

public sealed class DesktopHealthReport
{
    public int IconCount { get; set; }

    public Dictionary<IconCategory, int> CategoryCounts { get; set; } = [];

    public List<DesktopHealthItem> RecentFiles { get; set; } = [];

    public List<DesktopHealthItem> LargeFiles { get; set; } = [];

    public List<DesktopHealthItem> BrokenShortcuts { get; set; } = [];

    public List<string> Suggestions { get; set; } = [];
}

public sealed class DesktopHealthItem
{
    public string Name { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public IconCategory Category { get; set; }

    public long SizeBytes { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public string Details { get; set; } = string.Empty;
}

public sealed class AppVersionInfo
{
    public string Version { get; set; } = "1.1.5";

    public DateTime BuildTime { get; set; } = DateTime.Now;

    public string ReleaseChannel { get; set; } = "官网安装包";

    public bool IsSigned { get; set; }
}

public sealed class UpdateManifest
{
    public string Version { get; set; } = string.Empty;

    public DateTime? ReleaseDate { get; set; }

    public string DownloadUrl { get; set; } = string.Empty;

    public string PortableUrl { get; set; } = string.Empty;

    public string Sha256 { get; set; } = string.Empty;

    public List<string> Notes { get; set; } = [];
}

public sealed class UpdateCheckResult
{
    public bool IsConfigured { get; set; }

    public bool HasUpdate { get; set; }

    public bool IsLatest { get; set; }

    public string CurrentVersion { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public UpdateManifest? Manifest { get; set; }

    public string ManifestSourceUrl { get; set; } = string.Empty;

    public List<string> AttemptedSources { get; set; } = [];

    public List<string> AttemptErrors { get; set; } = [];
}

public sealed class DesktopOrganizeFolderNames
{
    public string Documents { get; set; } = "文档";

    public string Images { get; set; } = "图片";

    public string Media { get; set; } = "视频音频";

    public string ArchivesAndInstallers { get; set; } = "压缩包安装包";
}

public sealed class AppSettings
{
    public bool CheckUpdatesOnStartup { get; set; } = true;

    public DateTime? LastUpdateCheckAt { get; set; }

    public int FileOrganizeUndoRetentionCount { get; set; } = 10;

    public bool DiagnosticsIncludeFullPaths { get; set; }

    public bool FirstRunGuideCompleted { get; set; }

    public string UpdateManifestUrl { get; set; } = string.Empty;

    public DesktopOrganizeFolderNames DesktopOrganizeFolderNames { get; set; } = new();

    public static AppSettings CreateDefault() => new()
    {
        CheckUpdatesOnStartup = true,
        FileOrganizeUndoRetentionCount = 10,
        DiagnosticsIncludeFullPaths = false,
        FirstRunGuideCompleted = false,
        UpdateManifestUrl = string.Empty,
        DesktopOrganizeFolderNames = new DesktopOrganizeFolderNames(),
    };
}

public enum FileOrganizeTargetKind
{
    Document,
    Image,
    Media,
    ArchiveOrInstaller,
    Other,
}

public sealed class FileOrganizeSuggestion
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string FileName { get; set; } = string.Empty;

    public string OriginalPath { get; set; } = string.Empty;

    public IconCategory Category { get; set; } = IconCategory.Other;

    public FileOrganizeTargetKind TargetKind { get; set; } = FileOrganizeTargetKind.Other;

    public string SuggestedTargetPath { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public DateTime? LastModifiedAt { get; set; }

    public bool DefaultSelected { get; set; }

    public string Reason { get; set; } = string.Empty;
}

public sealed class FileOrganizeOperation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public bool IsUndoOperation { get; set; }

    public bool IsUndone { get; set; }

    public List<FileOrganizeMoveResult> Results { get; set; } = [];
}

public sealed class FileOrganizeMoveResult
{
    public string FileName { get; set; } = string.Empty;

    public string OriginalPath { get; set; } = string.Empty;

    public string TargetPath { get; set; } = string.Empty;

    public bool Success { get; set; }

    public string Status { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
}

public static class DisplayNames
{
    public static string ToDisplayText(this LayoutMode mode) => mode switch
    {
        LayoutMode.Left => "左边",
        LayoutMode.Right => "右边",
        LayoutMode.Top => "上面",
        LayoutMode.Bottom => "下面",
        LayoutMode.CenterCompact => "中间一团",
        LayoutMode.CustomPattern => "自定义图案",
        LayoutMode.DesktopZones => "桌面分区",
        _ => mode.ToString(),
    };

    public static string ToDisplayText(this SortMode mode) => mode switch
    {
        SortMode.TypeThenName => "类型分组",
        SortMode.NameAscending => "名称 A-Z",
        SortMode.CurrentOrder => "保持当前顺序",
        SortMode.LastModifiedTime => "最近修改时间",
        SortMode.FileSize => "文件大小",
        SortMode.Extension => "扩展名",
        SortMode.UsageFrequency => "使用频率",
        SortMode.ManualPriority => "手动优先级",
        _ => mode.ToString(),
    };

    public static string ToDisplayText(this IconCategory category) => category switch
    {
        IconCategory.Folder => "文件夹",
        IconCategory.ShortcutOrApp => "快捷方式/应用",
        IconCategory.Document => "文档",
        IconCategory.Image => "图片",
        IconCategory.Media => "音视频",
        IconCategory.Archive => "压缩包",
        IconCategory.System => "系统图标",
        IconCategory.Other => "其他",
        _ => category.ToString(),
    };

    public static string ToDisplayText(this PatternKind pattern) => pattern switch
    {
        PatternKind.Circle => "圆形",
        PatternKind.Semicircle => "半圆",
        PatternKind.Heart => "心形",
        PatternKind.Star => "星形",
        PatternKind.Wave => "波浪线",
        PatternKind.Diagonal => "斜线",
        PatternKind.SquareGrid => "方阵",
        PatternKind.Ring => "环形",
        PatternKind.VShape => "V 字形",
        PatternKind.XShape => "X 字形",
        PatternKind.Text => "文字轮廓",
        PatternKind.ImageMask => "图片蒙版",
        PatternKind.ManualPoints => "手动点位",
        _ => pattern.ToString(),
    };

    public static string ToDisplayText(this PatternFillMode fillMode) => fillMode switch
    {
        PatternFillMode.LeftToRight => "从左到右",
        PatternFillMode.TopToBottom => "从上到下",
        PatternFillMode.CenterOut => "从中心向外",
        PatternFillMode.Clockwise => "顺时针",
        _ => fillMode.ToString(),
    };
}
