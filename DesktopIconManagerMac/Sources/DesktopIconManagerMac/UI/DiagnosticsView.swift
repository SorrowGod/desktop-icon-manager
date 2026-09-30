import SwiftUI

struct DiagnosticsView: View {
    @EnvironmentObject private var appState: AppState

    var body: some View {
        VStack(alignment: .leading, spacing: 18) {
            Text("诊断更新")
                .font(.title2.weight(.semibold))

            GroupBox("更新") {
                VStack(alignment: .leading, spacing: 10) {
                    TextField("更新清单地址", text: $appState.settings.updateManifestUrl)
                        .textFieldStyle(.roundedBorder)
                        .onSubmit { appState.saveSettings() }
                    HStack {
                        Button {
                            appState.saveSettings()
                            Task { await appState.checkUpdates() }
                        } label: {
                            Label("检查更新", systemImage: "arrow.down.circle")
                        }
                        if let result = appState.updateResult {
                            Text(result.message)
                                .foregroundStyle(result.hasUpdate ? .orange : .secondary)
                            if result.hasUpdate {
                                Button("下载 Mac 版") { appState.openUpdateDownload() }
                            }
                        }
                    }
                }
                .padding(8)
            }

            GroupBox("诊断") {
                VStack(alignment: .leading, spacing: 10) {
                    Toggle("诊断包包含完整路径", isOn: $appState.settings.diagnosticsIncludeFullPaths)
                        .onChange(of: appState.settings.diagnosticsIncludeFullPaths) { _ in appState.saveSettings() }
                    Button {
                        appState.exportDiagnostics()
                    } label: {
                        Label("导出诊断包", systemImage: "square.and.arrow.up")
                    }
                }
                .padding(8)
            }

            GroupBox("Mac 平台提示") {
                VStack(alignment: .leading, spacing: 8) {
                    Label("首次应用布局时，macOS 可能要求允许本应用控制 Finder。", systemImage: "lock.shield")
                    Label("iCloud Desktop、Stage Manager 或隐藏桌面图标可能影响 Finder 图标位置。", systemImage: "desktopcomputer.trianglebadge.exclamationmark")
                    Label("当前版本按非 App Store 分发设计，使用 .dmg 安装。", systemImage: "shippingbox")
                }
                .font(.callout)
                .padding(8)
            }

            Spacer()
        }
        .padding(18)
        .onChange(of: appState.settings) { _ in appState.scheduleSettingsSave() }
    }
}
