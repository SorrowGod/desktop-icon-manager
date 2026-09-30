import AppKit
import Foundation

struct DesktopApplyResult {
    var movedCount: Int
    var skippedCount: Int
    var warnings: [String]
}

final class DesktopService {
    private let fileManager = FileManager.default

    func desktopURL() -> URL {
        fileManager.urls(for: .desktopDirectory, in: .userDomainMask).first
            ?? URL(fileURLWithPath: NSHomeDirectory()).appendingPathComponent("Desktop")
    }

    func workArea() -> RectValue {
        if let screen = NSScreen.main {
            return RectValue(screen.visibleFrame)
        }
        return RectValue(x: 0, y: 0, width: 1440, height: 900)
    }

    func currentSpacing() -> SizeValue {
        SizeValue(width: 112, height: 96)
    }

    func loadDesktopIcons() -> [DesktopIconInfo] {
        let desktop = desktopURL()
        let urls: [URL]
        do {
            urls = try fileManager.contentsOfDirectory(
                at: desktop,
                includingPropertiesForKeys: [.isDirectoryKey, .contentModificationDateKey, .fileSizeKey, .isHiddenKey],
                options: [.skipsPackageDescendants]
            )
        } catch {
            AppLogger.log("读取 Mac 桌面失败：\(desktop.path)", error: error)
            return []
        }

        return urls
            .filter { !$0.lastPathComponent.hasPrefix(".") }
            .enumerated()
            .map { index, url in
                let values = try? url.resourceValues(forKeys: [.isDirectoryKey, .contentModificationDateKey, .fileSizeKey])
                let category = classify(url: url, isDirectory: values?.isDirectory == true)
                return DesktopIconInfo(
                    index: index,
                    displayName: url.deletingPathExtension().lastPathComponent.isEmpty ? url.lastPathComponent : url.deletingPathExtension().lastPathComponent,
                    category: category,
                    position: synthesizedPosition(index: index),
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
    }

    func apply(layout: ArrangeLayout) async -> DesktopApplyResult {
        let script = finderScript(for: layout.positions)
        guard !script.isEmpty else {
            return DesktopApplyResult(movedCount: 0, skippedCount: layout.positions.count, warnings: ["没有可移动的桌面项目。"])
        }

        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/osascript")
        process.arguments = ["-e", script]
        let errorPipe = Pipe()
        process.standardError = errorPipe

        do {
            try process.run()
            process.waitUntilExit()
            if process.terminationStatus == 0 {
                return DesktopApplyResult(movedCount: layout.positions.count, skippedCount: 0, warnings: [])
            }

            let errorData = errorPipe.fileHandleForReading.readDataToEndOfFile()
            let errorText = String(data: errorData, encoding: .utf8) ?? "未知 Finder 错误"
            return DesktopApplyResult(
                movedCount: 0,
                skippedCount: layout.positions.count,
                warnings: ["Finder 未允许自动控制或不支持当前桌面状态：\(errorText.trimmingCharacters(in: .whitespacesAndNewlines))"]
            )
        } catch {
            AppLogger.log("调用 Finder 设置桌面图标位置失败。", error: error)
            return DesktopApplyResult(
                movedCount: 0,
                skippedCount: layout.positions.count,
                warnings: ["无法调用 Finder 自动化。请在 系统设置 > 隐私与安全性 > 自动化 中允许桌面管理器控制 Finder。"]
            )
        }
    }

    private func finderScript(for positions: [ArrangedIconPosition]) -> String {
        let commands = positions.compactMap { position -> String? in
            guard let path = position.icon.filePath else {
                return nil
            }
            let fileName = URL(fileURLWithPath: path).lastPathComponent
            let escapedName = fileName.replacingOccurrences(of: "\\", with: "\\\\").replacingOccurrences(of: "\"", with: "\\\"")
            return """
            try
              set desktop position of item "\(escapedName)" of desktop to {\(position.targetPosition.x), \(position.targetPosition.y)}
            end try
            """
        }

        guard !commands.isEmpty else {
            return ""
        }

        return """
        tell application "Finder"
          \(commands.joined(separator: "\n  "))
        end tell
        """
    }

    private func synthesizedPosition(index: Int) -> Point {
        let spacing = currentSpacing()
        let row = index % 8
        let column = index / 8
        return Point(x: 24 + column * spacing.width, y: 24 + row * spacing.height)
    }

    private func stableKey(url: URL, category: IconCategory) -> String {
        "\(category.rawValue)|\(url.lastPathComponent)".lowercased()
    }

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
