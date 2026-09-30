import Foundation

enum AppPaths {
    static var appSupportDirectory: URL {
        let base = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask).first
            ?? URL(fileURLWithPath: NSHomeDirectory()).appendingPathComponent("Library/Application Support")
        let directory = base.appendingPathComponent("DesktopIconManager", isDirectory: true)
        try? FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        return directory
    }

    static var profilesURL: URL {
        appSupportDirectory.appendingPathComponent("profiles.json")
    }

    static var settingsURL: URL {
        appSupportDirectory.appendingPathComponent("settings.json")
    }

    static var snapshotsURL: URL {
        appSupportDirectory.appendingPathComponent("snapshots.json")
    }

    static var pendingOperationURL: URL {
        appSupportDirectory.appendingPathComponent("pending-operation.json")
    }

    static var fileOrganizeOperationsURL: URL {
        appSupportDirectory.appendingPathComponent("file-organize-operations.json")
    }

    static var logURL: URL {
        appSupportDirectory.appendingPathComponent("desktop-icon-manager-mac.log")
    }

    static var diagnosticsDirectory: URL {
        let directory = appSupportDirectory.appendingPathComponent("diagnostics", isDirectory: true)
        try? FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        return directory
    }
}
