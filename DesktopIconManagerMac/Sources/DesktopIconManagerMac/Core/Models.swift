import Foundation

enum LayoutMode: String, Codable, CaseIterable, Identifiable {
    case left = "Left"
    case right = "Right"
    case top = "Top"
    case bottom = "Bottom"
    case centerCompact = "CenterCompact"
    case customPattern = "CustomPattern"
    case desktopZones = "DesktopZones"

    var id: String { rawValue }
}

enum SortMode: String, Codable, CaseIterable, Identifiable {
    case typeThenName = "TypeThenName"
    case nameAscending = "NameAscending"
    case currentOrder = "CurrentOrder"
    case lastModifiedTime = "LastModifiedTime"
    case fileSize = "FileSize"
    case `extension` = "Extension"
    case usageFrequency = "UsageFrequency"
    case manualPriority = "ManualPriority"

    var id: String { rawValue }
}

enum IconCategory: String, Codable, CaseIterable, Identifiable {
    case folder = "Folder"
    case shortcutOrApp = "ShortcutOrApp"
    case document = "Document"
    case image = "Image"
    case media = "Media"
    case archive = "Archive"
    case system = "System"
    case other = "Other"

    var id: String { rawValue }
}

enum PatternKind: String, Codable, CaseIterable, Identifiable {
    case circle = "Circle"
    case semicircle = "Semicircle"
    case heart = "Heart"
    case star = "Star"
    case wave = "Wave"
    case diagonal = "Diagonal"
    case squareGrid = "SquareGrid"
    case ring = "Ring"
    case vShape = "VShape"
    case xShape = "XShape"
    case text = "Text"
    case imageMask = "ImageMask"
    case manualPoints = "ManualPoints"

    var id: String { rawValue }
}

enum PatternFillMode: String, Codable, CaseIterable, Identifiable {
    case leftToRight = "LeftToRight"
    case topToBottom = "TopToBottom"
    case centerOut = "CenterOut"
    case clockwise = "Clockwise"

    var id: String { rawValue }
}

enum RuleMatchKind: String, Codable, CaseIterable {
    case nameContains = "NameContains"
    case category = "Category"
    case stableKey = "StableKey"
}

enum RuleActionKind: String, Codable, CaseIterable {
    case exclude = "Exclude"
    case sendToZone = "SendToZone"
    case pin = "Pin"
    case prioritize = "Prioritize"
}

struct DesktopIconInfo: Codable, Identifiable, Hashable {
    var id: String { stableKey.isEmpty ? "\(index)-\(displayName)" : stableKey }
    var index: Int
    var displayName: String
    var category: IconCategory
    var position: Point
    var stableKey: String
    var filePath: String?
    var fileExtension: String?
    var lastModifiedAt: Date?
    var fileSizeBytes: Int64?
    var isShortcut: Bool
    var shortcutTargetPath: String?
    var shortcutTargetExists: Bool?
}

struct ArrangeProfile: Codable, Identifiable, Hashable {
    var id: String { name }
    var name = "默认方案"
    var layoutMode: LayoutMode = .centerCompact
    var sortMode: SortMode = .typeThenName
    var targetScreenDeviceName: String?
    var useCurrentDesktopSpacing = true
    var leftMargin = 16
    var topMargin = 16
    var rightMargin = 16
    var bottomMargin = 16
    var columnSpacing = 112
    var rowSpacing = 96
    var startupEnabled = false
    var safeArrangeEnabled = true
    var excludeSystemIcons = false
    var snapshotRetentionCount = 10
    var sceneName = "默认场景"
    var excludedIconKeys: [String] = []
    var excludedCategories: [IconCategory] = []
    var excludedNamePatterns: [String] = []
    var pinnedIconPositions: [PinnedIconPosition] = []
    var rules: [IconRule] = []
    var tags: [IconTag] = []
    var customPattern = CustomPatternDefinition()
    var desktopZones: [DesktopZone] = []
}

struct CustomPatternDefinition: Codable, Hashable {
    var patternKind: PatternKind = .circle
    var fillMode: PatternFillMode = .leftToRight
    var centerXPercent = 50
    var centerYPercent = 50
    var widthPercent = 62
    var heightPercent = 48
    var rotationDegrees = 0
    var pointSpacing = 75
    var snapToGrid = true
    var overflowToGrid = true
    var text = "WORK"
    var imageMaskPath: String?
    var manualPoints: [PatternPoint] = []
}

struct PatternPoint: Codable, Hashable {
    var xPercent: Int
    var yPercent: Int
}

struct DesktopZone: Codable, Identifiable, Hashable {
    var id: String { name }
    var name = "新区块"
    var xPercent = 0
    var yPercent = 0
    var widthPercent = 35
    var heightPercent = 50
    var colorArgb = 0x4D78FF
    var layoutMode: LayoutMode = .left
    var sortMode: SortMode = .typeThenName
    var categories: [IconCategory] = []
    var nameContains: [String] = []
    var iconKeys: [String] = []

