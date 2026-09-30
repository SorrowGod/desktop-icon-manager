import Foundation

enum SettingsStore {
    static func load() -> AppSettings {
        normalize(JsonStore.load(AppSettings.self, from: AppPaths.settingsURL, default: AppSettings()))
    }

    static func save(_ settings: AppSettings) {
        do {
            try JsonStore.save(normalize(settings), to: AppPaths.settingsURL)
        } catch {
            AppLogger.log("保存设置失败。", error: error)
        }
    }

    static func normalize(_ settings: AppSettings) -> AppSettings {
        var normalized = settings
        if normalized.updateManifestUrl.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            normalized.updateManifestUrl = ProductInfo.defaultUpdateManifestURL
        }
        if normalized.desktopOrganizeFolderNames.documents.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            normalized.desktopOrganizeFolderNames.documents = "文档"
        }
        if normalized.desktopOrganizeFolderNames.images.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            normalized.desktopOrganizeFolderNames.images = "图片"
        }
        if normalized.desktopOrganizeFolderNames.media.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            normalized.desktopOrganizeFolderNames.media = "视频音频"
        }
        if normalized.desktopOrganizeFolderNames.archivesAndInstallers.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            normalized.desktopOrganizeFolderNames.archivesAndInstallers = "压缩包安装包"
        }
        return normalized
    }
}
