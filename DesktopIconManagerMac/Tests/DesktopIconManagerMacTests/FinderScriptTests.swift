import AppKit
import XCTest
@testable import DesktopIconManagerMac

final class FinderScriptTests: XCTestCase {
    func testFinderScriptsCompile() {
        for source in [DesktopService.readPositionsScript, DesktopService.applyPositionsScript] {
            var error: NSDictionary?
            let script = NSAppleScript(source: source)
            XCTAssertNotNil(script)
            XCTAssertTrue(script?.compileAndReturnError(&error) == true, String(describing: error))
        }
    }

    func testOsaScriptReceivesArguments() throws {
        let process = Process()
        let output = Pipe()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/osascript")
        process.arguments = [
            "-e",
            "on run argv\nreturn (item 1 of argv) & \"|\" & (item 2 of argv)\nend run",
            "--",
            "报告",
            "42"
        ]
        process.standardOutput = output
        try process.run()
        process.waitUntilExit()

        XCTAssertEqual(process.terminationStatus, 0)
        let result = String(data: output.fileHandleForReading.readDataToEndOfFile(), encoding: .utf8)?
            .trimmingCharacters(in: .whitespacesAndNewlines)
        XCTAssertEqual(result, "报告|42")
    }
}
