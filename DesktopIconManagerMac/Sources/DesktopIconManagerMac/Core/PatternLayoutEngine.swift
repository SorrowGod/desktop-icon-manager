import AppKit
import Foundation

enum PatternLayoutEngine {
    static func calculate(
        profile: ArrangeProfile,
        icons: [DesktopIconInfo],
        workArea: RectValue,
        spacing: SizeValue
    ) -> [ArrangedIconPosition] {
        guard !icons.isEmpty else {
            return []
        }

        let pattern = profile.customPattern
        var points = buildPatternPoints(pattern: pattern, requiredCount: icons.count, workArea: workArea, spacing: spacing)
        if points.count < icons.count && pattern.overflowToGrid {
            points.append(contentsOf: buildOverflowGrid(existing: points, count: icons.count - points.count, workArea: workArea, spacing: spacing))
        }

        guard !points.isEmpty else {
            return DesktopIconArranger.calculateBasicPositions(profile: profile, orderedIcons: icons, workArea: workArea, spacing: spacing)
        }

        points = order(points: points, fillMode: pattern.fillMode, bounds: patternBounds(pattern: pattern, workArea: workArea))
        return icons.enumerated().map { index, icon in
            ArrangedIconPosition(
                icon: icon,
                targetPosition: clampToWorkArea(points[min(index, points.count - 1)], workArea: workArea, spacing: spacing),
                zoneName: nil
            )
        }
    }

    static func buildPatternPoints(
        pattern: CustomPatternDefinition,
        requiredCount: Int,
        workArea: RectValue,
        spacing: SizeValue
    ) -> [Point] {
        let count = max(1, requiredCount)
        let baseBounds = patternBounds(pattern: pattern, workArea: workArea)
        let minimumScale = estimateScale(bounds: baseBounds, spacing: spacing, requiredCount: count)
        var selected: [Point] = []

        for attempt in 0..<10 {
            let scale = max(1 + Double(attempt) * 0.16, minimumScale + Double(attempt) * 0.08)
            let bounds = expand(bounds: baseBounds, workArea: workArea, scale: scale)
            let grid = buildGrid(bounds: bounds, workArea: workArea, spacing: spacing)
            selected = rawPatternPoints(pattern: pattern, bounds: bounds, grid: grid, workArea: workArea, spacing: spacing, requiredCount: count)
            if selected.count >= count {
                break
            }
        }

        if selected.count > count {
            selected = evenlySample(selected, count: count)
        }

        return unique(selected.map { clampToWorkArea($0, workArea: workArea, spacing: spacing) })
    }

    private static func rawPatternPoints(
        pattern: CustomPatternDefinition,
        bounds: RectValue,
        grid: [Point],
        workArea: RectValue,
        spacing: SizeValue,
        requiredCount: Int
    ) -> [Point] {
        var points: [Point]
        switch pattern.patternKind {
        case .circle:
            points = selectBest(grid: grid, count: requiredCount, score: { abs(ellipseRadius($0, bounds: bounds) - 1) }, secondary: { angle($0, bounds: bounds) })
        case .semicircle:
            points = selectBest(grid: grid, count: requiredCount, score: { semicircleScore($0, bounds: bounds) }, secondary: { Double($0.x) })
        case .heart:
            points = selectBest(grid: grid, count: requiredCount, score: { abs(heartEquation($0, bounds: bounds)) }, secondary: { Double($0.y) })
        case .star:
            points = selectByDistanceToPolyline(grid: grid, vertices: starVertices(bounds: bounds), closed: true, requiredCount: requiredCount)
        case .wave:
            points = selectBest(grid: grid, count: requiredCount, score: { waveDistance($0, bounds: bounds) }, secondary: { Double($0.x) })
        case .diagonal:
            points = selectByDistanceToPolyline(grid: grid, vertices: [CGPoint(x: Double(bounds.left), y: Double(bounds.top)), CGPoint(x: Double(bounds.right), y: Double(bounds.bottom))], closed: false, requiredCount: requiredCount)
        case .squareGrid:
            points = centeredGrid(bounds: bounds, count: requiredCount, spacing: spacing)
        case .ring:
            points = selectBest(grid: grid, count: requiredCount, score: { abs(ellipseRadius($0, bounds: bounds) - 0.78) }, secondary: { angle($0, bounds: bounds) })
        case .vShape:
            points = selectByDistanceToPolyline(grid: grid, vertices: [CGPoint(x: Double(bounds.left), y: Double(bounds.top)), CGPoint(x: Double(bounds.center.x), y: Double(bounds.bottom)), CGPoint(x: Double(bounds.right), y: Double(bounds.top))], closed: false, requiredCount: requiredCount)
        case .xShape:
            points = selectByDistanceToPolyline(grid: grid, vertices: [CGPoint(x: Double(bounds.left), y: Double(bounds.top)), CGPoint(x: Double(bounds.right), y: Double(bounds.bottom)), CGPoint(x: Double(bounds.right), y: Double(bounds.top)), CGPoint(x: Double(bounds.left), y: Double(bounds.bottom))], closed: false, requiredCount: requiredCount, breakAtMiddle: true)
        case .text:
            points = textPattern(pattern.text, bounds: bounds, grid: grid, requiredCount: requiredCount)
        case .imageMask:
            points = imageMask(pattern.imageMaskPath, bounds: bounds, grid: grid, requiredCount: requiredCount)
        case .manualPoints:
            points = pattern.manualPoints.map {
                Point(
                    x: workArea.left + workArea.width * clamp($0.xPercent, 0, 100) / 100,
                    y: workArea.top + workArea.height * clamp($0.yPercent, 0, 100) / 100
                )
            }
        }

        if pattern.rotationDegrees != 0 && ![.text, .imageMask, .manualPoints].contains(pattern.patternKind) {
            points = unique(rotate(points: points, center: bounds.center, degrees: pattern.rotationDegrees).map { snap($0, to: grid) })
        }

        return unique(points)
    }

