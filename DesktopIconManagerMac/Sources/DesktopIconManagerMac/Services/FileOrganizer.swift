import Foundation

enum FileOrganizer {
    static func generateSuggestions(settings: AppSettings, desktopService: DesktopService = DesktopService()) -> [FileOrganizeSuggestion] {
        let targetDirectories = targetDirectories(settings: settings, desktopService: desktopService)
        let targetSet = Set(targetDirectories.values.map { normalize($0.path) })
        let desktop = desktopService.desktopURL()

        let files: [URL]
        do {
            files = try FileManager.default.contentsOfDirectory(
                at: desktop,
                includingPropertiesForKeys: [.isDirectoryKey, .fileSizeKey, .contentModificationDateKey],
                options: [.skipsHiddenFiles, .skipsPackageDescendants]
            )
        } catch {
            AppLogger.log("扫描桌面文件失败。", error: error)
            return []
        }

        return files.compactMap { url in
            guard shouldInclude(url: url, targetDirectories: targetSet) else {
                return nil
            }

            let kind = targetKind(extension: "." + url.pathExtension.lowercased())
            guard kind != .other, let targetDirectory = targetDirectories[kind] else {
                return nil
            }

            let target = targetDirectory.appendingPathComponent(url.lastPathComponent)
            let values = try? url.resourceValues(forKeys: [.fileSizeKey, .contentModificationDateKey])
            return FileOrganizeSuggestion(
                fileName: url.lastPathComponent,
                originalPath: url.path,
                category: category(for: kind),
                targetKind: kind,
                suggestedTargetPath: target.path,
                sizeBytes: values?.fileSize.map(Int64.init) ?? 0,
                lastModifiedAt: values?.contentModificationDate,
                defaultSelected: true,
                reason: reason(for: kind)
            )
        }
        .sorted {
            if $0.targetKind != $1.targetKind {
                return $0.targetKind.rawValue < $1.targetKind.rawValue
            }
            return $0.fileName.localizedCaseInsensitiveCompare($1.fileName) == .orderedAscending
        }
    }

    static func execute(suggestions: [FileOrganizeSuggestion], settings: AppSettings) -> FileOrganizeOperation {
        var operation = FileOrganizeOperation(isUndoOperation: false)
        operation.results = suggestions.map { move(sourcePath: $0.originalPath, targetPath: $0.suggestedTargetPath, undo: false) }
        FileOrganizeOperationStore.add(operation, retentionCount: settings.fileOrganizeUndoRetentionCount)
        return operation
    }

    static func undoLatest(settings: AppSettings) -> FileOrganizeOperation? {
        guard let latest = FileOrganizeOperationStore.latestUndoable() else {
            return nil
        }

        var undo = FileOrganizeOperation(isUndoOperation: true)
        undo.results = latest.results.filter(\.success).map {
            move(sourcePath: $0.targetPath, targetPath: $0.originalPath, undo: true)
        }
        if undo.results.contains(where: \.success) {
            FileOrganizeOperationStore.markUndone(latest.id)
        }
        FileOrganizeOperationStore.add(undo, retentionCount: settings.fileOrganizeUndoRetentionCount)
        return undo
    }

    static func targetDirectories(settings: AppSettings, desktopService: DesktopService = DesktopService()) -> [FileOrganizeTargetKind: URL] {
        let names = settings.desktopOrganizeFolderNames
        let desktop = desktopService.desktopURL()
        return [
            .document: desktop.appendingPathComponent(sanitizeFolderName(names.documents, fallback: "文档"), isDirectory: true),
            .image: desktop.appendingPathComponent(sanitizeFolderName(names.images, fallback: "图片"), isDirectory: true),
            .media: desktop.appendingPathComponent(sanitizeFolderName(names.media, fallback: "视频音频"), isDirectory: true),
            .archiveOrInstaller: desktop.appendingPathComponent(sanitizeFolderName(names.archivesAndInstallers, fallback: "压缩包安装包"), isDirectory: true)
        ]
    }

