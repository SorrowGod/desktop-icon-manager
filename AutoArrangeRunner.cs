namespace DesktopIconManager;

public static class AutoArrangeRunner
{
    public static int Run(string? profileName)
    {
        try
        {
            var store = ProfileStore.Load();
            var profile = store.Profiles.FirstOrDefault(item =>
                    string.Equals(item.Name, profileName, StringComparison.OrdinalIgnoreCase))
                ?? store.Profiles.FirstOrDefault(item =>
                    string.Equals(item.Name, store.DefaultProfileName, StringComparison.OrdinalIgnoreCase))
                ?? store.Profiles[0];

            var icons = DesktopIconArranger.GetDesktopIcons();
            var layout = DesktopIconArranger.CalculateLayout(profile, icons);
            SnapshotStore.Add(
                DesktopIconArranger.CreateSnapshot(profile.Name, icons, profile, layout.Positions.Count, layout.ExcludedCount, "自动整理前保存"),
                profile.SnapshotRetentionCount);
            DesktopIconArranger.ApplyLayout(layout);
            AppLogger.Log($"自动整理完成：方案={profile.Name}，图标={layout.Positions.Count}。");
            return 0;
        }
        catch (Exception ex)
        {
            AppLogger.Log("自动整理失败。", ex);
            return 1;
        }
    }
}
