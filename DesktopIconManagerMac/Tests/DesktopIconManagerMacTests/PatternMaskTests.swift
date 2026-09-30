import AppKit
import ImageIO
import XCTest
@testable import DesktopIconManagerMac

final class PatternMaskTests: XCTestCase {
    func testImageMaskSamplesDarkPixels() throws {
        let directory = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        defer { try? FileManager.default.removeItem(at: directory) }
        let darkURL = directory.appendingPathComponent("dark.png")
        let lightURL = directory.appendingPathComponent("light.png")
        try writeImage(isDark: true, to: darkURL)
        try writeImage(isDark: false, to: lightURL)
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

    private func writeImage(isDark: Bool, to url: URL) throws {
        let context = try XCTUnwrap(CGContext(
            data: nil,
            width: 32,
            height: 32,
            bitsPerComponent: 8,
            bytesPerRow: 128,
            space: CGColorSpaceCreateDeviceRGB(),
            bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue | CGBitmapInfo.byteOrder32Big.rawValue
        ))
        let level: CGFloat = isDark ? 0 : 1
        context.setFillColor(red: level, green: level, blue: level, alpha: 1)
        context.fill(CGRect(x: 0, y: 0, width: 32, height: 32))
        let image = try XCTUnwrap(context.makeImage())
        let destination = try XCTUnwrap(CGImageDestinationCreateWithURL(url as CFURL, "public.png" as CFString, 1, nil))
        CGImageDestinationAddImage(destination, image, nil)
        XCTAssertTrue(CGImageDestinationFinalize(destination))
    }
}
