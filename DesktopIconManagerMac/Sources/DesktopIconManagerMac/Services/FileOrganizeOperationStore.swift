import Foundation

enum FileOrganizeOperationStore {
    static func load() -> [FileOrganizeOperation] {
        JsonStore.load([FileOrganizeOperation].self, from: AppPaths.fileOrganizeOperationsURL, default: [])
    }

    static func add(_ operation: FileOrganizeOperation, retentionCount: Int) {
        var operations = load()
        operations.insert(operation, at: 0)
        operations = Array(operations.prefix(max(1, retentionCount)))
        save(operations)
    }

    static func latestUndoable() -> FileOrganizeOperation? {
        load().first { !$0.isUndoOperation && !$0.isUndone && $0.results.contains(where: \.success) }
    }

    static func markUndone(_ id: String) {
        var operations = load()
        guard let index = operations.firstIndex(where: { $0.id == id }) else {
            return
        }
        operations[index].isUndone = true
        save(operations)
    }

    private static func save(_ operations: [FileOrganizeOperation]) {
        do {
            try JsonStore.save(operations, to: AppPaths.fileOrganizeOperationsURL)
        } catch {
            AppLogger.log("保存文件收纳记录失败。", error: error)
        }
    }
}
