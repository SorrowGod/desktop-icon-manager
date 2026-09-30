import Foundation

enum LayoutSafety {
    static func issue(for layout: ArrangeLayout, desktopIcons: [DesktopIconInfo] = []) -> String? {
        guard !layout.positions.isEmpty else { return "没有可移动的桌面项目" }
        let points = layout.positions.map(\.targetPosition)
        for index in points.indices {
            for other in points.indices where other > index {
                if overlaps(points[index], points[other], spacing: layout.spacing) {
                    return "布局点位不足，多个图标会重叠"
                }
            }
        }

        let area = layout.workArea
        let maxX = max(area.left, area.right - layout.spacing.width)
        let maxY = max(area.top, area.bottom - layout.spacing.height)
        guard points.allSatisfy({ $0.x >= area.left && $0.x <= maxX && $0.y >= area.top && $0.y <= maxY }) else {
            return "桌面空间不足，部分图标会超出可见范围"
        }
        let movingKeys = Set(layout.positions.map { $0.icon.stableKey })
        for icon in desktopIcons where !movingKeys.contains(icon.stableKey) {
            if points.contains(where: { overlaps($0, icon.position, spacing: layout.spacing) }) {
                return "布局会覆盖被排除的桌面项目"
            }
        }
        return nil
    }

    private static func overlaps(_ left: Point, _ right: Point, spacing: SizeValue) -> Bool {
        abs(left.x - right.x) < spacing.width && abs(left.y - right.y) < spacing.height
    }
}
