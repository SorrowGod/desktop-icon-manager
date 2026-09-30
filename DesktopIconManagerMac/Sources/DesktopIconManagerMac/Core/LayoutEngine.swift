import Foundation

enum DesktopIconArranger {
    static func calculateLayout(
        profile: ArrangeProfile,
        icons: [DesktopIconInfo],
        workArea: RectValue,
        spacing: SizeValue
    ) -> ArrangeLayout {
        let normalizedSpacing = SizeValue(
            width: max(48, profile.useCurrentDesktopSpacing ? spacing.width : profile.columnSpacing),
            height: max(48, profile.useCurrentDesktopSpacing ? spacing.height : profile.rowSpacing)
        )
        let excludedKeys = buildExcludedKeys(profile: profile, icons: icons)
        let movable = icons.filter { !excludedKeys.contains($0.stableKey.lowercased()) }
        let positions = calculatePositions(
            profile: profile,
            orderedIcons: movable,
            workArea: workArea,
            spacing: normalizedSpacing
        )

        return ArrangeLayout(
            profile: profile,
            workArea: workArea,
            spacing: normalizedSpacing,
            positions: positions,
            excludedCount: icons.count - movable.count,
            layoutSummary: profile.layoutMode.displayText
        )
    }

    static func createSnapshot(
        profileName: String,
        icons: [DesktopIconInfo],
        profile: ArrangeProfile?,
        movedCount: Int = 0,
        excludedCount: Int = 0,
        note: String = ""
    ) -> LayoutSnapshot {
        LayoutSnapshot(
            profileName: profileName,
            layoutMode: profile?.layoutMode ?? .centerCompact,
            sortMode: profile?.sortMode ?? .typeThenName,
            targetScreenName: profile?.targetScreenDeviceName ?? "",
            movedCount: movedCount,
            excludedCount: excludedCount,
            note: note,
            icons: icons.map {
                IconPositionSnapshot(
                    stableKey: $0.stableKey,
                    displayName: $0.displayName,
                    index: $0.index,
                    x: $0.position.x,
                    y: $0.position.y
                )
            }
        )
    }

    static func sortIcons(_ icons: [DesktopIconInfo], by sortMode: SortMode) -> [DesktopIconInfo] {
        switch sortMode {
        case .typeThenName:
            icons.sorted {
                if categoryOrder($0.category) != categoryOrder($1.category) {
                    return categoryOrder($0.category) < categoryOrder($1.category)
                }
                if $0.displayName.localizedCaseInsensitiveCompare($1.displayName) != .orderedSame {
                    return $0.displayName.localizedCaseInsensitiveCompare($1.displayName) == .orderedAscending
                }
                return $0.index < $1.index
            }
        case .nameAscending:
            icons.sorted {
                if $0.displayName.localizedCaseInsensitiveCompare($1.displayName) != .orderedSame {
                    return $0.displayName.localizedCaseInsensitiveCompare($1.displayName) == .orderedAscending
                }
                return $0.index < $1.index
            }
        case .lastModifiedTime:
            icons.sorted {
                ($0.lastModifiedAt ?? .distantPast) > ($1.lastModifiedAt ?? .distantPast)
            }
        case .fileSize:
            icons.sorted { ($0.fileSizeBytes ?? -1) > ($1.fileSizeBytes ?? -1) }
        case .extension:
            icons.sorted {
                let left = $0.fileExtension ?? ""
                let right = $1.fileExtension ?? ""
                if left.caseInsensitiveCompare(right) != .orderedSame {
                    return left.caseInsensitiveCompare(right) == .orderedAscending
                }
                return $0.displayName.localizedCaseInsensitiveCompare($1.displayName) == .orderedAscending
            }
        case .usageFrequency:
            icons.sorted {
                if $0.category != $1.category {
                    return $0.category == .shortcutOrApp
                }
                return $0.displayName.localizedCaseInsensitiveCompare($1.displayName) == .orderedAscending
            }
        case .manualPriority, .currentOrder:
            icons.sorted { $0.index < $1.index }
        }
    }

