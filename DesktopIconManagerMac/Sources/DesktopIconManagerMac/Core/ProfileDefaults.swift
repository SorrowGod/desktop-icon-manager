import Foundation

enum ProfileDefaults {
    static func defaultProfile() -> ArrangeProfile {
        ArrangeProfile(
            desktopZones: DesktopZoneEngine.createDefaultZones()
        )
    }

    static func defaultScenes() -> [DesktopScene] {
        [
            DesktopScene(
                name: "默认场景",
                isBuiltIn: true,
                profileName: "默认方案",
                layoutMode: .centerCompact,
                sortMode: .typeThenName
            ),
            DesktopScene(
                name: "工作区",
                isBuiltIn: true,
                profileName: "默认方案",
                layoutMode: .desktopZones,
                sortMode: .typeThenName,
                useDesktopZones: true,
                desktopZones: DesktopZoneEngine.createDefaultZones()
            ),
            DesktopScene(
                name: "演示模式",
                isBuiltIn: true,
                profileName: "默认方案",
                layoutMode: .right,
                sortMode: .typeThenName,
                excludeSystemIcons: true
            ),
            DesktopScene(
                name: "创意图案",
                isBuiltIn: true,
                profileName: "默认方案",
                layoutMode: .customPattern,
                sortMode: .typeThenName,
                customPattern: CustomPatternDefinition(patternKind: .circle)
            )
        ]
    }

    static func defaultStoreData() -> ProfileStoreData {
        ProfileStoreData(
            profiles: [defaultProfile()],
            scenes: defaultScenes()
        )
    }
}
