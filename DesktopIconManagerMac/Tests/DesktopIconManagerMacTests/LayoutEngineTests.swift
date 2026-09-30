import XCTest
@testable import DesktopIconManagerMac

final class LayoutEngineTests: XCTestCase {
    func testLeftLayoutUsesColumnsAfterRowsFill() {
        let profile = ArrangeProfile(layoutMode: .left, sortMode: .currentOrder, useCurrentDesktopSpacing: false, columnSpacing: 100, rowSpacing: 100)
        let icons = makeIcons(count: 6)
        let layout = DesktopIconArranger.calculateLayout(
            profile: profile,
            icons: icons,
            workArea: RectValue(x: 0, y: 0, width: 260, height: 260),
            spacing: SizeValue(width: 100, height: 100)
        )

        XCTAssertEqual(layout.positions.map(\.targetPosition), [
            Point(x: 16, y: 16),
            Point(x: 16, y: 116),
            Point(x: 116, y: 16),
            Point(x: 116, y: 116),
            Point(x: 216, y: 16),
            Point(x: 216, y: 116)
        ])
    }

    func testExcludedCategoryIsNotMoved() {
        var profile = ArrangeProfile(layoutMode: .centerCompact)
        profile.excludedCategories = [.system]
        var icons = makeIcons(count: 4)
        icons[1].category = .system

        let layout = DesktopIconArranger.calculateLayout(
            profile: profile,
            icons: icons,
            workArea: RectValue(x: 0, y: 0, width: 500, height: 400),
            spacing: SizeValue(width: 100, height: 100)
        )

        XCTAssertEqual(layout.excludedCount, 1)
        XCTAssertFalse(layout.positions.contains { $0.icon.stableKey == icons[1].stableKey })
    }

    func testDesktopZonesAssignsKnownCategories() {
        let profile = ArrangeProfile(layoutMode: .desktopZones, sortMode: .typeThenName)
        let icons = [
            icon(index: 0, name: "Safari", category: .shortcutOrApp),
            icon(index: 1, name: "Report", category: .document),
            icon(index: 2, name: "Photo", category: .image),
            icon(index: 3, name: "Todo", category: .other)
        ]

        let layout = DesktopIconArranger.calculateLayout(
            profile: profile,
            icons: icons,
            workArea: RectValue(x: 0, y: 0, width: 1200, height: 800),
            spacing: SizeValue(width: 100, height: 100)
        )

        XCTAssertEqual(Set(layout.positions.compactMap(\.zoneName)), Set(["常用软件", "工作文件", "媒体与压缩包", "待处理"]))
    }

    func testDuplicateZoneNamesDoNotCrashLayout() {
        var profile = ArrangeProfile(layoutMode: .desktopZones)
        profile.desktopZones = [
            DesktopZone(name: "工作", categories: [.document]),
            DesktopZone(name: "工作", xPercent: 50, categories: [.image])
        ]
        let icons = [
            icon(index: 0, name: "Report", category: .document),
            icon(index: 1, name: "Photo", category: .image)
        ]

        let layout = DesktopIconArranger.calculateLayout(
            profile: profile,
            icons: icons,
            workArea: RectValue(x: 0, y: 0, width: 1200, height: 800),
            spacing: SizeValue(width: 100, height: 100)
        )

        XCTAssertEqual(layout.positions.count, 2)
    }

    private func makeIcons(count: Int) -> [DesktopIconInfo] {
        (0..<count).map { icon(index: $0, name: "Icon\($0)", category: .document) }
    }

    private func icon(index: Int, name: String, category: IconCategory) -> DesktopIconInfo {
        DesktopIconInfo(
            index: index,
            displayName: name,
            category: category,
            position: Point(x: 0, y: 0),
            stableKey: "\(category.rawValue)|\(name)",
            filePath: nil,
            fileExtension: nil,
            lastModifiedAt: nil,
            fileSizeBytes: nil,
            isShortcut: false,
            shortcutTargetPath: nil,
            shortcutTargetExists: nil
        )
    }
}