    static func calculateBasicPositions(
        profile: ArrangeProfile,
        orderedIcons: [DesktopIconInfo],
        workArea: RectValue,
        spacing: SizeValue,
        zoneName: String? = nil
    ) -> [ArrangedIconPosition] {
        let positions: [ArrangedIconPosition]
        switch profile.layoutMode {
        case .left:
            positions = calculateVerticalEdgePositions(profile: profile, icons: orderedIcons, workArea: workArea, spacing: spacing, fromRight: false)
        case .right:
            positions = calculateVerticalEdgePositions(profile: profile, icons: orderedIcons, workArea: workArea, spacing: spacing, fromRight: true)
        case .top:
            positions = calculateHorizontalEdgePositions(profile: profile, icons: orderedIcons, workArea: workArea, spacing: spacing, fromBottom: false)
        case .bottom:
            positions = calculateHorizontalEdgePositions(profile: profile, icons: orderedIcons, workArea: workArea, spacing: spacing, fromBottom: true)
        default:
            positions = calculateCenterPositions(profile: profile, icons: orderedIcons, workArea: workArea, spacing: spacing)
        }

        guard let zoneName else {
            return positions
        }

        return positions.map {
            ArrangedIconPosition(icon: $0.icon, targetPosition: $0.targetPosition, zoneName: zoneName)
        }
    }

    static func matches(rule: IconRule, icon: DesktopIconInfo) -> Bool {
        switch rule.matchKind {
        case .nameContains:
            return !rule.matchValue.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty &&
                icon.displayName.localizedCaseInsensitiveContains(rule.matchValue.trimmingCharacters(in: .whitespacesAndNewlines))
        case .category:
            return IconCategory(rawValue: rule.matchValue) == icon.category
        case .stableKey:
            return icon.stableKey.caseInsensitiveCompare(rule.matchValue) == .orderedSame
        }
    }

    private static func calculatePositions(
        profile: ArrangeProfile,
        orderedIcons: [DesktopIconInfo],
        workArea: RectValue,
        spacing: SizeValue
    ) -> [ArrangedIconPosition] {
        guard !orderedIcons.isEmpty else {
            return []
        }

        switch profile.layoutMode {
        case .left:
            return calculateVerticalEdgePositions(profile: profile, icons: sortIcons(orderedIcons, by: profile.sortMode), workArea: workArea, spacing: spacing, fromRight: false)
        case .right:
            return calculateVerticalEdgePositions(profile: profile, icons: sortIcons(orderedIcons, by: profile.sortMode), workArea: workArea, spacing: spacing, fromRight: true)
        case .top:
            return calculateHorizontalEdgePositions(profile: profile, icons: sortIcons(orderedIcons, by: profile.sortMode), workArea: workArea, spacing: spacing, fromBottom: false)
        case .bottom:
            return calculateHorizontalEdgePositions(profile: profile, icons: sortIcons(orderedIcons, by: profile.sortMode), workArea: workArea, spacing: spacing, fromBottom: true)
        case .centerCompact:
            return calculateCenterPositions(profile: profile, icons: sortIcons(orderedIcons, by: profile.sortMode), workArea: workArea, spacing: spacing)
        case .customPattern:
            return PatternLayoutEngine.calculate(profile: profile, icons: sortIcons(orderedIcons, by: profile.sortMode), workArea: workArea, spacing: spacing)
        case .desktopZones:
            return DesktopZoneEngine.calculate(profile: profile, icons: orderedIcons, workArea: workArea, spacing: spacing)
        }
    }

    private static func calculateVerticalEdgePositions(
        profile: ArrangeProfile,
        icons: [DesktopIconInfo],
        workArea: RectValue,
        spacing: SizeValue,
        fromRight: Bool
    ) -> [ArrangedIconPosition] {
        let rowsPerColumn = max(1, (workArea.height - profile.topMargin - profile.bottomMargin) / spacing.height)
        return icons.enumerated().map { index, icon in
            let column = index / rowsPerColumn
            let row = index % rowsPerColumn
            let x = fromRight
                ? workArea.right - profile.rightMargin - spacing.width - column * spacing.width
                : workArea.left + profile.leftMargin + column * spacing.width
            let y = workArea.top + profile.topMargin + row * spacing.height
            return ArrangedIconPosition(icon: icon, targetPosition: Point(x: x, y: y), zoneName: nil)
        }
    }

