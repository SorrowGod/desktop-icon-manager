import SwiftUI

struct FileOrganizeView: View {
    @EnvironmentObject private var appState: AppState

    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            HStack {
                Text("文件收纳")
                    .font(.title2.weight(.semibold))
                Spacer()
                Button {
                    appState.refreshFileSuggestions()
                } label: {
                    Label("重新扫描", systemImage: "arrow.clockwise")
                }
            }

            Table(appState.fileSuggestions) {
                TableColumn("") { suggestion in
                    Toggle("", isOn: Binding {
                        appState.selectedSuggestionIds.contains(suggestion.id)
                    } set: { selected in
                        if selected {
                            appState.selectedSuggestionIds.insert(suggestion.id)
                        } else {
                            appState.selectedSuggestionIds.remove(suggestion.id)
                        }
                    })
                    .labelsHidden()
                }
                .width(32)
                TableColumn("文件", value: \.fileName)
                TableColumn("分类") { suggestion in
                    Text(suggestion.category.displayText)
                }
                TableColumn("目标") { suggestion in
                    Text(suggestion.suggestedTargetPath)
                        .lineLimit(1)
                }
                TableColumn("原因", value: \.reason)
            }

            HStack {
                Button {
                    appState.selectedSuggestionIds = Set(appState.fileSuggestions.map(\.id))
                } label: {
                    Label("全选", systemImage: "checklist.checked")
                }
                Button {
                    appState.selectedSuggestionIds.removeAll()
                } label: {
                    Label("清空", systemImage: "xmark.circle")
                }
                Button {
                    appState.undoFileOrganize()
                } label: {
                    Label("撤销上次", systemImage: "arrow.uturn.backward")
                }
                Spacer()
                Button {
                    appState.executeFileOrganize()
                } label: {
                    Label("收纳选中项", systemImage: "folder.badge.plus")
                }
                .buttonStyle(.borderedProminent)
                .disabled(appState.selectedSuggestionIds.isEmpty)
            }
        }
        .padding(18)
    }
}