    private static func move(sourcePath: String, targetPath: String, undo: Bool) -> FileOrganizeMoveResult {
        var result = FileOrganizeMoveResult(
            fileName: URL(fileURLWithPath: sourcePath).lastPathComponent,
            originalPath: sourcePath,
            targetPath: targetPath
        )

        do {
            guard FileManager.default.fileExists(atPath: sourcePath) else {
                result.status = "跳过"
                result.errorMessage = undo ? "目标位置文件已不存在" : "原文件不存在"
                return result
            }

            if FileManager.default.fileExists(atPath: targetPath) {
                result.status = "冲突"
                result.errorMessage = undo ? "原位置已有同名项目，未覆盖" : "目标位置已有同名项目，未覆盖"
                return result
            }

            let targetURL = URL(fileURLWithPath: targetPath)
            try FileManager.default.createDirectory(at: targetURL.deletingLastPathComponent(), withIntermediateDirectories: true)
            try FileManager.default.moveItem(at: URL(fileURLWithPath: sourcePath), to: targetURL)
            result.success = true
            result.status = undo ? "已撤销" : "已移动"
            return result
        } catch {
            result.status = "失败"
            result.errorMessage = error.localizedDescription
            AppLogger.log(undo ? "撤销文件收纳失败：\(sourcePath)" : "文件收纳失败：\(sourcePath)", error: error)
            return result
        }
    }

    private static func shouldInclude(url: URL, targetDirectories: Set<String>) -> Bool {
        let name = url.lastPathComponent
        if name.hasPrefix("~$") || name == ".DS_Store" {
            return false
        }

        let values = try? url.resourceValues(forKeys: [.isDirectoryKey])
        if values?.isDirectory == true {
            return false
        }

        let normalized = normalize(url.path)
        if targetDirectories.contains(normalized) {
            return false
        }

        var parent = url.deletingLastPathComponent()
        while parent.path != "/" {
            if targetDirectories.contains(normalize(parent.path)) {
                return false
            }
            let next = parent.deletingLastPathComponent()
            if next == parent {
                break
            }
            parent = next
        }

        return true
    }

    private static func targetKind(extension ext: String) -> FileOrganizeTargetKind {
        if documentExtensions.contains(ext) { return .document }
        if imageExtensions.contains(ext) { return .image }
        if mediaExtensions.contains(ext) { return .media }
        if archiveExtensions.contains(ext) || installerExtensions.contains(ext) { return .archiveOrInstaller }
        return .other
    }

    private static func category(for kind: FileOrganizeTargetKind) -> IconCategory {
        switch kind {
        case .document: .document
        case .image: .image
        case .media: .media
        case .archiveOrInstaller: .archive
        case .other: .other
        }
    }

    private static func reason(for kind: FileOrganizeTargetKind) -> String {
        switch kind {
        case .document: "文档文件，建议收纳到桌面/文档"
        case .image: "图片文件，建议收纳到桌面/图片"
        case .media: "视频或音频文件，建议收纳到桌面/视频音频"
        case .archiveOrInstaller: "压缩包或安装包，建议收纳到桌面/压缩包安装包"
        case .other: "建议收纳"
        }
    }

    private static func sanitizeFolderName(_ value: String, fallback: String) -> String {
        var text = value.trimmingCharacters(in: .whitespacesAndNewlines)
        if text.isEmpty {
            text = fallback
        }
        for character in CharacterSet(charactersIn: "/:").characters {
            text = text.replacingOccurrences(of: String(character), with: "_")
        }
        return text.isEmpty ? fallback : text
    }

    private static func normalize(_ path: String) -> String {
        URL(fileURLWithPath: path).standardizedFileURL.path.trimmingCharacters(in: CharacterSet(charactersIn: "/"))
    }

    private static let documentExtensions: Set<String> = [".txt", ".rtf", ".md", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".pdf", ".csv", ".pages", ".numbers", ".key"]
    private static let imageExtensions: Set<String> = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".heic", ".tiff"]
    private static let mediaExtensions: Set<String> = [".mp3", ".wav", ".flac", ".aac", ".m4a", ".mp4", ".mov", ".avi", ".mkv", ".webm"]
    private static let archiveExtensions: Set<String> = [".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso", ".dmg"]
    private static let installerExtensions: Set<String> = [".pkg", ".mpkg"]
}

private extension CharacterSet {
    var characters: [Character] {
        var result: [Character] = []
        for scalar in 0..<128 {
            guard let unicode = UnicodeScalar(scalar), contains(unicode) else {
                continue
            }
            result.append(Character(unicode))
        }
        return result
    }
}