    func rectangle(in workArea: RectValue) -> RectValue {
        let width = max(48, workArea.width * clamp(widthPercent, 5, 100) / 100)
        let height = max(48, workArea.height * clamp(heightPercent, 5, 100) / 100)
        let maxX = max(workArea.left, workArea.right - width)
        let maxY = max(workArea.top, workArea.bottom - height)
        let x = clamp(workArea.left + workArea.width * clamp(xPercent, 0, 100) / 100, workArea.left, maxX)
        let y = clamp(workArea.top + workArea.height * clamp(yPercent, 0, 100) / 100, workArea.top, maxY)
        return RectValue(x: x, y: y, width: width, height: height)
    }
}

struct DesktopScene: Codable, Identifiable, Hashable {
    var id: String { name }
    var name = "默认场景"
    var isBuiltIn = false
    var profileName = "默认方案"
    var layoutMode: LayoutMode = .centerCompact
    var sortMode: SortMode = .typeThenName
    var excludeSystemIcons = false
    var useDesktopZones = false
    var customPattern = CustomPatternDefinition()
    var desktopZones: [DesktopZone] = []
    var excludedIconKeys: [String] = []
}

struct IconRule: Codable, Identifiable, Hashable {
    var id = UUID().uuidString
    var name = "新规则"
    var matchKind: RuleMatchKind = .nameContains
    var matchValue = ""
    var actionKind: RuleActionKind = .exclude
    var actionValue = ""
    var priority = 100
    var enabled = true
}

struct PinnedIconPosition: Codable, Hashable {
    var stableKey = ""
    var x = 0
    var y = 0
}

struct IconTag: Codable, Hashable {
    var stableKey = ""
    var name = ""
    var colorArgb = 0xFFC107
}

struct ArrangeLayout: Codable {
    var profile: ArrangeProfile
    var workArea: RectValue
    var spacing: SizeValue
    var positions: [ArrangedIconPosition]
    var excludedCount: Int
    var layoutSummary: String
}

struct ArrangedIconPosition: Codable, Identifiable, Hashable {
    var id: String { icon.stableKey + "@" + "\(targetPosition.x),\(targetPosition.y)" }
    var icon: DesktopIconInfo
    var targetPosition: Point
    var zoneName: String?
}

struct ArrangePreview: Codable {
    var iconCount: Int
    var movableCount: Int
    var excludedCount: Int
    var layoutMode: LayoutMode
    var sortMode: SortMode
    var targetScreenName: String
    var safeArrangeEnabled: Bool
}

struct LayoutSnapshot: Codable, Identifiable, Hashable {
    var id = UUID().uuidString.replacingOccurrences(of: "-", with: "")
    var createdAt = Date()
    var profileName = ""
    var layoutMode: LayoutMode = .centerCompact
    var sortMode: SortMode = .typeThenName
    var targetScreenName = ""
    var movedCount = 0
    var excludedCount = 0
    var appVersion = ProductInfo.version
    var note = ""
    var icons: [IconPositionSnapshot] = []
}

struct IconPositionSnapshot: Codable, Hashable {
    var stableKey = ""
    var displayName = ""
    var index = 0
    var x = 0
    var y = 0
}

struct ProfileStoreData: Codable {
    var defaultProfileName = "默认方案"
    var profiles: [ArrangeProfile] = []
    var scenes: [DesktopScene] = []
}

struct PendingArrangeOperation: Codable {
    var createdAt = Date()
    var reason = "整理可能未完成"
    var snapshot = LayoutSnapshot()
}

struct DesktopOrganizeFolderNames: Codable, Hashable {
    var documents = "文档"
    var images = "图片"
    var media = "视频音频"
    var archivesAndInstallers = "压缩包安装包"
}

struct AppSettings: Codable, Hashable {
    var checkUpdatesOnStartup = true
    var lastUpdateCheckAt: Date?
    var fileOrganizeUndoRetentionCount = 10
    var diagnosticsIncludeFullPaths = false
    var firstRunGuideCompleted = false
    var updateManifestUrl = ProductInfo.defaultUpdateManifestURL
    var desktopOrganizeFolderNames = DesktopOrganizeFolderNames()
}

enum FileOrganizeTargetKind: String, Codable, CaseIterable {
    case document = "Document"
    case image = "Image"
    case media = "Media"
    case archiveOrInstaller = "ArchiveOrInstaller"
    case other = "Other"
}

struct FileOrganizeSuggestion: Codable, Identifiable, Hashable {
    var id = UUID().uuidString.replacingOccurrences(of: "-", with: "")
    var fileName = ""
    var originalPath = ""
    var category: IconCategory = .other
    var targetKind: FileOrganizeTargetKind = .other
    var suggestedTargetPath = ""
    var sizeBytes: Int64 = 0
    var lastModifiedAt: Date?
    var defaultSelected = false
    var reason = ""
}

struct FileOrganizeOperation: Codable, Identifiable, Hashable {
    var id = UUID().uuidString.replacingOccurrences(of: "-", with: "")
    var createdAt = Date()
    var isUndoOperation = false
    var isUndone = false
    var results: [FileOrganizeMoveResult] = []
}

struct FileOrganizeMoveResult: Codable, Hashable {
    var fileName = ""
    var originalPath = ""
    var targetPath = ""
    var success = false
    var status = ""
    var errorMessage = ""
}
