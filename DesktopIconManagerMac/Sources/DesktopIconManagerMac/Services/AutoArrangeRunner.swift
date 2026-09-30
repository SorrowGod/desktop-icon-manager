import Foundation

final class AutoArrangeRunner {
    private let desktopService = DesktopService()

    func run(profileName: String?) async -> Int32 {
        let store = ProfileStore.load()
        let profile = store.profiles.first {
            guard let profileName, !profileName.isEmpty else {
                return $0.name == store.defaultProfileName
            }
            return $0.name.caseInsensitiveCompare(profileName) == .orderedSame
        } ?? store.profiles.first ?? ProfileDefaults.defaultProfile()

        let icons = desktopService.loadDesktopIcons()
        let layout = DesktopIconArranger.calculateLayout(
            profile: profile,
            icons: icons,
            workArea: desktopService.workArea(),
            spacing: desktopService.currentSpacing()
        )
        SnapshotStore.add(
            DesktopIconArranger.createSnapshot(profileName: profile.name, icons: icons, profile: profile, movedCount: layout.positions.count, excludedCount: layout.excludedCount, note: "开机自动整理前快照"),
            retentionCount: profile.snapshotRetentionCount
        )
        let result = await desktopService.apply(layout: layout)
        for warning in result.warnings {
            AppLogger.log(warning)
        }
        return result.movedCount > 0 || layout.positions.isEmpty ? 0 : 1
    }
}
