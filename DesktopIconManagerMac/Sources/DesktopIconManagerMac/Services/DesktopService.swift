import AppKit
import Foundation

struct DesktopApplyResult {
    var movedCount: Int
    var skippedCount: Int
    var warnings: [String]
}

struct DesktopScanResult {
    var icons: [DesktopIconInfo]
    var positionsReliable: Bool
    var warning: String?
}

struct DesktopService: Sendable {

    func desktopURL() -> URL {
        FileManager.default.urls(for: .desktopDirectory, in: .userDomainMask).first
            ?? URL(fileURLWithPath: NSHomeDirectory()).appendingPathComponent("Desktop")
    }

    func workArea() -> RectValue {
        if let screen = NSScreen.screens.first {
            let frame = screen.frame
            let visible = screen.visibleFrame
            return RectValue(
                x: Int((visible.minX - frame.minX).rounded()),
                y: Int((frame.maxY - visible.maxY).rounded()),
                width: Int(visible.width.rounded()),
                height: Int(visible.height.rounded())
            )
        }
        return RectValue(x: 0, y: 0, width: 1440, height: 900)
    }

    func currentSpacing() -> SizeValue {
        SizeValue(width: 112, height: 96)
    }

    func scanDesktop() async -> DesktopScanResult {
        await Task.detached(priority: .userInitiated) {
            scanDesktopSync()
        }.value
    }

