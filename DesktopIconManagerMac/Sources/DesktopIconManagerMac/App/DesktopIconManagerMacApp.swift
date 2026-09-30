import AppKit
import SwiftUI

@main
struct DesktopIconManagerMacApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) private var appDelegate
    @StateObject private var appState = AppState()

    var body: some Scene {
        WindowGroup {
            if CommandLine.arguments.contains("--auto-arrange") {
                Color.clear
                    .frame(width: 1, height: 1)
                    .onAppear {
                        NSApp.windows.forEach { $0.orderOut(nil) }
                    }
            } else {
                ContentView()
                    .environmentObject(appState)
                    .frame(minWidth: 1120, minHeight: 720)
            }
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
            if !CommandLine.arguments.contains("--auto-arrange") {
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
        NSApp.hide(nil)

        Task {
            let profileName = Self.value(after: "--profile", in: arguments)
            let runner = AutoArrangeRunner()
            let exitCode = await runner.run(profileName: profileName)
            if exitCode != 0 {
                AppLogger.log("开机自动整理未完成。")
            }
            NSApp.terminate(nil)
        }
    }

    private static func value(after name: String, in arguments: [String]) -> String? {
        guard let index = arguments.firstIndex(of: name), arguments.indices.contains(index + 1) else {
            return nil
        }

        return arguments[index + 1]
    }
}
