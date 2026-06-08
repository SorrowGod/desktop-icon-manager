using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;

namespace DesktopIconManager;

public static class DesktopIconArranger
{
    private const uint LvmFirst = 0x1000;
    private const uint LvmGetItemCount = LvmFirst + 4;
    private const uint LvmSetItemPosition = LvmFirst + 15;
    private const uint LvmGetItemPosition = LvmFirst + 16;
    private const uint LvmGetItemSpacing = LvmFirst + 51;
    private const uint LvmGetItemTextW = LvmFirst + 115;
    private const uint SmtoAbortIfHung = 0x0002;
    private const int GwlStyle = -16;
    private const long LvsAutoArrange = 0x0100;
    private const uint SwpNoSize = 0x0001;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;
    private const int MaxTextLength = 512;

    private static readonly StringComparer KeyComparer = StringComparer.OrdinalIgnoreCase;

    public static int GetDesktopIconCount()
    {
        var listViewHandle = DesktopWindowFinder.FindDesktopListView();
        return SendDesktopMessage(listViewHandle, LvmGetItemCount, IntPtr.Zero, IntPtr.Zero).ToInt32();
    }

    public static IReadOnlyList<DesktopIconInfo> GetDesktopIcons()
    {
        var listViewHandle = DesktopWindowFinder.FindDesktopListView();
        var iconCount = SendDesktopMessage(listViewHandle, LvmGetItemCount, IntPtr.Zero, IntPtr.Zero).ToInt32();
        var desktopFiles = DesktopFileClassifier.BuildDesktopFileMap();
        using var reader = RemoteListViewReader.Create(listViewHandle);

        var icons = new List<DesktopIconInfo>(iconCount);
        for (var index = 0; index < iconCount; index++)
        {
            var displayName = reader.GetItemText(index);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                displayName = $"图标 {index + 1}";
            }

            var position = reader.GetItemPosition(index);
            var metadata = DesktopFileClassifier.Find(displayName, desktopFiles);
            var category = metadata?.Category ?? DesktopFileClassifier.Classify(displayName, desktopFiles);
            icons.Add(new DesktopIconInfo
            {
                Index = index,
                DisplayName = displayName,
                Category = category,
                Position = position,
                StableKey = MakeStableKey(displayName, category),
                FilePath = metadata?.Path,
                Extension = metadata?.Extension,
                LastModifiedAt = metadata?.LastModifiedAt,
                FileSizeBytes = metadata?.FileSizeBytes,
                IsShortcut = metadata?.IsShortcut ?? false,
                ShortcutTargetPath = metadata?.ShortcutTargetPath,
                ShortcutTargetExists = metadata?.ShortcutTargetExists,
            });
        }

