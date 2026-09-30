import AppKit
import XCTest
@testable import DesktopIconManagerMac

final class PatternMaskTests: XCTestCase {
    func testImageMaskSamplesDarkPixels() throws {
        let directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: directory) }
        let darkURL = directory.appendingPathComponent("dark.png")
        let lightURL = directory.appendingPathComponent("light.png")
        try writeImage(color: .black, to: darkURL)
        try writeImage(color: .white, to: lightURL)
        let workArea = RectValue(x: 0, y: 0, width: 500, height: 400)
        let spacing = SizeValue(width: 80, height: 80)

        let darkPoints = PatternLayoutEngine.buildPatternPoints(
            pattern: CustomPatternDefinition(patternKind: .imageMask, imageMaskPath: darkURL.path),
            requiredCount: 4,
            workArea: workArea,
            spacing: spacing
        )
        let lightPoints = PatternLayoutEngine.buildPatternPoints(
            pattern: CustomPatternDefinition(patternKind: .imageMask, imageMaskPath: lightURL.path),
            requiredCount: 4,
            workArea: workArea,
            spacing: spacing
        )

        XCTAssertEqual(darkPoints.count, 4)
        XCTAssertTrue(lightPoints.isEmpty)
    }

    private func writeImage(color: NSColor, to url: URL) throws {
        let bitmap = try XCTUnwrap(NSBitmapImageRep(
            bitmapDataPlanes: nil,
            pixelsWide: 32,
            pixelsHigh: 32,
            bitsPerSample: 8,
            samplesPerPixel: 4,
            hasAlpha: true,
            isPlanar: false,
            colorSpaceName: .deviceRGB,
            bytesPerRow: 0,
            bitsPerPixel: 0
        ))
        for y in 0..<32 {
            for x in 0..<32 {
                bitmap.setColor(color, atX: x, y: y)
            }
        }
        let data = try XCTUnwrap(bitmap.representation(using: .png, properties: [:]))
        try data.write(to: url)
    }
}
