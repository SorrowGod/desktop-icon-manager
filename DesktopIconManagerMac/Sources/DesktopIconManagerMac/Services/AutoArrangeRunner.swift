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

        let scan = await desktopService.scanDesktop()
        guard scan.positionsReliable else {
            AppLogger.log(scan.warning ?? "Finder 图标位置不可读取，已跳过自动整理。")
            return 1
        }
        let icons = scan.icons
        let layout = DesktopIconArranger.calculateLayout(
            profile: profile,
            icons: icons,
            workArea: desktopService.workArea(),
            spacing: desktopService.currentSpacing()
        )
        if let issue = LayoutSafety.issue(for: layout, desktopIcons: icons) {
            AppLogger.log("自动整理已跳过：\(issue)")
            return 1
        }
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
