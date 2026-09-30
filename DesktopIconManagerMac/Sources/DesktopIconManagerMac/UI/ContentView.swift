import SwiftUI

struct ContentView: View {
    @EnvironmentObject private var appState: AppState
    @State private var selectedTab: SidebarTab? = .layout

    var body: some View {
        NavigationSplitView {
            List(selection: $selectedTab) {
                ForEach(SidebarTab.allCases) { tab in
                    Label(tab.title, systemImage: tab.systemImage)
                        .tag(tab)
                }
            }
            .navigationTitle("桌面管理器")
        } detail: {
            VStack(spacing: 0) {
                switch selectedTab ?? .layout {
                case .layout:
                    LayoutView()
                case .files:
                    FileOrganizeView()
                case .diagnostics:
                    DiagnosticsView()
                }
                StatusBar()
            }
        }
    }
}

private enum SidebarTab: String, CaseIterable, Identifiable {
    case layout
    case files
    case diagnostics

    var id: String { rawValue }

    var title: String {
        switch self {
        case .layout: "布局整理"
        case .files: "文件收纳"
        case .diagnostics: "诊断更新"
        }
    }

    var systemImage: String {
        switch self {
        case .layout: "rectangle.grid.3x2"
        case .files: "folder.badge.gearshape"
        case .diagnostics: "stethoscope"
        }
    }
}

private struct StatusBar: View {
    @EnvironmentObject private var appState: AppState

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Divider()
            HStack {
                Text(appState.statusText)
                    .lineLimit(1)
                Spacer()
                if !appState.lastWarnings.isEmpty {
                    Label("\(appState.lastWarnings.count) 条提示", systemImage: "exclamationmark.triangle")
                        .foregroundStyle(.orange)
                }
            }
            .font(.footnote)
            .padding(.horizontal, 14)
            .padding(.vertical, 8)
        }
    }
}