        return icons;
    }

    public static ArrangeLayout CalculateLayout(ArrangeProfile profile, IReadOnlyList<DesktopIconInfo> icons)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(icons);

        var listViewHandle = DesktopWindowFinder.FindDesktopListView();
        var targetScreen = FindTargetScreen(profile.TargetScreenDeviceName);
        var spacing = profile.UseCurrentDesktopSpacing
            ? GetDesktopIconSpacing(listViewHandle)
            : new Size(profile.ColumnSpacing, profile.RowSpacing);

        spacing = new Size(Math.Max(48, spacing.Width), Math.Max(48, spacing.Height));

        var excludedKeys = BuildExcludedKeys(profile, icons);
        var movableIcons = icons
            .Where(icon => !excludedKeys.Contains(icon.StableKey))
            .ToList();
        var workArea = targetScreen.WorkingArea;
        var positions = CalculatePositions(profile, movableIcons, workArea, spacing);

        return new ArrangeLayout
        {
            Profile = profile,
            WorkArea = workArea,
            Spacing = spacing,
            Positions = positions,
            ExcludedCount = icons.Count - movableIcons.Count,
            LayoutSummary = profile.LayoutMode.ToDisplayText(),
        };
    }

    public static ArrangePreview CreatePreview(ArrangeProfile profile, IReadOnlyList<DesktopIconInfo> icons)
    {
        var screen = FindTargetScreen(profile.TargetScreenDeviceName);
        var excludedKeys = BuildExcludedKeys(profile, icons);
        var excludedCount = icons.Count(icon => excludedKeys.Contains(icon.StableKey));

        return new ArrangePreview
        {
            IconCount = icons.Count,
            MovableCount = icons.Count - excludedCount,
            ExcludedCount = excludedCount,
            LayoutMode = profile.LayoutMode,
            SortMode = profile.SortMode,
            TargetScreenName = DescribeScreen(screen),
            SafeArrangeEnabled = profile.SafeArrangeEnabled,
        };
    }

    public static ArrangeApplyResult ApplyLayout(ArrangeLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        if (layout.Profile.SafeArrangeEnabled)
        {
            PendingOperationStore.SaveIfMissing(new PendingArrangeOperation
            {
                CreatedAt = DateTime.Now,
                Reason = "整理图标时程序可能被中断",
                Snapshot = CreateSnapshot(layout.Profile.Name, GetDesktopIcons(), layout.Profile, layout.Positions.Count, layout.ExcludedCount),
            });
        }

        var result = ApplyMoveRequests(layout.Positions.Select(item => new MoveRequest(
            item.Icon.Index,
            item.Icon.DisplayName,
            item.Icon.StableKey,
            item.TargetPosition)));

        if (layout.Profile.SafeArrangeEnabled)
        {
            PendingOperationStore.Clear();
        }

        return result;
    }

    public static LayoutSnapshot CreateSnapshot(
        string profileName,
        IReadOnlyList<DesktopIconInfo> icons,
        ArrangeProfile? profile = null,
        int movedCount = 0,
        int excludedCount = 0,
        string note = "")
    {
        return new LayoutSnapshot
        {
            CreatedAt = DateTime.Now,
            ProfileName = profileName,
            LayoutMode = profile?.LayoutMode ?? LayoutMode.CenterCompact,
            SortMode = profile?.SortMode ?? SortMode.TypeThenName,
            TargetScreenName = profile is null ? string.Empty : DescribeScreen(FindTargetScreen(profile.TargetScreenDeviceName)),
            MovedCount = movedCount,
            ExcludedCount = excludedCount,
            AppVersion = typeof(DesktopIconArranger).Assembly.GetName().Version?.ToString() ?? string.Empty,
            Note = note,
            Icons = icons.Select(icon => new IconPositionSnapshot
            {
                StableKey = icon.StableKey,
                DisplayName = icon.DisplayName,
                Index = icon.Index,
                X = icon.Position.X,
                Y = icon.Position.Y,
            }).ToList(),
        };
    }

    public static int RestoreSnapshot(LayoutSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        var currentIcons = GetDesktopIcons();
        var byStableKey = currentIcons
            .GroupBy(icon => icon.StableKey, KeyComparer)
            .ToDictionary(group => group.Key, group => new Queue<DesktopIconInfo>(group), KeyComparer);
        var positions = new List<MoveRequest>();

        foreach (var savedIcon in snapshot.Icons)
        {
            DesktopIconInfo? match = null;
            if (byStableKey.TryGetValue(savedIcon.StableKey, out var queue) && queue.Count > 0)
            {
                match = queue.Dequeue();
            }
            else
            {
                match = currentIcons.FirstOrDefault(icon => icon.Index == savedIcon.Index);
            }

            if (match is not null)
            {
                positions.Add(new MoveRequest(match.Index, match.DisplayName, match.StableKey, savedIcon.ToPoint()));
            }
        }

        var result = ApplyMoveRequests(positions);
        return result.VerifiedAtTargetCount;
    }

    public static Screen FindTargetScreen(string? deviceName)
    {
        if (!string.IsNullOrWhiteSpace(deviceName))
        {
            var matchingScreen = Screen.AllScreens.FirstOrDefault(screen =>
                string.Equals(screen.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
            if (matchingScreen is not null)
            {
                return matchingScreen;
            }
        }

        return Screen.PrimaryScreen ?? Screen.AllScreens.First();
    }

    public static string DescribeScreen(Screen screen)
    {
        var primary = screen.Primary ? "主屏幕" : "扩展屏";
        return $"{primary} {screen.Bounds.Width}x{screen.Bounds.Height} {screen.DeviceName}";
    }

    public static IReadOnlyList<DesktopIconInfo> SortIcons(IReadOnlyList<DesktopIconInfo> icons, SortMode sortMode)
    {
        return sortMode switch
        {
            SortMode.TypeThenName => icons
                .OrderBy(icon => GetCategoryOrder(icon.Category))
                .ThenBy(icon => icon.DisplayName, StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true))
                .ThenBy(icon => icon.Index)
                .ToList(),
            SortMode.NameAscending => icons
                .OrderBy(icon => icon.DisplayName, StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true))
                .ThenBy(icon => icon.Index)
                .ToList(),
            SortMode.LastModifiedTime => icons
                .OrderByDescending(icon => icon.LastModifiedAt ?? DateTime.MinValue)
                .ThenBy(icon => icon.DisplayName, StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true))
                .ToList(),
            SortMode.FileSize => icons
                .OrderByDescending(icon => icon.FileSizeBytes ?? -1)
                .ThenBy(icon => icon.DisplayName, StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true))
                .ToList(),
            SortMode.Extension => icons
                .OrderBy(icon => icon.Extension ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(icon => icon.DisplayName, StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true))
                .ToList(),
            SortMode.UsageFrequency => icons
                .OrderBy(icon => icon.Category == IconCategory.ShortcutOrApp ? 0 : 1)
                .ThenBy(icon => icon.DisplayName, StringComparer.Create(CultureInfo.CurrentCulture, ignoreCase: true))
                .ToList(),
            SortMode.ManualPriority => icons
                .OrderBy(icon => icon.Index)
                .ToList(),
            SortMode.CurrentOrder => icons.OrderBy(icon => icon.Index).ToList(),
            _ => icons.OrderBy(icon => icon.Index).ToList(),
        };
    }

    private static int GetCategoryOrder(IconCategory category) => category switch
    {
        IconCategory.Folder => 0,
        IconCategory.ShortcutOrApp => 1,
        IconCategory.Document => 2,
        IconCategory.Image => 3,
        IconCategory.Media => 4,
        IconCategory.Archive => 5,
        IconCategory.System => 6,
        IconCategory.Other => 7,
        _ => 99,
    };

    private static IReadOnlyList<ArrangedIconPosition> CalculatePositions(
        ArrangeProfile profile,
        IReadOnlyList<DesktopIconInfo> orderedIcons,
        Rectangle workArea,
        Size spacing)
    {
        if (orderedIcons.Count == 0)
        {
            return [];
        }

        return profile.LayoutMode switch
        {
            LayoutMode.Left => CalculateVerticalEdgePositions(profile, SortIcons(orderedIcons, profile.SortMode), workArea, spacing, fromRight: false),
            LayoutMode.Right => CalculateVerticalEdgePositions(profile, SortIcons(orderedIcons, profile.SortMode), workArea, spacing, fromRight: true),
            LayoutMode.Top => CalculateHorizontalEdgePositions(profile, SortIcons(orderedIcons, profile.SortMode), workArea, spacing, fromBottom: false),
            LayoutMode.Bottom => CalculateHorizontalEdgePositions(profile, SortIcons(orderedIcons, profile.SortMode), workArea, spacing, fromBottom: true),
            LayoutMode.CenterCompact => CalculateCenterPositions(profile, SortIcons(orderedIcons, profile.SortMode), workArea, spacing),
            LayoutMode.CustomPattern => PatternLayoutEngine.Calculate(profile, SortIcons(orderedIcons, profile.SortMode), workArea, spacing),
            LayoutMode.DesktopZones => DesktopZoneEngine.Calculate(profile, orderedIcons, workArea, spacing),
            _ => CalculateCenterPositions(profile, SortIcons(orderedIcons, profile.SortMode), workArea, spacing),
        };
    }

    public static IReadOnlyList<ArrangedIconPosition> CalculateBasicPositions(
        ArrangeProfile profile,
        IReadOnlyList<DesktopIconInfo> orderedIcons,
        Rectangle workArea,
        Size spacing,
        string? zoneName = null)
    {
        var positions = profile.LayoutMode switch
        {
            LayoutMode.Left => CalculateVerticalEdgePositions(profile, orderedIcons, workArea, spacing, fromRight: false),
            LayoutMode.Right => CalculateVerticalEdgePositions(profile, orderedIcons, workArea, spacing, fromRight: true),
            LayoutMode.Top => CalculateHorizontalEdgePositions(profile, orderedIcons, workArea, spacing, fromBottom: false),
            LayoutMode.Bottom => CalculateHorizontalEdgePositions(profile, orderedIcons, workArea, spacing, fromBottom: true),
            _ => CalculateCenterPositions(profile, orderedIcons, workArea, spacing),
        };

        if (zoneName is null)
        {
            return positions;
        }

        return positions
            .Select(position => new ArrangedIconPosition
            {
                Icon = position.Icon,
                TargetPosition = position.TargetPosition,
                ZoneName = zoneName,
            })
            .ToList();
    }

    private static HashSet<string> BuildExcludedKeys(ArrangeProfile profile, IReadOnlyList<DesktopIconInfo> icons)
    {
        var excludedKeys = new HashSet<string>(profile.ExcludedIconKeys, KeyComparer);
        foreach (var icon in icons)
        {
            if (profile.ExcludeSystemIcons && icon.Category == IconCategory.System)
            {
                excludedKeys.Add(icon.StableKey);
            }

            if (profile.ExcludedCategories.Contains(icon.Category))
            {
                excludedKeys.Add(icon.StableKey);
            }

            if (profile.ExcludedNamePatterns.Any(pattern =>
                !string.IsNullOrWhiteSpace(pattern) &&
                icon.DisplayName.Contains(pattern.Trim(), StringComparison.CurrentCultureIgnoreCase)))
            {
                excludedKeys.Add(icon.StableKey);
            }

            foreach (var rule in profile.Rules.Where(rule => rule.Enabled && rule.ActionKind == RuleActionKind.Exclude).OrderBy(rule => rule.Priority))
            {
                if (MatchesRule(rule, icon))
                {
                    excludedKeys.Add(icon.StableKey);
                }
            }
        }

        return excludedKeys;
    }

    public static bool MatchesRule(IconRule rule, DesktopIconInfo icon)
    {
        return rule.MatchKind switch
        {
            RuleMatchKind.NameContains => !string.IsNullOrWhiteSpace(rule.MatchValue) &&
                icon.DisplayName.Contains(rule.MatchValue.Trim(), StringComparison.CurrentCultureIgnoreCase),
            RuleMatchKind.Category => Enum.TryParse<IconCategory>(rule.MatchValue, ignoreCase: true, out var category) && icon.Category == category,
            RuleMatchKind.StableKey => string.Equals(icon.StableKey, rule.MatchValue, StringComparison.OrdinalIgnoreCase),
            _ => false,
        };
    }

    private static IReadOnlyList<ArrangedIconPosition> CalculateVerticalEdgePositions(
        ArrangeProfile profile,
        IReadOnlyList<DesktopIconInfo> orderedIcons,
        Rectangle workArea,
        Size spacing,
        bool fromRight)
    {
        var rowsPerColumn = Math.Max(1, (workArea.Height - profile.TopMargin - profile.BottomMargin) / spacing.Height);
        var positions = new List<ArrangedIconPosition>(orderedIcons.Count);

        for (var index = 0; index < orderedIcons.Count; index++)
        {
            var column = index / rowsPerColumn;
            var row = index % rowsPerColumn;
            var x = fromRight
                ? workArea.Right - profile.RightMargin - spacing.Width - (column * spacing.Width)
                : workArea.Left + profile.LeftMargin + (column * spacing.Width);
            var y = workArea.Top + profile.TopMargin + (row * spacing.Height);
            positions.Add(new ArrangedIconPosition
            {
                Icon = orderedIcons[index],
                TargetPosition = new Point(x, y),
            });
        }

        return positions;
    }

    private static IReadOnlyList<ArrangedIconPosition> CalculateHorizontalEdgePositions(
        ArrangeProfile profile,
        IReadOnlyList<DesktopIconInfo> orderedIcons,
        Rectangle workArea,
        Size spacing,
        bool fromBottom)
    {
        var columnsPerRow = Math.Max(1, (workArea.Width - profile.LeftMargin - profile.RightMargin) / spacing.Width);
        var positions = new List<ArrangedIconPosition>(orderedIcons.Count);

        for (var index = 0; index < orderedIcons.Count; index++)
        {
            var row = index / columnsPerRow;
            var column = index % columnsPerRow;
            var x = workArea.Left + profile.LeftMargin + (column * spacing.Width);
            var y = fromBottom
                ? workArea.Bottom - profile.BottomMargin - spacing.Height - (row * spacing.Height)
                : workArea.Top + profile.TopMargin + (row * spacing.Height);
            positions.Add(new ArrangedIconPosition
            {
                Icon = orderedIcons[index],
                TargetPosition = new Point(x, y),
            });
        }

        return positions;
    }

    private static IReadOnlyList<ArrangedIconPosition> CalculateCenterPositions(
        ArrangeProfile profile,
        IReadOnlyList<DesktopIconInfo> orderedIcons,
        Rectangle workArea,
        Size spacing)
    {
        var maxColumns = Math.Max(1, (workArea.Width - profile.LeftMargin - profile.RightMargin) / spacing.Width);
        var maxRows = Math.Max(1, (workArea.Height - profile.TopMargin - profile.BottomMargin) / spacing.Height);
        var count = orderedIcons.Count;
        var aspect = Math.Max(0.25, (workArea.Width / (double)Math.Max(1, workArea.Height)) * (spacing.Height / (double)Math.Max(1, spacing.Width)));
        var columns = Math.Clamp((int)Math.Ceiling(Math.Sqrt(count * aspect)), 1, maxColumns);
        var rows = (int)Math.Ceiling(count / (double)columns);

        while (rows > maxRows && columns < maxColumns)
        {
            columns++;
            rows = (int)Math.Ceiling(count / (double)columns);
        }

        rows = Math.Min(rows, maxRows);
        var gridWidth = Math.Max(0, (columns - 1) * spacing.Width);
        var gridHeight = Math.Max(0, (rows - 1) * spacing.Height);
        var minX = workArea.Left + profile.LeftMargin;
        var maxX = workArea.Right - profile.RightMargin - gridWidth;
        var minY = workArea.Top + profile.TopMargin;
        var maxY = workArea.Bottom - profile.BottomMargin - gridHeight;
        var startX = Clamp(workArea.Left + ((workArea.Width - gridWidth) / 2), minX, Math.Max(minX, maxX));
        var startY = Clamp(workArea.Top + ((workArea.Height - gridHeight) / 2), minY, Math.Max(minY, maxY));

        var positions = new List<ArrangedIconPosition>(orderedIcons.Count);
        for (var i = 0; i < orderedIcons.Count; i++)
        {
            var row = i / columns;
            var column = i % columns;
            positions.Add(new ArrangedIconPosition
            {
                Icon = orderedIcons[i],
                TargetPosition = new Point(startX + (column * spacing.Width), startY + (row * spacing.Height)),
            });
        }

        return positions;
    }

    private static int Clamp(int value, int minimum, int maximum)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    private static ArrangeApplyResult ApplyMoveRequests(IEnumerable<MoveRequest> moveRequests)
    {
        var requests = moveRequests.ToList();
        if (requests.Count == 0)
        {
            return new ArrangeApplyResult();
        }

        var listViewHandle = DesktopWindowFinder.FindDesktopListView();
        var autoArrange = EnsureAutoArrangeDisabled(listViewHandle);
        var spacing = GetDesktopIconSpacing(listViewHandle);
        var parkingPositions = CreateParkingPositions(requests);
        ApplyRawPositions(listViewHandle, parkingPositions);
        Thread.Sleep(120);

        try
        {
            requests = RemapRequestsToCurrentIndexes(requests);
        }
        catch (Exception ex)
        {
            AppLogger.Log("重新读取桌面图标索引失败，使用原索引继续应用布局。", ex);
        }

        listViewHandle = DesktopWindowFinder.FindDesktopListView();
        if (autoArrange.WasEnabled && !autoArrange.Disabled)
        {
            var retryAutoArrange = EnsureAutoArrangeDisabled(listViewHandle);
            autoArrange = autoArrange with
            {
                Disabled = retryAutoArrange.Disabled,
                Error = string.IsNullOrWhiteSpace(autoArrange.Error) ? retryAutoArrange.Error : autoArrange.Error,
            };
        }

        ApplyRawPositions(listViewHandle, requests.Select(request => (request.Index, request.TargetPosition)));

        NativeMethods.InvalidateRect(listViewHandle, IntPtr.Zero, true);
        NativeMethods.UpdateWindow(listViewHandle);
        Thread.Sleep(260);

        var result = VerifyMoveRequests(requests, autoArrange, spacing, attemptCount: 1);
        if (result.FailedCount > 0)
        {
            listViewHandle = DesktopWindowFinder.FindDesktopListView();
            var retryAutoArrange = EnsureAutoArrangeDisabled(listViewHandle);
            autoArrange = autoArrange with
            {
                WasEnabled = autoArrange.WasEnabled || retryAutoArrange.WasEnabled,
                Disabled = autoArrange.Disabled || retryAutoArrange.Disabled,
                Error = string.IsNullOrWhiteSpace(autoArrange.Error) ? retryAutoArrange.Error : autoArrange.Error,
            };
            ApplyRawPositions(listViewHandle, requests.Select(request => (request.Index, request.TargetPosition)));
            NativeMethods.InvalidateRect(listViewHandle, IntPtr.Zero, true);
            NativeMethods.UpdateWindow(listViewHandle);
            Thread.Sleep(320);
            result = VerifyMoveRequests(requests, autoArrange, spacing, attemptCount: 2);
        }

        return result;
    }

    private static IEnumerable<(int Index, Point TargetPosition)> CreateParkingPositions(IReadOnlyList<MoveRequest> requests)
    {
        var screen = Screen.PrimaryScreen ?? Screen.AllScreens.First();
        var workArea = screen.WorkingArea;
        var parkingColumns = Math.Max(1, Math.Min(12, workArea.Width / 96));
        var startX = workArea.Right - 96;
        var startY = workArea.Bottom - 96;

        for (var i = 0; i < requests.Count; i++)
        {
            var x = Math.Max(workArea.Left, startX - ((i % parkingColumns) * 72));
            var y = Math.Max(workArea.Top, startY - ((i / parkingColumns) * 72));
            yield return (requests[i].Index, new Point(x, y));
        }
    }

    private static List<MoveRequest> RemapRequestsToCurrentIndexes(IReadOnlyList<MoveRequest> requests)
    {
        var currentIcons = GetDesktopIcons();
        var byKey = currentIcons
            .GroupBy(icon => icon.StableKey, KeyComparer)
            .ToDictionary(group => group.Key, group => new Queue<DesktopIconInfo>(group), KeyComparer);

        var remapped = new List<MoveRequest>(requests.Count);
        foreach (var request in requests)
        {
            if (byKey.TryGetValue(request.StableKey, out var matches) && matches.Count > 0)
            {
                var currentIcon = matches.Dequeue();
                remapped.Add(request with { Index = currentIcon.Index });
            }
            else
            {
                remapped.Add(request);
            }
        }

        return remapped;
    }

    private static void ApplyRawPositions(IntPtr listViewHandle, IEnumerable<(int Index, Point TargetPosition)> positions)
    {
        foreach (var (index, targetPosition) in positions)
        {
            var clientPoint = targetPosition;
            if (!NativeMethods.ScreenToClient(listViewHandle, ref clientPoint))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法换算桌面图标坐标。");
            }

            SendDesktopMessage(
                listViewHandle,
                LvmSetItemPosition,
                (IntPtr)index,
                MakeLParam(clientPoint.X, clientPoint.Y));
        }
    }

    private static Size GetDesktopIconSpacing(IntPtr listViewHandle)
    {
        var packedSpacing = SendDesktopMessage(listViewHandle, LvmGetItemSpacing, IntPtr.Zero, IntPtr.Zero).ToInt64();
        var width = (int)(packedSpacing & 0xffff);
        var height = (int)((packedSpacing >> 16) & 0xffff);

        if (width <= 0 || height <= 0)
        {
            return new Size(112, 96);
        }

        return new Size(width, height);
    }

    private static string MakeStableKey(string displayName, IconCategory category)
    {
        return $"{category}:{displayName.Trim()}";
    }

    private static IntPtr SendDesktopMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam)
    {
        var sent = NativeMethods.SendMessageTimeout(
            handle,
            message,
            wParam,
            lParam,
            SmtoAbortIfHung,
            1500,
            out var result);

        if (sent == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows 桌面没有响应。");
        }

        return result;
    }

    private static IntPtr MakeLParam(int lowWord, int highWord)
    {
        var value = ((highWord & 0xffff) << 16) | (lowWord & 0xffff);
        return (IntPtr)value;
    }

    private static ArrangeApplyResult VerifyMoveRequests(
        IReadOnlyList<MoveRequest> requests,
        AutoArrangeChange autoArrange,
        Size spacing,
        int attemptCount)
    {
        var currentIcons = GetDesktopIcons();
        var byKey = currentIcons
            .GroupBy(icon => icon.StableKey, KeyComparer)
            .ToDictionary(group => group.Key, group => new Queue<DesktopIconInfo>(group), KeyComparer);
        var tolerance = Math.Max(24, Math.Min(spacing.Width, spacing.Height) / 3);
        var failed = new List<string>();
        var verified = 0;

        foreach (var request in requests)
        {
            DesktopIconInfo? currentIcon = null;
            if (byKey.TryGetValue(request.StableKey, out var matches) && matches.Count > 0)
            {
                currentIcon = matches.Dequeue();
            }
            else
            {
                currentIcon = currentIcons.FirstOrDefault(icon => icon.Index == request.Index);
            }

            if (currentIcon is null)
            {
                failed.Add($"{request.DisplayName}：应用后没有重新读到这个图标");
                continue;
            }

            if (IsNear(currentIcon.Position, request.TargetPosition, tolerance))
            {
                verified++;
                continue;
            }

            failed.Add(
                $"{currentIcon.DisplayName}：目标 {request.TargetPosition.X},{request.TargetPosition.Y}；实际 {currentIcon.Position.X},{currentIcon.Position.Y}");
        }

        return new ArrangeApplyResult
        {
            RequestedCount = requests.Count,
            VerifiedAtTargetCount = verified,
            FailedCount = failed.Count,
            AutoArrangeWasEnabled = autoArrange.WasEnabled,
            AutoArrangeDisabled = autoArrange.Disabled,
            AutoArrangeDisableError = autoArrange.Error,
            AttemptCount = attemptCount,
            FailedIconDetails = failed,
        };
    }

    private static bool IsNear(Point actual, Point target, int tolerance)
    {
        return Math.Abs(actual.X - target.X) <= tolerance &&
               Math.Abs(actual.Y - target.Y) <= tolerance;
    }

    private static AutoArrangeChange EnsureAutoArrangeDisabled(IntPtr listViewHandle)
    {
        try
        {
            var style = GetWindowStyle(listViewHandle);
            var wasEnabled = (style & LvsAutoArrange) != 0;
            if (!wasEnabled)
            {
                return new AutoArrangeChange(false, false, string.Empty);
            }

            var newStyle = new IntPtr(style & ~LvsAutoArrange);
            NativeMethods.SetLastError(0);
            var previous = NativeMethods.SetWindowLongPtr(listViewHandle, GwlStyle, newStyle);
            var error = Marshal.GetLastWin32Error();
            if (previous == IntPtr.Zero && error != 0)
            {
                return new AutoArrangeChange(true, false, new Win32Exception(error).Message);
            }

            NativeMethods.SetWindowPos(
                listViewHandle,
                IntPtr.Zero,
                0,
                0,
                0,
                0,
                SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);

            return new AutoArrangeChange(true, true, string.Empty);
        }
        catch (Exception ex)
        {
            AppLogger.Log("关闭桌面自动排列图标失败。", ex);
            return new AutoArrangeChange(false, false, ex.Message);
        }
    }

    private static long GetWindowStyle(IntPtr listViewHandle)
    {
        NativeMethods.SetLastError(0);
        var style = NativeMethods.GetWindowLongPtr(listViewHandle, GwlStyle);
        var error = Marshal.GetLastWin32Error();
        if (style == IntPtr.Zero && error != 0)
        {
            throw new Win32Exception(error, "无法读取桌面图标视图样式。");
        }

        return style.ToInt64();
    }

    private sealed record AutoArrangeChange(bool WasEnabled, bool Disabled, string Error);

    private sealed record MoveRequest(int Index, string DisplayName, string StableKey, Point TargetPosition);

    private static class DesktopFileClassifier
    {
        private static readonly HashSet<string> DocumentExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".txt", ".rtf", ".md", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".ppt", ".pptx", ".one",
        };

        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".svg", ".ico", ".heic", ".tif", ".tiff",
        };

        private static readonly HashSet<string> MediaExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".flac", ".aac", ".m4a", ".mp4", ".mov", ".avi", ".mkv", ".wmv", ".webm",
        };

        private static readonly HashSet<string> ArchiveExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz", ".iso",
        };

        private static readonly HashSet<string> AppExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".lnk", ".url", ".exe", ".appref-ms", ".msi", ".bat", ".cmd", ".ps1",
        };

        private static readonly HashSet<string> SystemNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "回收站", "此电脑", "我的电脑", "计算机", "网络", "控制面板", "用户的文件",
            "Recycle Bin", "This PC", "Computer", "Network", "Control Panel",
        };

        public static Dictionary<string, DesktopFileMetadata> BuildDesktopFileMap()
        {
            var map = new Dictionary<string, DesktopFileMetadata>(StringComparer.OrdinalIgnoreCase);
            foreach (var directory in EnumerateDesktopDirectories())
            {
                if (!Directory.Exists(directory))
                {
                    continue;
                }

                foreach (var entry in Directory.EnumerateFileSystemEntries(directory))
                {
                    try
                    {
                        var attributes = File.GetAttributes(entry);
                        var fileInfo = attributes.HasFlag(FileAttributes.Directory) ? null : new FileInfo(entry);
                        var name = Path.GetFileName(entry);
                        var nameWithoutExtension = Path.GetFileNameWithoutExtension(entry);
                        var category = attributes.HasFlag(FileAttributes.Directory)
                            ? IconCategory.Folder
                            : ClassifyByExtension(Path.GetExtension(entry));
                        var shortcut = TryReadShortcut(entry);
                        var metadata = new DesktopFileMetadata(
                            entry,
                            category,
                            attributes.HasFlag(FileAttributes.Directory) ? string.Empty : Path.GetExtension(entry),
                            attributes.HasFlag(FileAttributes.Directory) ? null : fileInfo?.LastWriteTime,
                            attributes.HasFlag(FileAttributes.Directory) ? null : fileInfo?.Length,
                            !attributes.HasFlag(FileAttributes.Directory) && string.Equals(Path.GetExtension(entry), ".lnk", StringComparison.OrdinalIgnoreCase),
                            shortcut.TargetPath,
                            shortcut.TargetExists);

                        AddKey(map, name, metadata);
                        AddKey(map, nameWithoutExtension, metadata);

                        if (metadata.IsShortcut)
                        {
                            AddShortcutDisplayKeys(map, entry, metadata);
                        }
                    }
                    catch
                    {
                        // Ignore entries that cannot be inspected.
                    }
                }
            }

            return map;
        }

        public static DesktopFileMetadata? Find(string displayName, IReadOnlyDictionary<string, DesktopFileMetadata> desktopFiles)
        {
            displayName = NormalizeDisplayName(displayName) ?? string.Empty;
            return desktopFiles.TryGetValue(displayName, out var metadata) ? metadata : null;
        }

        public static IconCategory Classify(string displayName, IReadOnlyDictionary<string, DesktopFileMetadata> desktopFiles)
        {
            displayName = NormalizeDisplayName(displayName) ?? string.Empty;
            if (desktopFiles.TryGetValue(displayName, out var metadata))
            {
                return metadata.Category;
            }

            if (SystemNames.Contains(displayName.Trim()))
            {
                return IconCategory.System;
            }

            try
            {
                var extension = Path.GetExtension(displayName);
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    return ClassifyByExtension(extension);
                }
            }
            catch
            {
                // Fall back to Other below.
            }

            return IconCategory.Other;
        }

        private static IEnumerable<string> EnumerateDesktopDirectories()
        {
            yield return Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);
        }

        private static IconCategory ClassifyByExtension(string extension)
        {
            if (AppExtensions.Contains(extension))
            {
                return IconCategory.ShortcutOrApp;
            }

            if (DocumentExtensions.Contains(extension))
            {
                return IconCategory.Document;
            }

            if (ImageExtensions.Contains(extension))
            {
                return IconCategory.Image;
            }

            if (MediaExtensions.Contains(extension))
            {
                return IconCategory.Media;
            }

            if (ArchiveExtensions.Contains(extension))
            {
                return IconCategory.Archive;
            }

            return IconCategory.Other;
        }

        private static void AddKey(Dictionary<string, DesktopFileMetadata> map, string? key, DesktopFileMetadata metadata)
        {
            key = NormalizeDisplayName(key);
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
            {
                map.Add(key, metadata);
            }
        }

        private static string? NormalizeDisplayName(string? key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return key;
            }

            return key.Trim().Replace("\r", " ").Replace("\n", " ");
        }

        private static void AddShortcutDisplayKeys(Dictionary<string, DesktopFileMetadata> map, string path, DesktopFileMetadata metadata)
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(path);
            AddKey(map, nameWithoutExtension, metadata);

            try
            {
                if (!string.IsNullOrWhiteSpace(metadata.ShortcutTargetPath))
                {
                    AddKey(map, Path.GetFileNameWithoutExtension(metadata.ShortcutTargetPath), metadata);
                    AddKey(map, Path.GetFileName(metadata.ShortcutTargetPath), metadata);
                }
            }
            catch
            {
                // Shortcut metadata is best-effort; the filename keys above are enough for most icons.
            }
        }

        private static (string? TargetPath, bool? TargetExists) TryReadShortcut(string path)
        {
            if (!string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return (null, null);
            }

            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType is null)
                {
                    return (null, null);
                }

                dynamic shell = Activator.CreateInstance(shellType)!;
                dynamic shortcut = shell.CreateShortcut(path);
                string targetPath = shortcut.TargetPath;
                if (string.IsNullOrWhiteSpace(targetPath))
                {
                    return (null, false);
                }

                return (targetPath, File.Exists(targetPath) || Directory.Exists(targetPath));
            }
            catch
            {
                return (null, null);
            }
        }

        public sealed record DesktopFileMetadata(
            string Path,
            IconCategory Category,
            string? Extension,
            DateTime? LastModifiedAt,
            long? FileSizeBytes,
            bool IsShortcut,
            string? ShortcutTargetPath,
            bool? ShortcutTargetExists);
    }

    private sealed class RemoteListViewReader : IDisposable
    {
        private readonly IntPtr _listViewHandle;
        private readonly IntPtr _processHandle;
        private readonly IntPtr _remoteMemory;
        private readonly int _lvItemSize;
        private readonly IntPtr _remoteTextBuffer;
        private readonly IntPtr _remotePointBuffer;
        private bool _disposed;

        private RemoteListViewReader(IntPtr listViewHandle, IntPtr processHandle, IntPtr remoteMemory, int lvItemSize)
        {
            _listViewHandle = listViewHandle;
            _processHandle = processHandle;
            _remoteMemory = remoteMemory;
            _lvItemSize = lvItemSize;
            _remoteTextBuffer = IntPtr.Add(remoteMemory, lvItemSize);
            _remotePointBuffer = IntPtr.Add(_remoteTextBuffer, MaxTextLength * 2);
        }

        public static RemoteListViewReader Create(IntPtr listViewHandle)
        {
            NativeMethods.GetWindowThreadProcessId(listViewHandle, out var processId);
            if (processId == 0)
            {
                throw new InvalidOperationException("无法读取桌面进程信息。");
            }

            var processHandle = NativeMethods.OpenProcess(
                NativeMethods.ProcessQueryInformation | NativeMethods.ProcessVmOperation | NativeMethods.ProcessVmRead | NativeMethods.ProcessVmWrite,
                false,
                processId);

            if (processHandle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法打开 Windows 桌面进程。");
            }

            var lvItemSize = Marshal.SizeOf<NativeMethods.LvItemW>();
            var allocationSize = (nuint)(lvItemSize + (MaxTextLength * 2) + Marshal.SizeOf<NativeMethods.NativePoint>());
            var remoteMemory = NativeMethods.VirtualAllocEx(
                processHandle,
                IntPtr.Zero,
                allocationSize,
                NativeMethods.MemCommit | NativeMethods.MemReserve,
                NativeMethods.PageReadWrite);

            if (remoteMemory == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                NativeMethods.CloseHandle(processHandle);
                throw new Win32Exception(error, "无法分配桌面读取缓冲区。");
            }

            return new RemoteListViewReader(listViewHandle, processHandle, remoteMemory, lvItemSize);
        }

        public string GetItemText(int index)
        {
            var item = new NativeMethods.LvItemW
            {
                iSubItem = 0,
                pszText = _remoteTextBuffer,
                cchTextMax = MaxTextLength,
            };

            WriteStruct(_remoteMemory, item);
            SendDesktopMessage(_listViewHandle, LvmGetItemTextW, (IntPtr)index, _remoteMemory);

            var bytes = new byte[MaxTextLength * 2];
            if (!NativeMethods.ReadProcessMemory(_processHandle, _remoteTextBuffer, bytes, bytes.Length, out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法读取桌面图标名称。");
            }

            return System.Text.Encoding.Unicode.GetString(bytes).TrimEnd('\0');
        }

        public Point GetItemPosition(int index)
        {
            SendDesktopMessage(_listViewHandle, LvmGetItemPosition, (IntPtr)index, _remotePointBuffer);
            var point = ReadStruct<NativeMethods.NativePoint>(_remotePointBuffer);
            var screenPoint = new Point(point.X, point.Y);
            if (!NativeMethods.ClientToScreen(_listViewHandle, ref screenPoint))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法读取桌面图标位置。");
            }

            return screenPoint;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            if (_remoteMemory != IntPtr.Zero)
            {
                NativeMethods.VirtualFreeEx(_processHandle, _remoteMemory, 0, NativeMethods.MemRelease);
            }

            if (_processHandle != IntPtr.Zero)
            {
                NativeMethods.CloseHandle(_processHandle);
            }

            _disposed = true;
        }

        private void WriteStruct<T>(IntPtr remoteAddress, T value)
            where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var local = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(value, local, false);
                var bytes = new byte[size];
                Marshal.Copy(local, bytes, 0, size);
                if (!NativeMethods.WriteProcessMemory(_processHandle, remoteAddress, bytes, bytes.Length, out _))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "无法写入桌面读取缓冲区。");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(local);
            }
        }

        private T ReadStruct<T>(IntPtr remoteAddress)
            where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var bytes = new byte[size];
            if (!NativeMethods.ReadProcessMemory(_processHandle, remoteAddress, bytes, bytes.Length, out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "无法读取桌面图标数据。");
            }

            var local = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, local, size);
                return Marshal.PtrToStructure<T>(local);
            }
            finally
            {
                Marshal.FreeHGlobal(local);
            }
        }
    }

    private static class DesktopWindowFinder
    {
        public static IntPtr FindDesktopListView()
        {
            var progman = NativeMethods.FindWindow("Progman", null);
            var shellView = progman == IntPtr.Zero
                ? IntPtr.Zero
                : NativeMethods.FindWindowEx(progman, IntPtr.Zero, "SHELLDLL_DefView", null);

            if (shellView == IntPtr.Zero)
            {
                NativeMethods.EnumWindows((windowHandle, _) =>
                {
                    shellView = NativeMethods.FindWindowEx(windowHandle, IntPtr.Zero, "SHELLDLL_DefView", null);
                    return shellView == IntPtr.Zero;
                }, IntPtr.Zero);
            }

            if (shellView == IntPtr.Zero)
            {
                throw new InvalidOperationException("没有找到 Windows 桌面窗口。请确认 Explorer 桌面正在运行。");
            }

            var listView = NativeMethods.FindWindowEx(shellView, IntPtr.Zero, "SysListView32", "FolderView");
            if (listView == IntPtr.Zero)
            {
                listView = NativeMethods.FindWindowEx(shellView, IntPtr.Zero, "SysListView32", null);
            }

            if (listView == IntPtr.Zero)
            {
                throw new InvalidOperationException("没有找到桌面图标列表。请确认桌面图标没有被隐藏。");
            }

            return listView;
        }
    }

    private static class NativeMethods
    {
        public const uint ProcessVmOperation = 0x0008;
        public const uint ProcessVmRead = 0x0010;
        public const uint ProcessVmWrite = 0x0020;
        public const uint ProcessQueryInformation = 0x0400;
        public const uint MemCommit = 0x1000;
        public const uint MemReserve = 0x2000;
        public const uint MemRelease = 0x8000;
        public const uint PageReadWrite = 0x04;

        public delegate bool EnumWindowsProc(IntPtr windowHandle, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        public struct NativePoint
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LvItemW
        {
            public uint mask;
            public int iItem;
            public int iSubItem;
            public uint state;
            public uint stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public int iImage;
            public IntPtr lParam;
            public int iIndent;
            public int iGroupId;
            public uint cColumns;
            public IntPtr puColumns;
            public IntPtr piColFmt;
            public int iGroup;
        }

        [DllImport("user32.dll", EntryPoint = "FindWindowW", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string className, string? windowName);

        [DllImport("user32.dll", EntryPoint = "FindWindowExW", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string? windowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SendMessageTimeout(
            IntPtr windowHandle,
            uint message,
            IntPtr wParam,
            IntPtr lParam,
            uint flags,
            uint timeout,
            out IntPtr result);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        public static extern IntPtr GetWindowLongPtr(IntPtr windowHandle, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr windowHandle, int index, IntPtr newLong);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(
            IntPtr windowHandle,
            IntPtr windowInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ScreenToClient(IntPtr windowHandle, ref Point point);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ClientToScreen(IntPtr windowHandle, ref Point point);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InvalidateRect(IntPtr windowHandle, IntPtr rectangle, [MarshalAs(UnmanagedType.Bool)] bool erase);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateWindow(IntPtr windowHandle);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr windowHandle, out uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint desiredAccess, [MarshalAs(UnmanagedType.Bool)] bool inheritHandle, uint processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(IntPtr processHandle, IntPtr address, nuint size, uint allocationType, uint protect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool VirtualFreeEx(IntPtr processHandle, IntPtr address, nuint size, uint freeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ReadProcessMemory(IntPtr processHandle, IntPtr baseAddress, byte[] buffer, int size, out nuint bytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool WriteProcessMemory(IntPtr processHandle, IntPtr baseAddress, byte[] buffer, int size, out nuint bytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll")]
        public static extern void SetLastError(uint errorCode);
    }
}