    private static func calculateHorizontalEdgePositions(
        profile: ArrangeProfile,
        icons: [DesktopIconInfo],
        workArea: RectValue,
        spacing: SizeValue,
        fromBottom: Bool
    ) -> [ArrangedIconPosition] {
        let columnsPerRow = max(1, (workArea.width - profile.leftMargin - profile.rightMargin) / spacing.width)
        return icons.enumerated().map { index, icon in
            let row = index / columnsPerRow
            let column = index % columnsPerRow
            let x = workArea.left + profile.leftMargin + column * spacing.width
            let y = fromBottom
                ? workArea.bottom - profile.bottomMargin - spacing.height - row * spacing.height
                : workArea.top + profile.topMargin + row * spacing.height
            return ArrangedIconPosition(icon: icon, targetPosition: Point(x: x, y: y), zoneName: nil)
        }
    }

    private static func calculateCenterPositions(
        profile: ArrangeProfile,
        icons: [DesktopIconInfo],
        workArea: RectValue,
        spacing: SizeValue
    ) -> [ArrangedIconPosition] {
        let maxColumns = max(1, (workArea.width - profile.leftMargin - profile.rightMargin) / spacing.width)
        let maxRows = max(1, (workArea.height - profile.topMargin - profile.bottomMargin) / spacing.height)
        let count = icons.count
        let aspect = max(0.25, (Double(workArea.width) / Double(max(1, workArea.height))) * (Double(spacing.height) / Double(max(1, spacing.width))))
        var columns = clamp(Int(ceil(sqrt(Double(count) * aspect))), 1, maxColumns)
        var rows = Int(ceil(Double(count) / Double(columns)))

        while rows > maxRows && columns < maxColumns {
            columns += 1
            rows = Int(ceil(Double(count) / Double(columns)))
        }

        rows = min(rows, maxRows)
        let gridWidth = max(0, (columns - 1) * spacing.width)
        let gridHeight = max(0, (rows - 1) * spacing.height)
        let minX = workArea.left + profile.leftMargin
        let maxX = workArea.right - profile.rightMargin - gridWidth
        let minY = workArea.top + profile.topMargin
        let maxY = workArea.bottom - profile.bottomMargin - gridHeight
        let startX = clamp(workArea.left + (workArea.width - gridWidth) / 2, minX, max(minX, maxX))
        let startY = clamp(workArea.top + (workArea.height - gridHeight) / 2, minY, max(minY, maxY))

        return icons.enumerated().map { index, icon in
            let row = index / columns
            let column = index % columns
            return ArrangedIconPosition(
                icon: icon,
                targetPosition: Point(x: startX + column * spacing.width, y: startY + row * spacing.height),
                zoneName: nil
            )
        }
    }

    private static func buildExcludedKeys(profile: ArrangeProfile, icons: [DesktopIconInfo]) -> Set<String> {
        var keys = Set(profile.excludedIconKeys.map { $0.lowercased() })
        for icon in icons {
            if profile.excludeSystemIcons && icon.category == .system {
                keys.insert(icon.stableKey.lowercased())
            }
            if profile.excludedCategories.contains(icon.category) {
                keys.insert(icon.stableKey.lowercased())
            }
            if profile.excludedNamePatterns.contains(where: { pattern in
                let trimmed = pattern.trimmingCharacters(in: .whitespacesAndNewlines)
                return !trimmed.isEmpty && icon.displayName.localizedCaseInsensitiveContains(trimmed)
            }) {
                keys.insert(icon.stableKey.lowercased())
            }
            for rule in profile.rules where rule.enabled && rule.actionKind == .exclude {
                if matches(rule: rule, icon: icon) {
                    keys.insert(icon.stableKey.lowercased())
                }
            }
        }
        return keys
    }

    private static func categoryOrder(_ category: IconCategory) -> Int {
        switch category {
        case .folder: 0
        case .shortcutOrApp: 1
        case .document: 2
        case .image: 3
        case .media: 4
        case .archive: 5
        case .system: 6
        case .other: 7
        }
    }
}
