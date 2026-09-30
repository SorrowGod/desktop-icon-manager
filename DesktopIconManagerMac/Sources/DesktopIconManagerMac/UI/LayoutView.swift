import SwiftUI

struct LayoutView: View {
    @EnvironmentObject private var appState: AppState

    var body: some View {
        HStack(spacing: 0) {
            VStack(alignment: .leading, spacing: 14) {
                profilePicker
                layoutControls
                iconList
                Spacer()
                actionButtons
            }
            .frame(width: 340)
            .padding(16)

            Divider()

            VStack(alignment: .leading, spacing: 12) {
                HStack {
                    Text(appState.currentProfile.layoutMode.displayText)
                        .font(.title2.weight(.semibold))
                    Spacer()
                    Text("\(appState.icons.count) 个桌面项目")
                        .foregroundStyle(.secondary)
                }
                PreviewCanvas(layout: appState.currentLayout, icons: appState.icons)
                warningList
            }
            .padding(18)
        }
    }

    private var profilePicker: some View {
        VStack(alignment: .leading) {
            Text("方案")
                .font(.headline)
            Picker("方案", selection: $appState.selectedProfileName) {
                ForEach(appState.profileData.profiles) { profile in
                    Text(profile.name).tag(profile.name)
                }
            }
            .onChange(of: appState.selectedProfileName) { _ in
                appState.recalculateLayout()
            }
        }
    }

    private var layoutControls: some View {
        VStack(alignment: .leading, spacing: 10) {
            Text("布局")
                .font(.headline)
            Picker("模式", selection: binding(\.layoutMode)) {
                ForEach(LayoutMode.allCases) { mode in
                    Text(mode.displayText).tag(mode)
                }
            }
            Picker("排序", selection: binding(\.sortMode)) {
                ForEach(SortMode.allCases) { mode in
                    Text(mode.displayText).tag(mode)
                }
            }
            Toggle("排除系统/特殊项目", isOn: binding(\.excludeSystemIcons))
            Toggle("开机自动整理", isOn: binding(\.startupEnabled))
                .onChange(of: appState.currentProfile.startupEnabled) { _ in
                    appState.saveSettings()
                }
            HStack {
                Stepper("列距 \(appState.currentProfile.columnSpacing)", value: binding(\.columnSpacing), in: 48...240, step: 8)
                Stepper("行距 \(appState.currentProfile.rowSpacing)", value: binding(\.rowSpacing), in: 48...240, step: 8)
            }
            Toggle("使用当前桌面间距", isOn: binding(\.useCurrentDesktopSpacing))
        }
    }

    private var iconList: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("桌面项目")
                .font(.headline)
            List(appState.icons) { icon in
                HStack {
                    Image(systemName: symbol(for: icon.category))
                    VStack(alignment: .leading, spacing: 2) {
                        Text(icon.displayName)
                            .lineLimit(1)
                        Text(icon.category.displayText)
                            .font(.caption)
                            .foregroundStyle(.secondary)
                    }
                }
            }
            .frame(minHeight: 220)
        }
    }

    private var actionButtons: some View {
        HStack {
            Button {
                Task { await appState.refreshDesktop() }
            } label: {
                Label("刷新", systemImage: "arrow.clockwise")
            }

            Button {
                Task { await appState.restoreLatestSnapshot() }
            } label: {
                Label("恢复", systemImage: "clock.arrow.circlepath")
            }

            Spacer()

            Button {
                Task { await appState.applyCurrentLayout() }
            } label: {
                Label("应用", systemImage: "checkmark.circle")
            }
            .buttonStyle(.borderedProminent)
        }
    }

    private var warningList: some View {
        Group {
            if !appState.lastWarnings.isEmpty {
                VStack(alignment: .leading, spacing: 6) {
                    ForEach(appState.lastWarnings, id: \.self) { warning in
                        Label(warning, systemImage: "exclamationmark.triangle")
                            .font(.footnote)
                            .foregroundStyle(.orange)
                    }
                }
            }
        }
    }

    private func binding<Value>(_ keyPath: WritableKeyPath<ArrangeProfile, Value>) -> Binding<Value> {
        Binding {
            appState.currentProfile[keyPath: keyPath]
        } set: { value in
            var profile = appState.currentProfile
            profile[keyPath: keyPath] = value
            appState.currentProfile = profile
        }
    }

    private func symbol(for category: IconCategory) -> String {
        switch category {
        case .folder: "folder"
        case .shortcutOrApp: "app"
        case .document: "doc.text"
        case .image: "photo"
        case .media: "play.rectangle"
        case .archive: "archivebox"
        case .system: "desktopcomputer"
        case .other: "questionmark.square"
        }
    }
}
