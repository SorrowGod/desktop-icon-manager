import AppKit
import Combine
import Foundation

@MainActor
final class AppState: ObservableObject {
    @Published var icons: [DesktopIconInfo] = []
    @Published var canArrange = false
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

    func refreshDesktop(updateStatus: Bool = true) async {
        let scan = desktopService.scanDesktop()
        icons = scan.icons
        canArrange = scan.positionsReliable && !scan.icons.isEmpty
        if let warning = scan.warning {
            lastWarnings = [warning]
        } else if updateStatus {
            lastWarnings = []
        }
        if updateStatus {
            statusText = scan.warning ?? "已读取 \(icons.count) 个桌面项目"
        }
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
        guard canArrange else {
            statusText = "Finder 图标坐标尚不可用，不能安全应用布局"
            return
        }
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
        await refreshDesktop(updateStatus: false)
    }

    func restoreLatestSnapshot() async {
        guard canArrange else {
            statusText = "Finder 图标坐标尚不可用，不能安全恢复快照"
            return
        }
        guard let snapshot = SnapshotStore.load().first else {
            statusText = "没有可恢复的快照"
            return
        }

        let byKey = Dictionary(uniqueKeysWithValues: icons.map { ($0.stableKey, $0) })
        let positions = snapshot.icons.compactMap { saved -> ArrangedIconPosition? in
            guard let icon = byKey[saved.stableKey] else {
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
        await refreshDesktop(updateStatus: false)
    }

    func refreshFileSuggestions() {
        fileSuggestions = FileOrganizer.generateSuggestions(settings: settings)
        selectedSuggestionIds = Set(fileSuggestions.filter(\.defaultSelected).map(\.id))
    }

    func executeFileOrganize() {
        let selected = fileSuggestions.filter { selectedSuggestionIds.contains($0.id) }
        guard !selected.isEmpty else { return }
        let operation = FileOrganizer.execute(suggestions: selected, settings: settings)
        statusText = "文件收纳完成：成功 \(operation.results.filter(\.success).count) 个"
        refreshFileSuggestions()
        Task { await refreshDesktop(updateStatus: false) }
    }

    func undoFileOrganize() {
        guard let operation = FileOrganizer.undoLatest(settings: settings) else {
            statusText = "没有可撤销的文件收纳记录"
            return
        }
        statusText = "撤销收纳完成：成功 \(operation.results.filter(\.success).count) 个"
        refreshFileSuggestions()
        Task { await refreshDesktop(updateStatus: false) }
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
