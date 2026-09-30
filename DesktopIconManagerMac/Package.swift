// swift-tools-version: 5.9

import PackageDescription

let package = Package(
    name: "DesktopIconManagerMac",
    defaultLocalization: "zh-Hans",
    platforms: [
        .macOS(.v13)
    ],
    products: [
        .executable(name: "DesktopIconManagerMac", targets: ["DesktopIconManagerMac"])
    ],
    targets: [
        .executableTarget(
            name: "DesktopIconManagerMac",
            path: "Sources/DesktopIconManagerMac",
            exclude: ["Resources"]
        ),
        .testTarget(
            name: "DesktopIconManagerMacTests",
            dependencies: ["DesktopIconManagerMac"],
            path: "Tests/DesktopIconManagerMacTests"
        )
    ]
)
