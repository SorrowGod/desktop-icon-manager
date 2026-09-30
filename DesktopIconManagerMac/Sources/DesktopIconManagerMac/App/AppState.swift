import AppKit
import Combine
import Foundation

@MainActor
final class AppState: ObservableObject {
    @Published var icons: [DesktopIconInfo] = []
    @Published var profileData: ProfileStoreData = ProfileDefaults.defaultStoreData()
    @Published var selectedProfileName = "默认方案"
    @Published var settings = AppSettings()
    @Published var currentLayout: ArrangeLayout?
    @Published var fileSuggestions: [FileOrganizeSuggestion] = []
    @Published var selectedSuggestionIds: Set<String> = []
    @Published var statusText = "就绪"
    @Published var lastWarnings: [String] = []
    @Published var updateResult: UpdateCheckResult?

    private let desktopService = DesktopService()

    var currentProfile: ArrangeProfile {
        get {
            profileData.profiles.first { $0.name == selectedProfileName }
                ?? profileData.profiles.first
                ?? ProfileDefaults.defaultProfile()
        }
        set {
            if let index = profileData.profiles.firstIndex(where: { $0.name == selectedProfileName }) {
                profileData.profiles[index] = newValue
            } else {
                profileData.profiles.append(newValue)
                selectedProfileName = newValue.name
            }
            profileData.defaultProfileName = selectedProfileName
            ProfileStore.save(profileData)
            recalculateLayout()
        }
    }

    init() {
        profileData = ProfileStore.load()
        selectedProfileName = profileData.defaultProfileName
        settings = SettingsStore.load()
        Task { await refreshDesktop() }
    }

    func refreshDesktop() async {
        icons = desktopService.loadDesktopIcons()
        statusText = "已读取 \(icons.count) 个桌面项目"
        recalculateLayout()
        refreshFileSuggestions()
    }

    func recalculateLayout() {
        currentLayout = DesktopIconArranger.calculateLayout(
            profile: currentProfile,
            icons: icons,
            workArea: desktopService.workArea(),
            spacing: desktopService.currentSpacing()
        )
    }

    func applyCurrentLayout() async {
        guard let layout = currentLayout else {
            statusText = "没有可应用的布局"
            return
        }

        SnapshotStore.add(
            DesktopIconArranger.createSnapshot(
                profileName: currentProfile.name,
                icons: icons,
                profile: currentProfile,
                movedCount: layout.positions.count,
                excludedCount: layout.excludedCount,
                note: "Mac 应用布局前快照"
            ),
            retentionCount: currentProfile.snapshotRetentionCount
        )

        let result = await desktopService.apply(layout: layout)
        lastWarnings = result.warnings
        statusText = result.warnings.isEmpty
            ? "已应用布局，移动 \(result.movedCount) 个项目"
            : "布局未完全应用，移动 \(result.movedCount) 个，跳过 \(result.skippedCount) 个"
        await refreshDesktop()
    }

    func restoreLatestSnapshot() async {
        guard let snapshot = SnapshotStore.load().first else {
            statusText = "没有可恢复的快照"
            return
        }

        let byKey = Dictionary(uniqueKeysWithValues: icons.map { ($0.stableKey.lowercased(), $0) })
        let positions = snapshot.icons.compactMap { saved -> ArrangedIconPosition? in
            guard let icon = byKey[saved.stableKey.lowercased()] else {
                return nil
            }
            return ArrangedIconPosition(icon: icon, targetPosition: Point(x: saved.x, y: saved.y), zoneName: nil)
        }
        let layout = ArrangeLayout(
            profile: currentProfile,
            workArea: desktopService.workArea(),
            spacing: desktopService.currentSpacing(),
            positions: positions,
            excludedCount: 0,
            layoutSummary: "恢复快照"
        )
        let result = await desktopService.apply(layout: layout)
        lastWarnings = result.warnings
        statusText = "恢复快照：移动 \(result.movedCount) 个项目"
        await refreshDesktop()
    }

    func refreshFileSuggestions() {
        fileSuggestions = FileOrganizer.generateSuggestions(settings: settings)
        selectedSuggestionIds = Set(fileSuggestions.filter(\.defaultSelected).map(\.id))
    }

    func executeFileOrganize() {
        let selected = fileSuggestions.filter { selectedSuggestionIds.contains($0.id) }
        let operation = FileOrganizer.execute(suggestions: selected, settings: settings)
        statusText = "文件收纳完成：成功 \(operation.results.filter(\.success).count) 个"
        refreshFileSuggestions()
        Task { await refreshDesktop() }
    }

    func undoFileOrganize() {
        guard let operation = FileOrganizer.undoLatest(settings: settings) else {
            statusText = "没有可撤销的文件收纳记录"
            return
        }
        statusText = "撤销收纳完成：成功 \(operation.results.filter(\.success).count) 个"
        refreshFileSuggestions()
        Task { await refreshDesktop() }
    }

    func checkUpdates() async {
        updateResult = await UpdateChecker.check(settings: settings)
        statusText = updateResult?.message ?? "检查更新完成"
    }

    func exportDiagnostics() {
        if let url = DiagnosticPackageBuilder.export(settings: settings, icons: icons) {
            statusText = "诊断包已导出：\(url.path)"
            NSWorkspace.shared.activateFileViewerSelecting([url])
        } else {
            statusText = "诊断包导出失败"
        }
    }

    func saveSettings() {
        settings = SettingsStore.normalize(settings)
        SettingsStore.save(settings)
        StartupManager.setEnabled(currentProfile.startupEnabled, profileName: currentProfile.name)
    }
}
