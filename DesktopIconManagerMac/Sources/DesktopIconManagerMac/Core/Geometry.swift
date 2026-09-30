import CoreGraphics
import Foundation

struct Point: Codable, Hashable {
    var x: Int
    var y: Int

    var cgPoint: CGPoint {
        CGPoint(x: x, y: y)
    }
}

struct SizeValue: Codable, Hashable {
    var width: Int
    var height: Int
}

struct RectValue: Codable, Hashable {
    var x: Int
    var y: Int
    var width: Int
    var height: Int

    var left: Int { x }
    var top: Int { y }
    var right: Int { x + width }
    var bottom: Int { y + height }
    var center: Point { Point(x: x + width / 2, y: y + height / 2) }

    init(x: Int, y: Int, width: Int, height: Int) {
        self.x = x
        self.y = y
        self.width = width
        self.height = height
    }

    init(_ rect: CGRect) {
        self.init(
            x: Int(rect.origin.x.rounded()),
            y: Int(rect.origin.y.rounded()),
            width: Int(rect.width.rounded()),
            height: Int(rect.height.rounded())
        )
    }

    var cgRect: CGRect {
        CGRect(x: x, y: y, width: width, height: height)
    }
}

func clamp<T: Comparable>(_ value: T, _ lower: T, _ upper: T) -> T {
    min(max(value, lower), upper)
}
