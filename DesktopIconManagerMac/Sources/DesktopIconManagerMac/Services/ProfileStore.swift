import Foundation

enum ProfileStore {
    static func load() -> ProfileStoreData {
        var data = JsonStore.load(ProfileStoreData.self, from: AppPaths.profilesURL, default: ProfileDefaults.defaultStoreData())
        if data.profiles.isEmpty {
            data.profiles = [ProfileDefaults.defaultProfile()]
        }
        if data.scenes.isEmpty {
            data.scenes = ProfileDefaults.defaultScenes()
        }
        if data.defaultProfileName.isEmpty {
            data.defaultProfileName = data.profiles[0].name
        }
        return data
    }

    static func save(_ data: ProfileStoreData) {
        do {
            try JsonStore.save(data, to: AppPaths.profilesURL)
        } catch {
            AppLogger.log("保存方案失败。", error: error)
        }
    }
}
