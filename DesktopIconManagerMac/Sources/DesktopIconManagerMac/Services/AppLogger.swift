import Foundation

enum AppLogger {
    static func log(_ message: String, error: Error? = nil) {
        let line = [
            ISO8601DateFormatter().string(from: Date()),
            message,
            error.map { String(describing: $0) }
        ]
        .compactMap { $0 }
        .joined(separator: " | ")
        .appending("\n")

        do {
            try FileManager.default.createDirectory(at: AppPaths.logURL.deletingLastPathComponent(), withIntermediateDirectories: true)
            if FileManager.default.fileExists(atPath: AppPaths.logURL.path),
               let handle = try? FileHandle(forWritingTo: AppPaths.logURL) {
                defer { try? handle.close() }
                try handle.seekToEnd()
                if let data = line.data(using: .utf8) {
                    try handle.write(contentsOf: data)
                }
            } else {
                try line.write(to: AppPaths.logURL, atomically: true, encoding: .utf8)
            }
        } catch {
            NSLog("DesktopIconManager log failed: %@", String(describing: error))
        }
    }
}
