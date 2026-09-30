import Foundation

enum StartupManager {
    private static let label = "com.desktopiconmanager.autoarrange"

    static var launchAgentURL: URL {
        URL(fileURLWithPath: NSHomeDirectory())
            .appendingPathComponent("Library/LaunchAgents", isDirectory: true)
            .appendingPathComponent("\(label).plist")
    }

    static func isEnabled() -> Bool {
        FileManager.default.fileExists(atPath: launchAgentURL.path)
    }

    static func setEnabled(_ enabled: Bool, profileName: String) {
        do {
            if enabled {
                try createLaunchAgent(profileName: profileName)
            } else if FileManager.default.fileExists(atPath: launchAgentURL.path) {
                try FileManager.default.removeItem(at: launchAgentURL)
            }
        } catch {
            AppLogger.log("更新 Mac 开机自动整理失败。", error: error)
        }
    }

    private static func createLaunchAgent(profileName: String) throws {
        let executablePath = Bundle.main.executablePath ?? ProcessInfo.processInfo.arguments.first ?? ""
        let plist = """
        <?xml version="1.0" encoding="UTF-8"?>
        <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
        <plist version="1.0">
        <dict>
          <key>Label</key>
          <string>\(label)</string>
          <key>ProgramArguments</key>
          <array>
            <string>\(escape(executablePath))</string>
            <string>--auto-arrange</string>
            <string>--profile</string>
            <string>\(escape(profileName))</string>
          </array>
          <key>RunAtLoad</key>
          <true/>
          <key>StandardOutPath</key>
          <string>\(escape(AppPaths.logURL.path))</string>
          <key>StandardErrorPath</key>
          <string>\(escape(AppPaths.logURL.path))</string>
        </dict>
        </plist>
        """
        try FileManager.default.createDirectory(at: launchAgentURL.deletingLastPathComponent(), withIntermediateDirectories: true)
        try plist.write(to: launchAgentURL, atomically: true, encoding: .utf8)
    }

    private static func escape(_ value: String) -> String {
        value
            .replacingOccurrences(of: "&", with: "&amp;")
            .replacingOccurrences(of: "<", with: "&lt;")
            .replacingOccurrences(of: ">", with: "&gt;")
    }
}
