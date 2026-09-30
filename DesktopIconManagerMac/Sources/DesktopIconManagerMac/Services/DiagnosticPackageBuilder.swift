import Foundation

enum DiagnosticPackageBuilder {
    static func export(settings: AppSettings, icons: [DesktopIconInfo]) -> URL? {
        let filename = "DesktopIconManagerMac-Diagnostics-\(timestamp()).txt"
        let url = AppPaths.diagnosticsDirectory.appendingPathComponent(filename)
        let redactedHome = NSHomeDirectory()
        let lines = [
            "DesktopIconManager Mac",
            "Version: \(ProductInfo.version)",
            "Date: \(Date())",
            "Process64Bit: \(MemoryLayout<Int>.size == 8)",
            "Icons: \(icons.count)",
            "Startup: \(StartupManager.isEnabled())",
            "UpdateManifestUrl: \(settings.updateManifestUrl)",
            "",
            "Icons:",
            icons.map { icon in
                let path = icon.filePath ?? ""
                let safePath = settings.diagnosticsIncludeFullPaths ? path : path.replacingOccurrences(of: redactedHome, with: "~")
                return "- \(icon.displayName) | \(icon.category.displayText) | \(safePath)"
            }.joined(separator: "\n")
        ]
        .joined(separator: "\n")

        do {
            try lines.write(to: url, atomically: true, encoding: .utf8)
            return url
        } catch {
            AppLogger.log("导出诊断包失败。", error: error)
            return nil
        }
    }

    private static func timestamp() -> String {
        let formatter = DateFormatter()
        formatter.dateFormat = "yyyyMMdd-HHmmss"
        return formatter.string(from: Date())
    }
}
