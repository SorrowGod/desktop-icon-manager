import Foundation

enum DesktopZoneEngine {
    static func calculate(
        profile: ArrangeProfile,
        icons: [DesktopIconInfo],
        workArea: RectValue,
        spacing: SizeValue
    ) -> [ArrangedIconPosition] {
        guard !icons.isEmpty else {
            return []
        }

        let zones = profile.desktopZones.isEmpty ? createDefaultZones() : profile.desktopZones
        var assignments = Array(repeating: [DesktopIconInfo](), count: zones.count)
        var unassigned: [DesktopIconInfo] = []

        for icon in icons {
            if let zone = findZone(profile: profile, zones: zones, icon: icon),
               let index = zones.firstIndex(of: zone) {
                assignments[index].append(icon)
            } else {
                unassigned.append(icon)
            }
        }

        if !unassigned.isEmpty {
            let fallbackIndex = zones.firstIndex { $0.name.caseInsensitiveCompare("待处理") == .orderedSame } ?? zones.count - 1
            assignments[fallbackIndex].append(contentsOf: unassigned)
        }

        var result: [ArrangedIconPosition] = []
        for (index, zone) in zones.enumerated() {
            let zoneIcons = assignments[index]
            guard !zoneIcons.isEmpty else {
                continue
            }

            var zoneProfile = profile
            zoneProfile.layoutMode = [.customPattern, .desktopZones].contains(zone.layoutMode) ? .centerCompact : zone.layoutMode
            zoneProfile.sortMode = zone.sortMode
            zoneProfile.useCurrentDesktopSpacing = false
            zoneProfile.leftMargin = 8
            zoneProfile.topMargin = 8
            zoneProfile.rightMargin = 8
            zoneProfile.bottomMargin = 8

            result.append(contentsOf: DesktopIconArranger.calculateBasicPositions(
                profile: zoneProfile,
                orderedIcons: DesktopIconArranger.sortIcons(zoneIcons, by: zone.sortMode),
                workArea: zone.rectangle(in: workArea),
                spacing: spacing,
                zoneName: zone.name
            ))
        }

        return result
    }

    static func createDefaultZones() -> [DesktopZone] {
        [
            DesktopZone(
                name: "常用软件",
                xPercent: 0,
                yPercent: 0,
                widthPercent: 22,
                heightPercent: 100,
                colorArgb: 0x3C63E6,
                layoutMode: .left,
                sortMode: .typeThenName,
                categories: [.shortcutOrApp, .system]
            ),
            DesktopZone(
                name: "工作文件",
                xPercent: 24,
                yPercent: 0,
                widthPercent: 38,
                heightPercent: 60,
                colorArgb: 0x00897B,
                layoutMode: .top,
                sortMode: .typeThenName,
                categories: [.document, .folder]
            ),
            DesktopZone(
                name: "媒体与压缩包",
                xPercent: 64,
                yPercent: 0,
                widthPercent: 36,
                heightPercent: 45,
                colorArgb: 0xC26414,
                layoutMode: .top,
                sortMode: .typeThenName,
                categories: [.image, .media, .archive]
            ),
            DesktopZone(
                name: "待处理",
                xPercent: 24,
                yPercent: 64,
                widthPercent: 76,
                heightPercent: 36,
                colorArgb: 0x734CAA,
                layoutMode: .top,
                sortMode: .currentOrder,
                categories: [.other]
            )
        ]
    }

    private static func findZone(profile: ArrangeProfile, zones: [DesktopZone], icon: DesktopIconInfo) -> DesktopZone? {
        let sendRules = profile.rules
            .filter { $0.enabled && $0.actionKind == .sendToZone }
            .sorted { $0.priority < $1.priority }

        for rule in sendRules where DesktopIconArranger.matches(rule: rule, icon: icon) {
            if let match = zones.first(where: { $0.name.caseInsensitiveCompare(rule.actionValue) == .orderedSame }) {
                return match
            }
        }

        return zones.first { zone in
            zone.iconKeys.contains { $0.caseInsensitiveCompare(icon.stableKey) == .orderedSame } ||
                zone.categories.contains(icon.category) ||
                zone.nameContains.contains { keyword in
                    let trimmed = keyword.trimmingCharacters(in: .whitespacesAndNewlines)
                    return !trimmed.isEmpty && icon.displayName.localizedCaseInsensitiveContains(trimmed)
                }
        }
    }
}
