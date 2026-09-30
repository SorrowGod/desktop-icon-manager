import SwiftUI

struct LayoutView: View {
    @EnvironmentObject private var appState: AppState
    @State private var searchText = ""
    @State private var categoryFilter: IconCategory?
    @State private var showingDeleteProfile = false

    var body: some View {
        HStack(spacing: 0) {
            VStack(alignment: .leading, spacing: 14) {
                ScrollView {
                    VStack(alignment: .leading, spacing: 16) {
                        profilePicker
                        scenePicker
                        layoutControls
                        if appState.currentProfile.layoutMode == .customPattern {
                            patternControls
                        }
                        if appState.currentProfile.layoutMode == .desktopZones {
                            zoneControls
                        }
                        iconList
                    }
                }
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
                    if let layout = appState.currentLayout, layout.excludedCount > 0 {
                        Text("排除 \(layout.excludedCount) 个")
                            .foregroundStyle(.secondary)
                    }
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
            HStack {
                Picker("方案", selection: $appState.selectedProfileName) {
                    ForEach(appState.profileData.profiles) { profile in
                        Text(profile.name).tag(profile.name)
                    }
                }
                .onChange(of: appState.selectedProfileName) { _ in appState.selectProfile() }
                Button {
                    appState.addProfile()
                } label: {
                    Image(systemName: "plus")
                }
                .help("复制当前方案")
                Button {
                    showingDeleteProfile = true
                } label: {
                    Image(systemName: "trash")
                }
                .help("删除当前方案")
                .disabled(appState.profileData.profiles.count <= 1)
            }
            .confirmationDialog("删除方案 \(appState.selectedProfileName)？", isPresented: $showingDeleteProfile) {
                Button("删除方案", role: .destructive) { appState.deleteCurrentProfile() }
            }
        }
    }

    private var scenePicker: some View {
        Picker("场景", selection: Binding(
            get: { appState.currentProfile.sceneName },
            set: { name in
                if let scene = appState.profileData.scenes.first(where: { $0.name == name }) {
                    appState.applyScene(scene)
                }
            }
        )) {
            ForEach(appState.profileData.scenes) { scene in
                Text(scene.name).tag(scene.name)
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
                    appState.syncStartup()
                }
            Stepper("列距 \(appState.currentProfile.columnSpacing)", value: binding(\.columnSpacing), in: 48...240, step: 8)
            Stepper("行距 \(appState.currentProfile.rowSpacing)", value: binding(\.rowSpacing), in: 48...240, step: 8)
            Toggle("使用当前桌面间距", isOn: binding(\.useCurrentDesktopSpacing))
        }
    }

    private var patternControls: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("图案设置").font(.headline)
            Picker("图案", selection: binding(\.customPattern.patternKind)) {
                ForEach(PatternKind.allCases) { kind in
                    Text(kind.displayText).tag(kind)
                }
            }
            Picker("填充", selection: binding(\.customPattern.fillMode)) {
                Text("从左到右").tag(PatternFillMode.leftToRight)
                Text("从上到下").tag(PatternFillMode.topToBottom)
                Text("从中心向外").tag(PatternFillMode.centerOut)
                Text("顺时针").tag(PatternFillMode.clockwise)
            }
            Stepper("宽度 \(appState.currentProfile.customPattern.widthPercent)%", value: binding(\.customPattern.widthPercent), in: 10...100, step: 2)
            Stepper("高度 \(appState.currentProfile.customPattern.heightPercent)%", value: binding(\.customPattern.heightPercent), in: 10...100, step: 2)
            Stepper("旋转 \(appState.currentProfile.customPattern.rotationDegrees)°", value: binding(\.customPattern.rotationDegrees), in: -180...180, step: 15)
            if appState.currentProfile.customPattern.patternKind == .text {
                TextField("文字", text: binding(\.customPattern.text))
            }
            if appState.currentProfile.customPattern.patternKind == .imageMask {
                Button {
                    appState.chooseImageMask()
                } label: {
                    Label("选择图片", systemImage: "photo.on.rectangle")
                }
                if let path = appState.currentProfile.customPattern.imageMaskPath {
                    Text(URL(fileURLWithPath: path).lastPathComponent)
                        .font(.caption)
                        .foregroundStyle(.secondary)
                        .lineLimit(1)
                }
            }
            if appState.currentProfile.customPattern.patternKind == .manualPoints {
                Button {
                    appState.recordManualPattern()
                } label: {
                    Label("记录当前桌面位置", systemImage: "scope")
                }
                .disabled(!appState.canArrange)
            }
        }
    }

    private var zoneControls: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("桌面分区").font(.headline)
            ForEach(Array(appState.currentProfile.desktopZones.enumerated()), id: \.offset) { index, zone in
                DisclosureGroup(zone.name) {
                    VStack(alignment: .leading, spacing: 6) {
                        TextField("名称", text: zoneBinding(index, \.name))
                        Picker("排列", selection: zoneBinding(index, \.layoutMode)) {
                            ForEach([LayoutMode.left, .right, .top, .bottom, .centerCompact]) { mode in
                                Text(mode.displayText).tag(mode)
                            }
                        }
                        Stepper("左 \(zone.xPercent)%", value: zoneBinding(index, \.xPercent), in: 0...95, step: 2)
                        Stepper("上 \(zone.yPercent)%", value: zoneBinding(index, \.yPercent), in: 0...95, step: 2)
                        Stepper("宽 \(zone.widthPercent)%", value: zoneBinding(index, \.widthPercent), in: 5...100, step: 2)
                        Stepper("高 \(zone.heightPercent)%", value: zoneBinding(index, \.heightPercent), in: 5...100, step: 2)
                        DisclosureGroup("匹配类型") {
                            ForEach(IconCategory.allCases) { category in
                                Toggle(category.displayText, isOn: Binding(
                                    get: { appState.currentProfile.desktopZones[index].categories.contains(category) },
                                    set: { enabled in
                                        var profile = appState.currentProfile
                                        guard profile.desktopZones.indices.contains(index) else { return }
                                        profile.desktopZones[index].categories.removeAll { $0 == category }
                                        if enabled { profile.desktopZones[index].categories.append(category) }
                                        appState.currentProfile = profile
                                    }
                                ))
                            }
                        }
                        Button(role: .destructive) {
                            var profile = appState.currentProfile
                            profile.desktopZones.remove(at: index)
                            appState.currentProfile = profile
                        } label: {
                            Label("删除分区", systemImage: "trash")
                        }
                    }
                    .padding(.vertical, 6)
                }
            }
            Button {
                var profile = appState.currentProfile
                var zone = DesktopZone()
                zone.name = "分区 \(profile.desktopZones.count + 1)"
                profile.desktopZones.append(zone)
                appState.currentProfile = profile
            } label: {
                Label("添加分区", systemImage: "plus")
            }
        }
    }

    private var iconList: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("桌面项目")
                .font(.headline)
            TextField("搜索桌面项目", text: $searchText)
                .textFieldStyle(.roundedBorder)
            Picker("类型", selection: $categoryFilter) {
                Text("全部类型").tag(nil as IconCategory?)
                ForEach(IconCategory.allCases) { category in
                    Text(category.displayText).tag(category as IconCategory?)
                }
            }
            List(filteredIcons) { icon in
                Toggle(isOn: Binding(
                    get: { appState.isExcluded(icon) },
                    set: { appState.setExcluded($0, icon: icon) }
                )) {
                    HStack {
                        Image(systemName: symbol(for: icon.category))
                        Text(icon.displayName)
                            .lineLimit(1)
                    }
                }
                .toggleStyle(.checkbox)
                .help("勾选以排除此项目")
            }
            .frame(minHeight: 220)
        }
    }

    private var filteredIcons: [DesktopIconInfo] {
        appState.icons.filter { icon in
            (categoryFilter == nil || icon.category == categoryFilter) &&
                (searchText.isEmpty || icon.displayName.localizedCaseInsensitiveContains(searchText))
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
            .disabled(!appState.canArrange)

            Spacer()

            Button {
                Task { await appState.applyCurrentLayout() }
            } label: {
                Label("应用", systemImage: "checkmark.circle")
            }
            .buttonStyle(.borderedProminent)
            .disabled(!appState.canArrange)
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

    private func zoneBinding<Value>(_ index: Int, _ keyPath: WritableKeyPath<DesktopZone, Value>) -> Binding<Value> {
        Binding {
            appState.currentProfile.desktopZones[index][keyPath: keyPath]
        } set: { value in
            var profile = appState.currentProfile
            guard profile.desktopZones.indices.contains(index) else { return }
            profile.desktopZones[index][keyPath: keyPath] = value
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
