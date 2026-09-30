import Foundation

enum SnapshotStore {
    static func load() -> [LayoutSnapshot] {
        JsonStore.load([LayoutSnapshot].self, from: AppPaths.snapshotsURL, default: [])
    }

    static func add(_ snapshot: LayoutSnapshot, retentionCount: Int) {
        var snapshots = load()
        snapshots.insert(snapshot, at: 0)
        snapshots = Array(snapshots.prefix(max(1, retentionCount)))
        do {
            try JsonStore.save(snapshots, to: AppPaths.snapshotsURL)
        } catch {
            AppLogger.log("保存快照失败。", error: error)
        }
    }
}
