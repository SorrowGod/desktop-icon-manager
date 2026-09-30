import SwiftUI

struct PreviewCanvas: View {
    let layout: ArrangeLayout?
    let icons: [DesktopIconInfo]

    var body: some View {
        GeometryReader { proxy in
            let bounds = proxy.size
            ZStack(alignment: .topLeading) {
                RoundedRectangle(cornerRadius: 8)
                    .fill(Color(nsColor: .controlBackgroundColor))
                RoundedRectangle(cornerRadius: 8)
                    .stroke(Color.secondary.opacity(0.25))

                if let layout {
                    ForEach(layout.positions) { position in
                        let point = map(position.targetPosition, from: layout.workArea, to: bounds)
                        VStack(spacing: 3) {
                            Circle()
                                .fill(color(for: position.icon.category))
                                .frame(width: 16, height: 16)
                            Text(position.icon.displayName)
                                .font(.caption2)
                                .lineLimit(1)
                                .frame(width: 76)
                        }
                        .position(x: point.x, y: point.y)
                    }
                } else {
                    Text("暂无预览")
                        .foregroundStyle(.secondary)
                        .frame(maxWidth: .infinity, maxHeight: .infinity)
                }
            }
        }
        .aspectRatio(16 / 9, contentMode: .fit)
    }

    private func map(_ point: Point, from workArea: RectValue, to size: CGSize) -> CGPoint {
        let x = CGFloat(point.x - workArea.left) / CGFloat(max(1, workArea.width)) * size.width
        let y = CGFloat(point.y - workArea.top) / CGFloat(max(1, workArea.height)) * size.height
        return CGPoint(x: x, y: y)
    }

    private func color(for category: IconCategory) -> Color {
        switch category {
        case .folder: .blue
        case .shortcutOrApp: .purple
        case .document: .teal
        case .image: .pink
        case .media: .orange
        case .archive: .brown
        case .system: .gray
        case .other: .secondary
        }
    }
}
