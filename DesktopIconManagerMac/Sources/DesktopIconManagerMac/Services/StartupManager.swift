import Foundation

enum StartupManager {
    private static let label = "com.desktopiconmanager.autoarrange"

    enum StartupError: LocalizedError {
        case installAppFirst
        case missingExecutable

        var errorDescription: String? {
            switch self {
            case .installAppFirst: "请先将桌面管理器拖入“应用程序”文件夹，再启用开机自动整理。"
            case .missingExecutable: "无法找到桌面管理器的可执行文件。"
            }
        }
    }

    static var launchAgentURL: URL {
        URL(fileURLWithPath: NSHomeDirectory())
            .appendingPathComponent("Library/LaunchAgents", isDirectory: true)
            .appendingPathComponent("\(label).plist")
    }

    static func isEnabled() -> Bool {
        FileManager.default.fileExists(atPath: launchAgentURL.path)
    }

    static func setEnabled(_ enabled: Bool, profileName: String) throws {
        if enabled {
            try createLaunchAgent(profileName: profileName)
        } else if FileManager.default.fileExists(atPath: launchAgentURL.path) {
            try FileManager.default.removeItem(at: launchAgentURL)
        }
    }

    private static func createLaunchAgent(profileName: String) throws {
        let executablePath = Bundle.main.executablePath ?? ProcessInfo.processInfo.arguments.first ?? ""
        guard !executablePath.isEmpty else { throw StartupError.missingExecutable }
        if executablePath.hasPrefix("/Volumes/") || executablePath.contains("/AppTranslocation/") {
            throw StartupError.installAppFirst
        }
        let data = try launchAgentData(executablePath: executablePath, profileName: profileName, logPath: AppPaths.logURL.path)
        try FileManager.default.createDirectory(at: launchAgentURL.deletingLastPathComponent(), withIntermediateDirectories: true)
        try data.write(to: launchAgentURL, options: .atomic)
    }

    static func launchAgentData(executablePath: String, profileName: String, logPath: String) throws -> Data {
        let plist: [String: Any] = [
            "Label": label,
            "ProgramArguments": [executablePath, "--auto-arrange", "--profile", profileName],
            "RunAtLoad": true,
            "StandardOutPath": logPath,
            "StandardErrorPath": logPath
        ]
        return try PropertyListSerialization.data(fromPropertyList: plist, format: .xml, options: 0)
    }
}