    private static func patternBounds(pattern: CustomPatternDefinition, workArea: RectValue) -> RectValue {
        let width = max(96, workArea.width * clamp(pattern.widthPercent, 10, 100) / 100)
        let height = max(96, workArea.height * clamp(pattern.heightPercent, 10, 100) / 100)
        let centerX = workArea.left + workArea.width * clamp(pattern.centerXPercent, 0, 100) / 100
        let centerY = workArea.top + workArea.height * clamp(pattern.centerYPercent, 0, 100) / 100
        let x = clamp(centerX - width / 2, workArea.left, max(workArea.left, workArea.right - width))
        let y = clamp(centerY - height / 2, workArea.top, max(workArea.top, workArea.bottom - height))
        return RectValue(x: x, y: y, width: width, height: height)
    }

    private static func expand(bounds: RectValue, workArea: RectValue, scale: Double) -> RectValue {
        let width = min(workArea.width, max(bounds.width, Int((Double(bounds.width) * scale).rounded())))
        let height = min(workArea.height, max(bounds.height, Int((Double(bounds.height) * scale).rounded())))
        let center = bounds.center
        let x = clamp(center.x - width / 2, workArea.left, max(workArea.left, workArea.right - width))
        let y = clamp(center.y - height / 2, workArea.top, max(workArea.top, workArea.bottom - height))
        return RectValue(x: x, y: y, width: width, height: height)
    }

    private static func buildGrid(bounds: RectValue, workArea: RectValue, spacing: SizeValue) -> [Point] {
        let stepX = max(48, spacing.width)
        let stepY = max(48, spacing.height)
        let columns = max(1, bounds.width / stepX + 1)
        let rows = max(1, bounds.height / stepY + 1)
        let usedWidth = max(0, (columns - 1) * stepX)
        let usedHeight = max(0, (rows - 1) * stepY)
        let startX = bounds.left + max(0, (bounds.width - usedWidth) / 2)
        let startY = bounds.top + max(0, (bounds.height - usedHeight) / 2)
        var points: [Point] = []
        for row in 0..<rows {
            for column in 0..<columns {
                points.append(clampToWorkArea(Point(x: startX + column * stepX, y: startY + row * stepY), workArea: workArea, spacing: spacing))
            }
        }
        return unique(points)
    }

    private static func estimateScale(bounds: RectValue, spacing: SizeValue, requiredCount: Int) -> Double {
        let columns = max(1, bounds.width / max(48, spacing.width) + 1)
        let rows = max(1, bounds.height / max(48, spacing.height) + 1)
        let capacity = max(1, columns * rows)
        guard capacity < requiredCount else {
            return 1
        }
        return min(2.4, sqrt(Double(requiredCount) / Double(capacity)) * 1.12)
    }

    private static func selectBest(grid: [Point], count: Int, score: (Point) -> Double, secondary: (Point) -> Double) -> [Point] {
        Array(grid.sorted {
            let leftScore = score($0)
            let rightScore = score($1)
            if leftScore != rightScore {
                return leftScore < rightScore
            }
            return secondary($0) < secondary($1)
        }.prefix(count))
    }

