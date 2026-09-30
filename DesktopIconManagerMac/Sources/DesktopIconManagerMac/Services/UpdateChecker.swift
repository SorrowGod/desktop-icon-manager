import Foundation
#if canImport(FoundationNetworking)
import FoundationNetworking
#endif

struct UpdateManifest: Codable {
    var version: String
    var releaseDate: Date?
    var downloadUrl: String
    var portableUrl: String?
    var sha256: String?
    var macDmgUrl: String?
    var macSha256: String?
    var notes: [String]
}

struct UpdateCheckResult {
    var isConfigured: Bool
    var hasUpdate: Bool
    var isLatest: Bool
    var currentVersion: String
    var message: String
    var manifest: UpdateManifest?
}

enum UpdateChecker {
    static func check(settings: AppSettings) async -> UpdateCheckResult {
        let urlText = settings.updateManifestUrl.trimmingCharacters(in: .whitespacesAndNewlines)
        guard let url = URL(string: urlText), !urlText.isEmpty else {
            return UpdateCheckResult(isConfigured: false, hasUpdate: false, isLatest: false, currentVersion: ProductInfo.version, message: "未配置更新清单地址。", manifest: nil)
        }

        do {
            var decoder = JSONDecoder()
            decoder.dateDecodingStrategy = .formatted(Self.dateFormatter)
            let (data, _) = try await URLSession.shared.data(from: url)
            let manifest = try decoder.decode(UpdateManifest.self, from: data)
            let hasUpdate = compare(manifest.version, ProductInfo.version) == .orderedDescending
            return UpdateCheckResult(
                isConfigured: true,
                hasUpdate: hasUpdate,
                isLatest: !hasUpdate,
                currentVersion: ProductInfo.version,
                message: hasUpdate ? "发现新版本 \(manifest.version)" : "当前已是最新版本。",
                manifest: manifest
            )
        } catch {
            AppLogger.log("检查更新失败。", error: error)
            return UpdateCheckResult(isConfigured: true, hasUpdate: false, isLatest: false, currentVersion: ProductInfo.version, message: "检查更新失败：\(error.localizedDescription)", manifest: nil)
        }
    }

    private static func compare(_ left: String, _ right: String) -> ComparisonResult {
        let leftParts = left.split(separator: ".").map { Int($0) ?? 0 }
        let rightParts = right.split(separator: ".").map { Int($0) ?? 0 }
        for index in 0..<max(leftParts.count, rightParts.count) {
            let lhs = index < leftParts.count ? leftParts[index] : 0
            let rhs = index < rightParts.count ? rightParts[index] : 0
            if lhs > rhs { return .orderedDescending }
            if lhs < rhs { return .orderedAscending }
        }
        return .orderedSame
    }

    private static let dateFormatter: DateFormatter = {
        let formatter = DateFormatter()
        formatter.calendar = Calendar(identifier: .gregorian)
        formatter.locale = Locale(identifier: "en_US_POSIX")
        formatter.dateFormat = "yyyy-MM-dd"
        return formatter
    }()
}
