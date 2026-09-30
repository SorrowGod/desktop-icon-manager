import SwiftUI

struct SettingsView: View {
    @EnvironmentObject private var appState: AppState

    var body: some View {
        Form {
            Section("文件收纳文件夹") {
                TextField("文档", text: $appState.settings.desktopOrganizeFolderNames.documents)
                TextField("图片", text: $appState.settings.desktopOrganizeFolderNames.images)
                TextField("视频音频", text: $appState.settings.desktopOrganizeFolderNames.media)
                TextField("压缩包安装包", text: $appState.settings.desktopOrganizeFolderNames.archivesAndInstallers)
            }
            Section("更新") {
                Toggle("启动时检查更新", isOn: $appState.settings.checkUpdatesOnStartup)
                TextField("更新清单地址", text: $appState.settings.updateManifestUrl)
            }
        }
        .padding(20)
        .onChange(of: appState.settings) { _ in
            appState.scheduleSettingsSave()
            appState.refreshFileSuggestions()
        }
    }
}
