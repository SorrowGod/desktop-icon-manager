import Foundation

extension LayoutMode {
    var displayText: String {
        switch self {
        case .left: "左边"
        case .right: "右边"
        case .top: "上面"
        case .bottom: "下面"
        case .centerCompact: "中间一团"
        case .customPattern: "自定义图案"
        case .desktopZones: "桌面分区"
        }
    }
}

extension SortMode {
    var displayText: String {
        switch self {
        case .typeThenName: "类型分组"
        case .nameAscending: "名称 A-Z"
        case .currentOrder: "保持当前顺序"
        case .lastModifiedTime: "最近修改时间"
        case .fileSize: "文件大小"
        case .extension: "扩展名"
        case .usageFrequency: "使用频率"
        case .manualPriority: "手动优先级"
        }
    }
}

extension IconCategory {
    var displayText: String {
        switch self {
        case .folder: "文件夹"
        case .shortcutOrApp: "快捷方式/应用"
        case .document: "文档"
        case .image: "图片"
        case .media: "音视频"
        case .archive: "压缩包"
        case .system: "系统图标"
        case .other: "其他"
        }
    }
}

extension PatternKind {
    var displayText: String {
        switch self {
        case .circle: "圆形"
        case .semicircle: "半圆"
        case .heart: "心形"
        case .star: "星形"
        case .wave: "波浪线"
        case .diagonal: "斜线"
        case .squareGrid: "方阵"
        case .ring: "环形"
        case .vShape: "V 字形"
        case .xShape: "X 字形"
        case .text: "文字轮廓"
        case .imageMask: "图片蒙版"
        case .manualPoints: "手动点位"
        }
    }
}