    private static func ellipseRadius(_ point: Point, bounds: RectValue) -> Double {
        let center = bounds.center
        let dx = Double(point.x - center.x) / max(1.0, Double(bounds.width) / 2.0)
        let dy = Double(point.y - center.y) / max(1.0, Double(bounds.height) / 2.0)
        return sqrt(dx * dx + dy * dy)
    }

    private static func semicircleScore(_ point: Point, bounds: RectValue) -> Double {
        let penalty = point.y > bounds.center.y ? 8.0 : 0.0
        return penalty + abs(ellipseRadius(point, bounds: bounds) - 1)
    }

    private static func heartEquation(_ point: Point, bounds: RectValue) -> Double {
        let center = bounds.center
        let x = Double(point.x - center.x) / max(1.0, Double(bounds.width) / 2.0) * 1.35
        let y = -Double(point.y - center.y) / max(1.0, Double(bounds.height) / 2.0) * 1.35
        return pow(x * x + y * y - 1, 3) - x * x * pow(y, 3)
    }

    private static func waveDistance(_ point: Point, bounds: RectValue) -> Double {
        let progress = Double(point.x - bounds.left) / Double(max(1, bounds.width))
        let expected = Double(bounds.top) + Double(bounds.height) / 2.0 + sin(progress * Double.pi * 4) * Double(bounds.height) * 0.34
        return abs(Double(point.y) - expected)
    }

    private static func angle(_ point: Point, bounds: RectValue) -> Double {
        let center = bounds.center
        return atan2(Double(point.y - center.y), Double(point.x - center.x))
    }

    private static func starVertices(bounds: RectValue) -> [CGPoint] {
        let center = bounds.center
        let outer = Double(min(bounds.width, bounds.height)) / 2.0
        let inner = outer * 0.45
        return (0..<10).map { index in
            let angle = -Double.pi / 2 + Double(index) * Double.pi / 5
            let radius = index.isMultiple(of: 2) ? outer : inner
            return CGPoint(x: Double(center.x) + cos(angle) * radius, y: Double(center.y) + sin(angle) * radius)
        }
    }

    private static func selectByDistanceToPolyline(grid: [Point], vertices: [CGPoint], closed: Bool, requiredCount: Int, breakAtMiddle: Bool = false) -> [Point] {
        let segments = buildSegments(vertices: vertices, closed: closed, breakAtMiddle: breakAtMiddle)
        return Array(grid.sorted {
            let left = minPolylineDistance($0, segments: segments)
            let right = minPolylineDistance($1, segments: segments)
            if left != right {
                return left < right
            }
            return minPolylineDistanceAlong($0, segments: segments) < minPolylineDistanceAlong($1, segments: segments)
        }.prefix(requiredCount))
    }

    private static func centeredGrid(bounds: RectValue, count: Int, spacing: SizeValue) -> [Point] {
        let columns = max(1, Int(ceil(sqrt(Double(count)))))
        let rows = Int(ceil(Double(count) / Double(columns)))
        let gridWidth = (columns - 1) * spacing.width
        let gridHeight = (rows - 1) * spacing.height
        let startX = bounds.left + max(0, (bounds.width - gridWidth) / 2)
        let startY = bounds.top + max(0, (bounds.height - gridHeight) / 2)
        return (0..<count).map {
            Point(x: startX + ($0 % columns) * spacing.width, y: startY + ($0 / columns) * spacing.height)
        }
    }

    private static func textPattern(_ text: String, bounds: RectValue, grid: [Point], requiredCount: Int) -> [Point] {
        let normalized = text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty ? "WORK" : text
        guard let bitmap = renderMask(bounds: bounds, draw: { canvas in
            var fontSize = max(24, canvas.height * 0.66)
            var rendered = NSAttributedString(string: normalized, attributes: [
                .font: NSFont.boldSystemFont(ofSize: fontSize),
                .foregroundColor: NSColor.black
            ])
            while rendered.size().width > canvas.width * 0.94 && fontSize > 12 {
                fontSize -= 2
                rendered = NSAttributedString(string: normalized, attributes: [
                    .font: NSFont.boldSystemFont(ofSize: fontSize),
                    .foregroundColor: NSColor.black
                ])
            }
            let textSize = rendered.size()
            rendered.draw(in: NSRect(
                x: (canvas.width - textSize.width) / 2,
                y: (canvas.height - textSize.height) / 2,
                width: textSize.width,
                height: textSize.height
            ))
        }) else { return [] }
        return sampleMask(bitmap, bounds: bounds, grid: grid, requiredCount: requiredCount)
    }

