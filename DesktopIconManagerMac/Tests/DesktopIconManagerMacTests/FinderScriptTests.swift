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
}