    private func scanDesktopSync() -> DesktopScanResult {
        let desktop = desktopURL()
        let urls: [URL]
        do {
            urls = try FileManager.default.contentsOfDirectory(
                at: desktop,
                includingPropertiesForKeys: [.isDirectoryKey, .contentModificationDateKey, .fileSizeKey, .isHiddenKey],
                options: [.skipsPackageDescendants]
            )
        } catch {
            AppLogger.log("读取 Mac 桌面失败：\(desktop.path)", error: error)
            return DesktopScanResult(icons: [], positionsReliable: false, warning: "无法读取桌面文件：\(error.localizedDescription)")
        }

        let visibleURLs = urls
            .filter { !$0.lastPathComponent.hasPrefix(".") }
            .sorted { $0.lastPathComponent.localizedStandardCompare($1.lastPathComponent) == .orderedAscending }
        let finder = visibleURLs.isEmpty
            ? FinderResult(status: 0, output: "", error: "")
            : runFinder(script: Self.readPositionsScript, arguments: visibleURLs.map(\.lastPathComponent))
        let lines = finder.output.components(separatedBy: .newlines).filter { !$0.isEmpty }
        let parsedPositions = lines.map { line -> Point? in
            let parts = line.split(separator: ",")
            guard parts.count == 2, let x = Int(parts[0]), let y = Int(parts[1]) else { return nil }
            return Point(x: x, y: y)
        }
        let positionsReliable = finder.status == 0 && lines.count == visibleURLs.count && parsedPositions.allSatisfy { $0 != nil }
        let warning = positionsReliable ? nil : "无法从 Finder 读取全部图标位置。请允许桌面管理器控制 Finder，并检查桌面图标是否可见。"

        let icons = visibleURLs
            .enumerated()
            .map { index, url in
                let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .contentModificationDateKey, .fileSizeKey])
                let category = classify(url: url, isDirectory: values?.isDirectory == true)
                return DesktopIconInfo(
                    index: index,
                    displayName: url.deletingPathExtension().lastPathComponent.isEmpty ? url.lastPathComponent : url.deletingPathExtension().lastPathComponent,
                    category: category,
                    position: index < parsedPositions.count ? (parsedPositions[index] ?? Point(x: 0, y: 0)) : Point(x: 0, y: 0),
                    stableKey: stableKey(url: url, category: category),
                    filePath: url.path,
                    fileExtension: url.pathExtension.isEmpty ? nil : "." + url.pathExtension.lowercased(),
                    lastModifiedAt: values?.contentModificationDate,
                    fileSizeBytes: values?.fileSize.map(Int64.init),
                    isShortcut: ["alias", "webloc"].contains(url.pathExtension.lowercased()),
                    shortcutTargetPath: nil,
                    shortcutTargetExists: nil
                )
            }
        return DesktopScanResult(icons: icons, positionsReliable: positionsReliable, warning: warning)
    }

    func apply(layout: ArrangeLayout) async -> DesktopApplyResult {
        await Task.detached(priority: .userInitiated) {
            applySync(layout: layout)
        }.value
    }

    private func applySync(layout: ArrangeLayout) -> DesktopApplyResult {
        let desktopPath = desktopURL().standardizedFileURL.path
        let positions = layout.positions.filter { position in
            guard let path = position.icon.filePath else { return false }
            return URL(fileURLWithPath: path).deletingLastPathComponent().standardizedFileURL.path == desktopPath
        }
        guard !positions.isEmpty else {
            return DesktopApplyResult(movedCount: 0, skippedCount: layout.positions.count, warnings: ["没有可由 Finder 移动的桌面项目。"])
        }

        let arguments = positions.compactMap { position -> [String]? in
            guard let path = position.icon.filePath else { return nil }
            return [
                URL(fileURLWithPath: path).lastPathComponent,
                String(position.targetPosition.x),
                String(position.targetPosition.y)
            ]
        }
        let response = runFinder(script: Self.applyPositionsScript, arguments: arguments)
        let lines = response.output.components(separatedBy: .newlines).filter { !$0.isEmpty }
        guard response.status == 0, lines.count == positions.count else {
            AppLogger.log("Finder 布局命令失败：\(response.error)")
            return DesktopApplyResult(movedCount: 0, skippedCount: layout.positions.count, warnings: ["Finder 未完成布局操作。请检查自动化权限和桌面排列设置。"])
        }

        let accepted = zip(positions, lines).compactMap { pair in
            pair.1 == "ok" ? pair.0 : nil
        }
        let refreshed = scanDesktopSync()
        guard refreshed.positionsReliable else {
            return DesktopApplyResult(movedCount: 0, skippedCount: layout.positions.count, warnings: ["Finder 接受了 \(accepted.count) 项移动，但无法重新读取坐标确认结果。"])
        }

        let actualByPath = Dictionary(uniqueKeysWithValues: refreshed.icons.compactMap { icon -> (String, Point)? in
            guard let path = icon.filePath else { return nil }
            return (path, icon.position)
        })
        let tolerance = max(24, min(layout.spacing.width, layout.spacing.height) / 3)
        let verified = accepted.filter { position in
            guard let path = position.icon.filePath, let actual = actualByPath[path] else { return false }
            return abs(actual.x - position.targetPosition.x) <= tolerance && abs(actual.y - position.targetPosition.y) <= tolerance
        }.count
        let skipped = layout.positions.count - verified
        let warnings = skipped == 0 ? [] : ["\(skipped) 个图标未到达预期位置。请关闭 Finder 的自动排列或检查桌面网格设置。"]
        return DesktopApplyResult(movedCount: verified, skippedCount: skipped, warnings: warnings)
    }

    private func stableKey(url: URL, category: IconCategory) -> String {
        url.standardizedFileURL.path
    }

    private struct FinderResult {
        var status: Int32
        var output: String
        var error: String
    }

    private func runFinder(script: String, arguments: [String]) -> FinderResult {
        let process = Process()
        let outputPipe = Pipe()
        let errorPipe = Pipe()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/osascript")
        process.arguments = ["-e", script, "--"] + arguments
        process.standardOutput = outputPipe
        process.standardError = errorPipe
        do {
            try process.run()
            process.waitUntilExit()
            return FinderResult(
                status: process.terminationStatus,
                output: String(data: outputPipe.fileHandleForReading.readDataToEndOfFile(), encoding: .utf8) ?? "",
                error: String(data: errorPipe.fileHandleForReading.readDataToEndOfFile(), encoding: .utf8) ?? ""
            )
        } catch {
            AppLogger.log("调用 Finder 自动化失败。", error: error)
            return FinderResult(status: -1, output: "", error: error.localizedDescription)
        }
    }

    static let readPositionsScript = """
    on run argv
        set answerLines to {}
        tell application "Finder"
            repeat with fileName in argv
                try
                    set iconPoint to desktop position of item (contents of fileName) of desktop
                    set end of answerLines to ((item 1 of iconPoint as integer) as text) & "," & ((item 2 of iconPoint as integer) as text)
                on error
                    set end of answerLines to "error"
                end try
            end repeat
        end tell
        set AppleScript's text item delimiters to ASCII character 10
        return answerLines as text
    end run
    """

    static let applyPositionsScript = """
    on run argv
        set answerLines to {}
        tell application "Finder"
            repeat with itemIndex from 1 to (count of argv) by 3
                try
                    set fileName to item itemIndex of argv
                    set targetX to item (itemIndex + 1) of argv as integer
                    set targetY to item (itemIndex + 2) of argv as integer
                    set desktop position of item fileName of desktop to {targetX, targetY}
                    set end of answerLines to "ok"
                on error
                    set end of answerLines to "error"
                end try
            end repeat
        end tell
        set AppleScript's text item delimiters to ASCII character 10
        return answerLines as text
    end run
    """

    private func classify(url: URL, isDirectory: Bool) -> IconCategory {
        if isDirectory {
            if url.pathExtension.lowercased() == "app" {
                return .shortcutOrApp
            }
            return .folder
        }

        let ext = "." + url.pathExtension.lowercased()
        if Self.appExtensions.contains(ext) { return .shortcutOrApp }
        if Self.documentExtensions.contains(ext) { return .document }
        if Self.imageExtensions.contains(ext) { return .image }
        if Self.mediaExtensions.contains(ext) { return .media }
        if Self.archiveExtensions.contains(ext) { return .archive }
        return .other
    }

    private static let documentExtensions: Set<String> = [".txt", ".rtf", ".md", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".pdf", ".csv", ".pages", ".numbers", ".key"]
    private static let imageExtensions: Set<String> = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".heic", ".tiff"]
    private static let mediaExtensions: Set<String> = [".mp3", ".wav", ".flac", ".aac", ".m4a", ".mp4", ".mov", ".avi", ".mkv", ".webm"]
    private static let archiveExtensions: Set<String> = [".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".dmg", ".pkg", ".iso"]
    private static let appExtensions: Set<String> = [".app", ".alias", ".webloc", ".command", ".sh"]
}
