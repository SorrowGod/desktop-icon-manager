import Foundation
import XCTest
@testable import DesktopIconManagerMac

final class FileOrganizerTests: XCTestCase {
    func testMoveDoesNotOverwriteExistingFile() throws {
        let root = try makeTemporaryDirectory()
        defer { try? FileManager.default.removeItem(at: root) }
        let source = root.appendingPathComponent("report.pdf")
        let target = root.appendingPathComponent("文档/report.pdf")
        try FileManager.default.createDirectory(at: target.deletingLastPathComponent(), withIntermediateDirectories: true)
        try Data("original".utf8).write(to: source)
        try Data("existing".utf8).write(to: target)

        let result = FileOrganizer.move(sourcePath: source.path, targetPath: target.path, undo: false)

        XCTAssertFalse(result.success)
        XCTAssertEqual(result.status, "冲突")
        XCTAssertEqual(try String(contentsOf: source), "original")
        XCTAssertEqual(try String(contentsOf: target), "existing")
    }

    func testMoveCanBeReversedWithoutLosingContent() throws {
        let root = try makeTemporaryDirectory()
        defer { try? FileManager.default.removeItem(at: root) }
        let source = root.appendingPathComponent("report.pdf")
        let target = root.appendingPathComponent("文档/report.pdf")
        try Data("report data".utf8).write(to: source)

        XCTAssertTrue(FileOrganizer.move(sourcePath: source.path, targetPath: target.path, undo: false).success)
        XCTAssertFalse(FileManager.default.fileExists(atPath: source.path))
        XCTAssertTrue(FileOrganizer.move(sourcePath: target.path, targetPath: source.path, undo: true).success)
        XCTAssertEqual(try String(contentsOf: source), "report data")
    }

    func testRejectsSourceOutsideDesktopAndEscapingDestination() throws {
        let root = try makeTemporaryDirectory()
        defer { try? FileManager.default.removeItem(at: root) }
        let desktop = root.appendingPathComponent("Desktop", isDirectory: true)
        try FileManager.default.createDirectory(at: desktop, withIntermediateDirectories: true)
        let settings = AppSettings()
        var suggestion = FileOrganizeSuggestion(
            fileName: "report.pdf",
            originalPath: desktop.appendingPathComponent("report.pdf").path,
            category: .document,
            targetKind: .document,
            suggestedTargetPath: desktop.appendingPathComponent("文档/report.pdf").path
        )

        XCTAssertTrue(FileOrganizer.validateMove(suggestion, settings: settings, desktopURL: desktop))
        suggestion.originalPath = root.appendingPathComponent("report.pdf").path
        XCTAssertFalse(FileOrganizer.validateMove(suggestion, settings: settings, desktopURL: desktop))
        suggestion.originalPath = desktop.appendingPathComponent("report.pdf").path
        suggestion.suggestedTargetPath = root.appendingPathComponent("report.pdf").path
        XCTAssertFalse(FileOrganizer.validateMove(suggestion, settings: settings, desktopURL: desktop))
    }

    func testRejectsSymbolicLinkDestinationDirectory() throws {
        let root = try makeTemporaryDirectory()
        defer { try? FileManager.default.removeItem(at: root) }
        let desktop = root.appendingPathComponent("Desktop", isDirectory: true)
        let external = root.appendingPathComponent("External", isDirectory: true)
        try FileManager.default.createDirectory(at: desktop, withIntermediateDirectories: true)
        try FileManager.default.createDirectory(at: external, withIntermediateDirectories: true)
        try FileManager.default.createSymbolicLink(at: desktop.appendingPathComponent("文档"), withDestinationURL: external)
        let suggestion = FileOrganizeSuggestion(
            fileName: "report.pdf",
            originalPath: desktop.appendingPathComponent("report.pdf").path,
            category: .document,
            targetKind: .document,
            suggestedTargetPath: desktop.appendingPathComponent("文档/report.pdf").path
        )

        XCTAssertFalse(FileOrganizer.validateMove(suggestion, settings: AppSettings(), desktopURL: desktop))
    }

    private func makeTemporaryDirectory() throws -> URL {
        let url = FileManager.default.temporaryDirectory.appendingPathComponent(UUID().uuidString, isDirectory: true)
        try FileManager.default.createDirectory(at: url, withIntermediateDirectories: true)
        return url
    }
}
