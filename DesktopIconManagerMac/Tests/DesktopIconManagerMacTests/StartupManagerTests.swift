import Foundation
import XCTest
@testable import DesktopIconManagerMac

final class StartupManagerTests: XCTestCase {
    func testLaunchAgentPlistPreservesArguments() throws {
        let data = try StartupManager.launchAgentData(
            executablePath: "/Applications/桌面管理器.app/Contents/MacOS/DesktopIconManagerMac",
            profileName: "工作 & 生活 <常用>",
            logPath: "/Users/test/Library/Application Support/DesktopIconManager/log.txt"
        )
        let plist = try XCTUnwrap(PropertyListSerialization.propertyList(from: data, options: [], format: nil) as? [String: Any])
        let arguments = try XCTUnwrap(plist["ProgramArguments"] as? [String])

        XCTAssertEqual(arguments, [
            "/Applications/桌面管理器.app/Contents/MacOS/DesktopIconManagerMac",
            "--auto-arrange",
            "--profile",
            "工作 & 生活 <常用>"
        ])
        XCTAssertEqual(plist["RunAtLoad"] as? Bool, true)
    }
}