    private static func imageMask(_ path: String?, bounds: RectValue, grid: [Point], requiredCount: Int) -> [Point] {
        guard let path, let image = NSImage(contentsOfFile: path),
              let bitmap = renderMask(bounds: bounds, draw: { canvas in
                  image.draw(in: canvas, from: .zero, operation: .sourceOver, fraction: 1)
              }) else {
            return selectBest(grid: grid, count: requiredCount, score: { abs(ellipseRadius($0, bounds: bounds) - 1) }, secondary: { angle($0, bounds: bounds) })
        }
        return sampleMask(bitmap, bounds: bounds, grid: grid, requiredCount: requiredCount)
    }

    private struct MaskBitmap {
        let width: Int
        let height: Int
        let pixels: [UInt8]
    }

    private static func renderMask(bounds: RectValue, draw: (NSRect) -> Void) -> MaskBitmap? {
        let width = max(160, bounds.width)
        let height = max(90, bounds.height)
        let bytesPerRow = width * 4
        guard let context = CGContext(
            data: nil,
            width: width,
            height: height,
            bitsPerComponent: 8,
            bytesPerRow: bytesPerRow,
            space: CGColorSpaceCreateDeviceRGB(),
            bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue | CGBitmapInfo.byteOrder32Big.rawValue
        ) else {
            return nil
        }

        context.setFillColor(red: 1, green: 1, blue: 1, alpha: 1)
        context.fill(CGRect(x: 0, y: 0, width: width, height: height))
        NSGraphicsContext.saveGraphicsState()
        NSGraphicsContext.current = NSGraphicsContext(cgContext: context, flipped: false)
        let canvas = NSRect(x: 0, y: 0, width: CGFloat(width), height: CGFloat(height))
        draw(canvas)
        NSGraphicsContext.current?.flushGraphics()
        NSGraphicsContext.restoreGraphicsState()
        guard let data = context.data else { return nil }
        let pixels = Array(UnsafeBufferPointer(start: data.assumingMemoryBound(to: UInt8.self), count: bytesPerRow * height))
        return MaskBitmap(width: width, height: height, pixels: pixels)
    }

    private static func sampleMask(_ bitmap: MaskBitmap, bounds: RectValue, grid: [Point], requiredCount: Int) -> [Point] {
        let selected = grid.filter { point in
            let x = clamp((point.x - bounds.left) * bitmap.width / max(1, bounds.width), 0, bitmap.width - 1)
            let y = clamp(bitmap.height - 1 - (point.y - bounds.top) * bitmap.height / max(1, bounds.height), 0, bitmap.height - 1)
            var darkNeighbors = 0
            for dy in -1...1 {
                for dx in -1...1 {
                    let pixelX = clamp(x + dx, 0, bitmap.width - 1)
                    let pixelY = clamp(y + dy, 0, bitmap.height - 1)
                    let offset = (pixelY * bitmap.width + pixelX) * 4
                    let brightness = Int(bitmap.pixels[offset]) + Int(bitmap.pixels[offset + 1]) + Int(bitmap.pixels[offset + 2])
                    if bitmap.pixels[offset + 3] > 38 && brightness < 513 {
                        darkNeighbors += 1
                    }
                }
            }
            return darkNeighbors >= 2
        }
        return selected.count > requiredCount ? evenlySample(selected, count: requiredCount) : selected
    }

    private static func buildOverflowGrid(existing: [Point], count: Int, workArea: RectValue, spacing: SizeValue) -> [Point] {
        var occupied = Set(existing)
        let center = existing.isEmpty
            ? workArea.center
            : Point(x: Int((Double(existing.map(\.x).reduce(0, +)) / Double(existing.count)).rounded()), y: Int((Double(existing.map(\.y).reduce(0, +)) / Double(existing.count)).rounded()))
        let columns = max(1, workArea.width / max(48, spacing.width))
        let rows = max(1, workArea.height / max(48, spacing.height))
        var candidates: [Point] = []
        for row in 0..<rows {
            for column in 0..<columns {
                candidates.append(clampToWorkArea(Point(x: workArea.left + column * spacing.width, y: workArea.top + row * spacing.height), workArea: workArea, spacing: spacing))
            }
        }

        var points: [Point] = []
        for point in unique(candidates).sorted(by: { distance($0, center) < distance($1, center) }) {
            if occupied.insert(point).inserted {
                points.append(point)
                if points.count >= count {
                    break
                }
            }
        }
        return points
    }

