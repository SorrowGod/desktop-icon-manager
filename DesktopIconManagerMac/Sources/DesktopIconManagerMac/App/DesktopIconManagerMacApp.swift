import AppKit
import SwiftUI

@main
struct DesktopIconManagerMacApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) private var appDelegate
    @StateObject private var appState = AppState()

    var body: some Scene {
        if CommandLine.arguments.contains("--auto-arrange") {
            Settings { EmptyView() }
        } else {
            WindowGroup {
                ContentView()
                    .environmentObject(appState)
                    .frame(minWidth: 1120, minHeight: 720)
            }
            .commands {
                CommandGroup(replacing: .newItem) {}
                CommandMenu("桌面管理器") {
                    Button("刷新桌面") {
                        Task { await appState.refreshDesktop() }
                    }
                    .keyboardShortcut("r", modifiers: [.command])

                    Button("应用当前布局") {
                        Task { await appState.applyCurrentLayout() }
                    }
                    .keyboardShortcut(.return, modifiers: [.command])
                }
            }

            Settings {
                SettingsView()
                    .environmentObject(appState)
                    .frame(width: 620, height: 420)
            }
        }
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationWillFinishLaunching(_ notification: Notification) {
        if CommandLine.arguments.contains("--auto-arrange") {
            NSApp.setActivationPolicy(.prohibited)
        }
    }

    func applicationDidFinishLaunching(_ notification: Notification) {
        let arguments = CommandLine.arguments
        guard arguments.contains("--auto-arrange") else {
            return
        }

        Task {
            let profileName = Self.value(after: "--profile", in: arguments)
            let runner = AutoArrangeRunner()
            let exitCode = await runner.run(profileName: profileName)
            NSApp.terminate(exitCode == 0 ? nil : self)
        }
    }

    private static func value(after name: String, in arguments: [String]) -> String? {
        guard let index = arguments.firstIndex(of: name), arguments.indices.contains(index + 1) else {
            return nil
        }

        return arguments[index + 1]
    }
}