    private static func order(points: [Point], fillMode: PatternFillMode, bounds: RectValue) -> [Point] {
        let center = bounds.center
        switch fillMode {
        case .topToBottom:
            return points.sorted { $0.y == $1.y ? $0.x < $1.x : $0.y < $1.y }
        case .centerOut:
            return points.sorted { distance($0, center) < distance($1, center) }
        case .clockwise:
            return points.sorted { atan2(Double($0.y - center.y), Double($0.x - center.x)) < atan2(Double($1.y - center.y), Double($1.x - center.x)) }
        case .leftToRight:
            return points.sorted { $0.x == $1.x ? $0.y < $1.y : $0.x < $1.x }
        }
    }

    private static func evenlySample(_ points: [Point], count: Int) -> [Point] {
        guard points.count > count else {
            return points
        }

        let step = Double(points.count) / Double(count)
        return unique((0..<count).map { points[min(points.count - 1, Int(floor(Double($0) * step)))] })
    }

    private static func rotate(points: [Point], center: Point, degrees: Int) -> [Point] {
        let radians = Double(degrees) * Double.pi / 180.0
        let cosValue = cos(radians)
        let sinValue = sin(radians)
        return points.map {
            let dx = Double($0.x - center.x)
            let dy = Double($0.y - center.y)
            return Point(
                x: Int((Double(center.x) + dx * cosValue - dy * sinValue).rounded()),
                y: Int((Double(center.y) + dx * sinValue + dy * cosValue).rounded())
            )
        }
    }

    private static func snap(_ point: Point, to grid: [Point]) -> Point {
        grid.min { distance($0, point) < distance($1, point) } ?? point
    }

    private static func clampToWorkArea(_ point: Point, workArea: RectValue, spacing: SizeValue) -> Point {
        Point(
            x: clamp(point.x, workArea.left, max(workArea.left, workArea.right - spacing.width)),
            y: clamp(point.y, workArea.top, max(workArea.top, workArea.bottom - spacing.height))
        )
    }

    private static func distance(_ a: Point, _ b: Point) -> Double {
        sqrt(pow(Double(a.x - b.x), 2) + pow(Double(a.y - b.y), 2))
    }

    private static func distanceToSegment(_ point: Point, _ a: CGPoint, _ b: CGPoint) -> Double {
        let dx = b.x - a.x
        let dy = b.y - a.y
        guard abs(dx) >= 0.001 || abs(dy) >= 0.001 else {
            return sqrt(pow(Double(point.x) - a.x, 2) + pow(Double(point.y) - a.y, 2))
        }
        var t = ((Double(point.x) - a.x) * dx + (Double(point.y) - a.y) * dy) / (dx * dx + dy * dy)
        t = min(max(t, 0), 1)
        let closestX = a.x + t * dx
        let closestY = a.y + t * dy
        return sqrt(pow(Double(point.x) - closestX, 2) + pow(Double(point.y) - closestY, 2))
    }

    private static func buildSegments(vertices: [CGPoint], closed: Bool, breakAtMiddle: Bool) -> [(CGPoint, CGPoint)] {
        var segments: [(CGPoint, CGPoint)] = []
        guard vertices.count > 1 else {
            return segments
        }
        for index in 0..<(vertices.count - 1) where !(breakAtMiddle && index == 1) {
            segments.append((vertices[index], vertices[index + 1]))
        }
        if closed {
            segments.append((vertices[vertices.count - 1], vertices[0]))
        }
        return segments
    }

    private static func minPolylineDistance(_ point: Point, segments: [(CGPoint, CGPoint)]) -> Double {
        segments.map { distanceToSegment(point, $0.0, $0.1) }.min() ?? 0
    }

    private static func minPolylineDistanceAlong(_ point: Point, segments: [(CGPoint, CGPoint)]) -> Double {
        var best = Double.greatestFiniteMagnitude
        var offset = 0.0
        var bestAlong = 0.0
        for segment in segments {
            let length = sqrt(pow(segment.1.x - segment.0.x, 2) + pow(segment.1.y - segment.0.y, 2))
            let distance = distanceToSegment(point, segment.0, segment.1)
            if distance < best {
                best = distance
                bestAlong = offset + length / 2
            }
            offset += length
        }
        return bestAlong
    }

    private static func unique(_ points: [Point]) -> [Point] {
        var seen = Set<Point>()
        return points.filter { seen.insert($0).inserted }
    }
}
