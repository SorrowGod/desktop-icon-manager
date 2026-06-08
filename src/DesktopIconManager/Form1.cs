using System.Diagnostics;

namespace DesktopIconManager;

public partial class Form1 : Form
{
    private readonly ComboBox _profileComboBox = new();
    private readonly ComboBox _screenComboBox = new();
    private readonly ComboBox _layoutComboBox = new();
    private readonly ComboBox _sortComboBox = new();
    private readonly ComboBox _patternKindComboBox = new();
    private readonly ComboBox _patternFillComboBox = new();
    private readonly ComboBox _sceneComboBox = new();
    private readonly ComboBox _sceneLayoutComboBox = new();
    private readonly ComboBox _sceneSortComboBox = new();
    private readonly ComboBox _profileEditorLayoutComboBox = new();
    private readonly ComboBox _profileEditorSortComboBox = new();
    private readonly ComboBox _profileEditorScreenComboBox = new();
    private readonly TextBox _patternTextBox = new();
    private readonly TextBox _imageMaskPathBox = new();
    private readonly CheckBox _showBeforeCheckBox = new();
    private readonly CheckBox _useDesktopSpacingCheckBox = new();
    private readonly CheckBox _startupCheckBox = new();
    private readonly CheckBox _safeArrangeCheckBox = new();
    private readonly CheckBox _excludeSystemIconsCheckBox = new();
    private readonly CheckBox _sceneUseDesktopZonesCheckBox = new();
    private readonly CheckBox _sceneExcludeSystemIconsCheckBox = new();
    private readonly CheckBox _profileEditorUseDesktopSpacingCheckBox = new();
    private readonly CheckBox _profileEditorSafeArrangeCheckBox = new();
    private readonly CheckBox _profileEditorStartupCheckBox = new();
    private readonly CheckBox _profileEditorExcludeSystemIconsCheckBox = new();
    private readonly NumericUpDown _leftMarginBox = new();
    private readonly NumericUpDown _topMarginBox = new();
    private readonly NumericUpDown _rightMarginBox = new();
    private readonly NumericUpDown _bottomMarginBox = new();
    private readonly NumericUpDown _columnSpacingBox = new();
    private readonly NumericUpDown _rowSpacingBox = new();
    private readonly NumericUpDown _patternCenterXBox = new();
    private readonly NumericUpDown _patternCenterYBox = new();
    private readonly NumericUpDown _patternWidthBox = new();
    private readonly NumericUpDown _patternHeightBox = new();
    private readonly NumericUpDown _patternRotationBox = new();
    private readonly NumericUpDown _patternSpacingBox = new();
    private readonly NumericUpDown _snapshotRetentionBox = new();
    private readonly NumericUpDown _profileEditorSnapshotRetentionBox = new();
    private readonly TextBox _iconSearchBox = new();
    private readonly ComboBox _categoryFilterComboBox = new();
    private readonly ListView _iconListView = new();
    private readonly ListView _profileListView = new();
    private readonly ListView _snapshotListView = new();
    private readonly ListView _zoneListView = new();
    private readonly ListView _healthListView = new();
    private readonly ListView _fileOrganizeListView = new();
    private readonly ListBox _suggestionListBox = new();
    private readonly PreviewCanvas _previewCanvas = new();
    private SplitContainer? _arrangeSplitContainer;
    private SplitContainer? _previewSplitContainer;
    private readonly Label _summaryLabel = new();
    private readonly Label _previewLabel = new();
    private readonly Label _healthSummaryLabel = new();
    private readonly Label _fileOrganizeSummaryLabel = new();
    private readonly Label _manualPointCountLabel = new();
    private readonly Label _sceneEditorStatusLabel = new();
    private readonly Label _profileEditorStatusLabel = new();
    private readonly Label _profileEditorSummaryLabel = new();
    private readonly CheckBox _checkUpdatesOnStartupCheckBox = new();
    private readonly CheckBox _diagnosticsFullPathsCheckBox = new();
    private readonly TextBox _updateManifestUrlBox = new();
    private readonly NumericUpDown _fileOrganizeRetentionBox = new();
    private readonly ComboBox _fileOrganizeKindComboBox = new();
    private readonly Label _versionLabel = new();
    private readonly Button _applyButton = new();
    private readonly Button _previewButton = new();
    private readonly Button _refreshButton = new();
    private readonly Button _saveSnapshotButton = new();
    private readonly Button _restoreButton = new();
    private readonly Button _undoButton = new();
    private readonly Button _saveProfileButton = new();
    private readonly Button _newProfileButton = new();
    private readonly Button _copyProfileButton = new();
    private readonly Button _renameProfileButton = new();
    private readonly Button _deleteProfileButton = new();
    private readonly Button _importProfileButton = new();
    private readonly Button _exportProfileButton = new();
    private readonly ToolStripStatusLabel _statusLabel = new();
    private readonly System.Windows.Forms.Timer _desktopRefreshTimer = new();
    private TabControl? _mainTabs;

    private ProfileStoreData _profileStore = new();
    private AppSettings _appSettings = AppSettings.CreateDefault();
    private ArrangeProfile _currentProfile = ProfileStore.CreateDefaultProfile();
    private IReadOnlyList<DesktopIconInfo> _desktopIcons = [];
    private List<FileOrganizeSuggestion> _fileOrganizeSuggestions = [];
    private ArrangeLayout? _currentLayout;
    private int _lastDesktopIconCount = -1;
    private bool _loading;
    private bool _busy;
    private bool _splittersInitialized;
    private bool _suppressIconCheckedEvents;
    private bool _suppressAutoSave;
    private bool _suppressSceneEditorEvents;
    private bool _suppressProfileEditorEvents;

    public Form1()
    {
        InitializeComponent();
        ConfigureWindow();
        BuildLayout();
        ConfigureAutoRefresh();
        LoadState();
    }

    private void ConfigureWindow()
    {
        Text = "桌面管理器";
        MinimumSize = new Size(1120, 740);
        Size = new Size(1280, 820);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        BackColor = UiColors.Background;
        KeyPreview = true;
        KeyDown += Form1_KeyDown;
        Shown += (_, _) => BeginInvoke(new Action(() => InitializeResponsiveSplitters(force: true)));
        Shown += (_, _) => BeginInvoke(new Action(RunStartupUiTasks));
        Resize += (_, _) => InitializeResponsiveSplitters(force: true);

        var executableIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (executableIcon is not null)
        {
            Icon = executableIcon;
        }
    }

    private void BuildLayout()
    {
        SuspendLayout();
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            Padding = new Padding(16),
            BackColor = UiColors.Background,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        root.Controls.Add(BuildHeader(), 0, 0);
        root.Controls.Add(BuildTabs(), 0, 1);
        root.Controls.Add(BuildActionPanel(), 0, 2);
        root.Controls.Add(BuildStatusStrip(), 0, 3);

        Controls.Add(root);
        ResumeLayout();
    }

    private void ConfigureAutoRefresh()
    {
        _desktopRefreshTimer.Interval = 3000;
        _desktopRefreshTimer.Tick += (_, _) => RefreshDesktopIconsIfCountChanged();
    }

    private void InitializeResponsiveSplitters(bool force = false)
    {
        if ((!force && _splittersInitialized) || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        var initialized = true;
        initialized &= TrySetSplitterDistance(_arrangeSplitContainer, vertical: true, ratio: 0.36);
        initialized &= TrySetSplitterDistance(_previewSplitContainer, vertical: false, ratio: 0.58);
        if (initialized)
        {
            _splittersInitialized = true;
        }
    }

    private static bool TrySetSplitterDistance(SplitContainer? split, bool vertical, double ratio)
    {
        if (split is null || split.IsDisposed)
        {
            return false;
        }

        var total = vertical ? split.ClientSize.Width : split.ClientSize.Height;
        var available = total - split.SplitterWidth;
        if (available < 80)
        {
            return false;
        }

        var min1 = vertical ? 260 : 140;
        var min2 = vertical ? 360 : 190;
        if (available < min1 + min2)
        {
            var scale = available / (double)(min1 + min2);
            min1 = Math.Max(40, (int)Math.Floor(min1 * scale));
            min2 = Math.Max(40, available - min1);
            if (min1 + min2 > available)
            {
                min2 = Math.Max(0, available - min1);
            }
        }

        var min = Math.Min(min1, Math.Max(0, available - min2));
        var max = available - min2;
        if (max < min || max < 0)
        {
            return false;
        }

        try
        {
            split.SplitterDistance = Math.Clamp((int)Math.Round(available * ratio), min, max);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    private Control BuildHeader()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12),
            BackColor = UiColors.Background,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 480));

        var titlePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = UiColors.Background,
        };
        titlePanel.Controls.Add(new Label
        {
            Text = "桌面管理器",
            AutoSize = true,
            Font = new Font(Font.FontFamily, 20F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = UiColors.Text,
        });
        titlePanel.Controls.Add(new Label
        {
            Text = "整理布局、桌面分区、自定义图案、快照恢复和安全诊断。",
            AutoSize = true,
            ForeColor = UiColors.MutedText,
            Margin = new Padding(1, 2, 0, 0),
        });

        _summaryLabel.Dock = DockStyle.Fill;
        _summaryLabel.TextAlign = ContentAlignment.MiddleRight;
        _summaryLabel.ForeColor = UiColors.Text;
        _summaryLabel.AutoEllipsis = true;
        _summaryLabel.Font = new Font(Font.FontFamily, 9F, FontStyle.Regular, GraphicsUnit.Point);

        panel.Controls.Add(titlePanel, 0, 0);
        panel.Controls.Add(_summaryLabel, 1, 0);
        return panel;
    }

    private Control BuildTabs()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Appearance = TabAppearance.Normal,
            HotTrack = true,
        };
        _mainTabs = tabs;
        tabs.TabPages.Add(CreateTab("整理布局", BuildArrangePage()));
        tabs.TabPages.Add(CreateTab("桌面分区", BuildZonesPage()));
        tabs.TabPages.Add(CreateTab("图标管理", BuildIconPage()));
        tabs.TabPages.Add(CreateTab("快照恢复", BuildSnapshotsPage()));
        tabs.TabPages.Add(CreateTab("方案中心", BuildProfilesPage()));
        tabs.TabPages.Add(CreateTab("场景模式", BuildScenesPage()));
        tabs.TabPages.Add(CreateTab("健康检查", BuildHealthPage()));
        tabs.TabPages.Add(CreateTab("文件收纳", BuildFileOrganizePage()));
        tabs.TabPages.Add(CreateTab("设置与诊断", BuildSettingsPage()));
        return tabs;
    }

    private static TabPage CreateTab(string title, Control content)
    {
        var page = new TabPage(title)
        {
            BackColor = UiColors.Background,
            Padding = new Padding(8),
        };
        page.Controls.Add(content);
        return page;
    }

    private Control BuildArrangePage()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 10,
            FixedPanel = FixedPanel.None,
            BackColor = UiColors.Background,
        };
        _arrangeSplitContainer = split;
        split.Panel1.Controls.Add(BuildArrangeSettings());
        split.Panel2.Controls.Add(BuildPreviewPanel());
        return split;
    }

    private Control BuildArrangeSettings()
    {
        var panel = CreateSectionPanel("整理设置");
        var grid = CreateSettingsGrid(15, 116);

        ConfigureComboBox(_profileComboBox);
        _profileComboBox.SelectedIndexChanged += (_, _) => SelectProfileFromComboBox();
        ConfigureComboBox(_screenComboBox);
        _screenComboBox.SelectedIndexChanged += (_, _) => UpdateCurrentProfileFromControls();
        ConfigureComboBox(_layoutComboBox);
        _layoutComboBox.Items.AddRange(Enum.GetValues<LayoutMode>().Cast<object>().ToArray());
        _layoutComboBox.Format += (_, e) =>
        {
            if (e.ListItem is LayoutMode mode)
            {
                e.Value = mode.ToDisplayText();
            }
        };
        _layoutComboBox.SelectedIndexChanged += (_, _) =>
        {
            UpdateCurrentProfileFromControls();
            UpdatePatternControlsVisibility();
        };
        ConfigureComboBox(_sortComboBox);
        _sortComboBox.Items.AddRange(Enum.GetValues<SortMode>().Cast<object>().ToArray());
        _sortComboBox.Format += (_, e) =>
        {
            if (e.ListItem is SortMode mode)
            {
                e.Value = mode.ToDisplayText();
            }
        };
        _sortComboBox.SelectedIndexChanged += (_, _) => UpdateCurrentProfileFromControls();

        ConfigureNumberBox(_leftMarginBox, 0, 400, 16);
        ConfigureNumberBox(_topMarginBox, 0, 400, 16);
        ConfigureNumberBox(_rightMarginBox, 0, 400, 16);
        ConfigureNumberBox(_bottomMarginBox, 0, 400, 16);
        ConfigureNumberBox(_columnSpacingBox, 48, 260, 112);
        ConfigureNumberBox(_rowSpacingBox, 48, 260, 96);
        ConfigureNumberBox(_snapshotRetentionBox, 1, 50, 10);

        _useDesktopSpacingCheckBox.Text = "使用 Windows 当前图标间距";
        _useDesktopSpacingCheckBox.AutoSize = true;
        UiTheme.StyleCheckBox(_useDesktopSpacingCheckBox);
        _useDesktopSpacingCheckBox.CheckedChanged += (_, _) =>
        {
            UpdateSpacingControls();
            UpdateCurrentProfileFromControls();
        };
        _safeArrangeCheckBox.Text = "安全整理模式";
        _safeArrangeCheckBox.AutoSize = true;
        UiTheme.StyleCheckBox(_safeArrangeCheckBox);
        _safeArrangeCheckBox.CheckedChanged += (_, _) => UpdateCurrentProfileFromControls();
        _excludeSystemIconsCheckBox.Text = "排除系统图标";
        _excludeSystemIconsCheckBox.AutoSize = true;
        UiTheme.StyleCheckBox(_excludeSystemIconsCheckBox);
        _excludeSystemIconsCheckBox.CheckedChanged += (_, _) => UpdateCurrentProfileFromControls();

        AddRow(grid, "方案", _profileComboBox, 0);
        AddRow(grid, "目标屏幕", _screenComboBox, 1);
        AddRow(grid, "布局", _layoutComboBox, 2);
        AddRow(grid, "排序", _sortComboBox, 3);
        AddRow(grid, "左边距", _leftMarginBox, 4);
        AddRow(grid, "上边距", _topMarginBox, 5);
        AddRow(grid, "右边距", _rightMarginBox, 6);
        AddRow(grid, "下边距", _bottomMarginBox, 7);
        grid.Controls.Add(_useDesktopSpacingCheckBox, 1, 8);
        AddRow(grid, "列间距", _columnSpacingBox, 9);
        AddRow(grid, "行间距", _rowSpacingBox, 10);
        grid.Controls.Add(_safeArrangeCheckBox, 1, 11);
        grid.Controls.Add(_excludeSystemIconsCheckBox, 1, 12);
        AddRow(grid, "快照保留", _snapshotRetentionBox, 13);

        _previewLabel.Dock = DockStyle.Fill;
        _previewLabel.ForeColor = UiColors.MutedText;
        _previewLabel.TextAlign = ContentAlignment.MiddleLeft;
        grid.Controls.Add(new Label
        {
            Text = "摘要",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiColors.MutedText,
        }, 0, 14);
        grid.Controls.Add(_previewLabel, 1, 14);

        panel.Controls.Add(grid);
        return panel;
    }

    private Control BuildPreviewPanel()
    {
        var panel = CreateSectionPanel("预览画布");
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 10,
            FixedPanel = FixedPanel.Panel2,
            BackColor = UiColors.Background,
        };
        _previewSplitContainer = split;

        _showBeforeCheckBox.Text = "对比：显示整理前位置（不是应用结果）";
        _showBeforeCheckBox.AutoSize = true;
        UiTheme.StyleCheckBox(_showBeforeCheckBox);
        _showBeforeCheckBox.CheckedChanged += (_, _) => RefreshPreviewCanvas();

        var previewRoot = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UiColors.Surface,
        };
        previewRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        previewRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        previewRoot.Controls.Add(_showBeforeCheckBox, 0, 0);
        previewRoot.Controls.Add(_previewCanvas, 0, 1);

        split.Panel1.Controls.Add(previewRoot);
        split.Panel2.Controls.Add(BuildPatternEditor());
        panel.Controls.Add(split);
        return panel;
    }

    private Control BuildPatternEditor()
    {
        var group = CreateSubSection("自定义图案");
        var grid = CreateSettingsGrid(4, 92);

        ConfigureComboBox(_patternKindComboBox);
        _patternKindComboBox.Items.AddRange(Enum.GetValues<PatternKind>().Cast<object>().ToArray());
        _patternKindComboBox.Format += (_, e) =>
        {
            if (e.ListItem is PatternKind kind)
            {
                e.Value = kind.ToDisplayText();
            }
        };
        _patternKindComboBox.SelectedIndexChanged += (_, _) => UpdateCurrentProfileFromControls();

        ConfigureComboBox(_patternFillComboBox);
        _patternFillComboBox.Items.AddRange(Enum.GetValues<PatternFillMode>().Cast<object>().ToArray());
        _patternFillComboBox.Format += (_, e) =>
        {
            if (e.ListItem is PatternFillMode mode)
            {
                e.Value = mode.ToDisplayText();
            }
        };
        _patternFillComboBox.SelectedIndexChanged += (_, _) => UpdateCurrentProfileFromControls();

        ConfigureNumberBox(_patternCenterXBox, 0, 100, 50);
        ConfigureNumberBox(_patternCenterYBox, 0, 100, 50);
        ConfigureNumberBox(_patternWidthBox, 10, 100, 62);
        ConfigureNumberBox(_patternHeightBox, 10, 100, 48);
        ConfigureNumberBox(_patternRotationBox, -180, 180, 0);
        ConfigureNumberBox(_patternSpacingBox, 35, 120, 75);
        _patternTextBox.Dock = DockStyle.Fill;
        UiTheme.StyleTextBox(_patternTextBox);
        _patternTextBox.TextChanged += (_, _) => UpdateCurrentProfileFromControls();
        _imageMaskPathBox.Dock = DockStyle.Fill;
        UiTheme.StyleTextBox(_imageMaskPathBox);
        _imageMaskPathBox.TextChanged += (_, _) => UpdateCurrentProfileFromControls();
        var browseMaskButton = CreateButton("选图", false, 62);
        browseMaskButton.Click += (_, _) => ChooseImageMask();
        var recordManualButton = CreateButton("记录当前点位", false, 112);
        recordManualButton.Click += (_, _) => RecordManualPatternFromDesktop();
        var clearManualButton = CreateButton("清空点位", false, 84);
        clearManualButton.Click += (_, _) => ClearManualPatternPoints();
        _manualPointCountLabel.AutoSize = true;
        _manualPointCountLabel.ForeColor = UiColors.MutedText;
        _manualPointCountLabel.Padding = new Padding(0, 7, 0, 0);

        var top = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ColumnCount = 4,
            BackColor = UiColors.SurfaceAlt,
        };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 24));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        top.Controls.Add(LabeledControl("图案", _patternKindComboBox), 0, 0);
        top.Controls.Add(LabeledControl("填充", _patternFillComboBox), 1, 0);
        top.Controls.Add(LabeledControl("文字", _patternTextBox), 2, 0);
        var imagePanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = UiColors.SurfaceAlt };
        imagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        imagePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68));
        imagePanel.Controls.Add(_imageMaskPathBox, 0, 0);
        imagePanel.Controls.Add(browseMaskButton, 1, 0);
        top.Controls.Add(LabeledControl("图片路径", imagePanel), 3, 0);

        var bottom = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = false,
            ColumnCount = 6,
            BackColor = UiColors.SurfaceAlt,
        };
        for (var column = 0; column < 6; column++)
        {
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F / 6));
        }
        bottom.Controls.Add(LabeledControl("中心 X%", _patternCenterXBox), 0, 0);
        bottom.Controls.Add(LabeledControl("中心 Y%", _patternCenterYBox), 1, 0);
        bottom.Controls.Add(LabeledControl("宽度%", _patternWidthBox), 2, 0);
        bottom.Controls.Add(LabeledControl("高度%", _patternHeightBox), 3, 0);
        bottom.Controls.Add(LabeledControl("旋转", _patternRotationBox), 4, 0);
        bottom.Controls.Add(LabeledControl("间距%", _patternSpacingBox), 5, 0);

        var manualPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 6, 0, 0),
            BackColor = UiColors.SurfaceAlt,
        };
        manualPanel.Controls.AddRange([recordManualButton, clearManualButton, _manualPointCountLabel]);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            AutoScroll = true,
            BackColor = UiColors.SurfaceAlt,
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
        content.Controls.Add(manualPanel, 0, 0);
        content.Controls.Add(top, 0, 1);
        content.Controls.Add(bottom, 0, 2);

        group.Controls.Add(content);
        return group;
    }

    private Control BuildZonesPage()
    {
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = UiColors.Background };
        split.Padding = new Padding(0);
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));

        var left = CreateSectionPanel("桌面分区");
        _zoneListView.Dock = DockStyle.Fill;
        _zoneListView.View = View.Details;
        UiTheme.StyleListView(_zoneListView);
        _zoneListView.Columns.Add("名称", 150);
        _zoneListView.Columns.Add("位置/大小", 180);
        _zoneListView.Columns.Add("布局", 120);
        _zoneListView.Columns.Add("匹配类型", 220);
        left.Controls.Add(_zoneListView);

        var right = CreateSectionPanel("分区说明");
        var text = new Label
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            MaximumSize = new Size(520, 0),
            Text = "分区只是一组桌面坐标规则，不会创建真实文件夹，也不会移动文件。\r\n\r\n默认分区会把快捷方式、文档、图片、压缩包等放入不同区域。选择“桌面分区”布局后，点击“预览”可在画布上看到分区边界。",
            ForeColor = UiColors.Text,
            Padding = new Padding(4),
        };
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(0, 16, 0, 0),
            BackColor = UiColors.Surface,
        };
        var add = CreateButton("新建分区", false, 100);
        add.Click += (_, _) => AddZone();
        var edit = CreateButton("编辑分区", false, 100);
        edit.Click += (_, _) => EditSelectedZone();
        var delete = CreateButton("删除分区", false, 100);
        delete.Click += (_, _) => DeleteSelectedZone();
        var reset = CreateButton("恢复默认分区", false, 128);
        reset.Click += (_, _) =>
        {
            _currentProfile.DesktopZones = DesktopZoneEngine.CreateDefaultZones();
            PopulateZones();
            CalculatePreview();
            PersistCurrentProfileSettings(updateStartup: false);
            SetStatus("已恢复默认桌面分区。");
        };
        buttons.Controls.AddRange([add, edit, delete, reset]);
        right.Controls.Add(buttons);
        right.Controls.Add(text);

        split.Controls.Add(left, 0, 0);
        split.Controls.Add(right, 1, 0);
        return split;
    }

    private Control BuildIconPage()
    {
        var panel = CreateSectionPanel("图标管理");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1, BackColor = UiColors.Surface };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, BackColor = UiColors.Surface, Padding = new Padding(0, 0, 0, 10) };
        _iconSearchBox.Width = 220;
        _iconSearchBox.PlaceholderText = "搜索图标名称";
        UiTheme.StyleTextBox(_iconSearchBox);
        _iconSearchBox.TextChanged += (_, _) => PopulateIconList();
        ConfigureComboBox(_categoryFilterComboBox);
        _categoryFilterComboBox.Width = 150;
        _categoryFilterComboBox.Items.Add("全部类型");
        _categoryFilterComboBox.Items.AddRange(Enum.GetValues<IconCategory>().Select(item => item.ToDisplayText()).Cast<object>().ToArray());
        _categoryFilterComboBox.SelectedIndex = 0;
        _categoryFilterComboBox.SelectedIndexChanged += (_, _) => PopulateIconList();

        var excludeAll = CreateButton("全选排除", false, 94);
        excludeAll.Click += (_, _) => SetVisibleIconChecks(true);
        var clearExclude = CreateButton("清空排除", false, 94);
        clearExclude.Click += (_, _) => SetVisibleIconChecks(false);
        var excludeSystem = CreateButton("排除系统图标", false, 118);
        excludeSystem.Click += (_, _) => ExcludeSystemIconsInList();
        var invert = CreateButton("反选", false, 70);
        invert.Click += (_, _) => InvertVisibleIconChecks();
        toolbar.Controls.AddRange([new Label { Text = "搜索", AutoSize = true, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(0, 7, 4, 0) }, _iconSearchBox, _categoryFilterComboBox, excludeAll, clearExclude, excludeSystem, invert]);

        _iconListView.Dock = DockStyle.Fill;
        _iconListView.CheckBoxes = true;
        UiTheme.StyleListView(_iconListView);
        _iconListView.View = View.Details;
        _iconListView.Columns.Add("保持原位", 86);
        _iconListView.Columns.Add("名称", 260);
        _iconListView.Columns.Add("类型", 120);
        _iconListView.Columns.Add("当前位置", 110);
        _iconListView.Columns.Add("文件信息", 260);
        _iconListView.ItemChecked += IconListView_ItemChecked;

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(_iconListView, 0, 1);
        panel.Controls.Add(root);
        return panel;
    }

    private Control BuildSnapshotsPage()
    {
        var panel = CreateSectionPanel("快照恢复");
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, BackColor = UiColors.Surface };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _snapshotListView.Dock = DockStyle.Fill;
        _snapshotListView.View = View.Details;
        UiTheme.StyleListView(_snapshotListView);
        _snapshotListView.Columns.Add("时间", 160);
        _snapshotListView.Columns.Add("方案", 140);
        _snapshotListView.Columns.Add("布局", 100);
        _snapshotListView.Columns.Add("移动/排除", 110);
        _snapshotListView.Columns.Add("备注", 280);
        root.Controls.Add(_snapshotListView, 0, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 12, 0, 0), BackColor = UiColors.Surface };
        var restoreSelected = CreateButton("恢复所选快照", true, 132);
        restoreSelected.Click += (_, _) => RestoreSelectedSnapshot();
        var deleteSelected = CreateButton("删除所选", false, 94);
        deleteSelected.Click += (_, _) => DeleteSelectedSnapshot();
        var clear = CreateButton("清空历史", false, 94);
        clear.Click += (_, _) => ClearSnapshots();
        actions.Controls.AddRange([restoreSelected, deleteSelected, clear]);
        root.Controls.Add(actions, 0, 1);
        panel.Controls.Add(root);
        return panel;
    }

    private Control BuildProfilesPage()
    {
        var panel = CreateSectionPanel("方案中心");
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiColors.Surface,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));

        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = UiColors.Surface,
            Padding = new Padding(0, 0, 12, 0),
        };
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _profileListView.Dock = DockStyle.Fill;
        _profileListView.View = View.Details;
        _profileListView.FullRowSelect = true;
        _profileListView.MultiSelect = false;
        UiTheme.StyleListView(_profileListView);
        _profileListView.Columns.Add("方案", 160);
        _profileListView.Columns.Add("标记", 84);
        _profileListView.Columns.Add("布局", 104);
        _profileListView.Columns.Add("排序", 112);
        _profileListView.Columns.Add("分区/排除", 96);
        _profileListView.SelectedIndexChanged += (_, _) => LoadSelectedProfileIntoEditor();
        left.Controls.Add(_profileListView, 0, 0);

        var managementButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 12, 0, 0),
            BackColor = UiColors.Surface,
        };
        ConfigureSmallButton(_saveProfileButton, "保存");
        ConfigureSmallButton(_newProfileButton, "新建");
        ConfigureSmallButton(_copyProfileButton, "复制");
        ConfigureSmallButton(_renameProfileButton, "重命名");
        ConfigureSmallButton(_deleteProfileButton, "删除");
        ConfigureSmallButton(_importProfileButton, "导入");
        ConfigureSmallButton(_exportProfileButton, "导出");
        _saveProfileButton.Click += (_, _) => SaveCurrentProfile();
        _newProfileButton.Click += (_, _) => CreateProfile();
        _copyProfileButton.Click += (_, _) => CopyProfile();
        _renameProfileButton.Click += (_, _) => RenameProfile();
        _deleteProfileButton.Click += (_, _) => DeleteProfile();
        _importProfileButton.Click += (_, _) => ImportProfile();
        _exportProfileButton.Click += (_, _) => ExportProfile();
        managementButtons.Controls.AddRange([_saveProfileButton, _newProfileButton, _copyProfileButton, _renameProfileButton, _deleteProfileButton, _importProfileButton, _exportProfileButton]);
        left.Controls.Add(managementButtons, 0, 1);

        var editor = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 10,
            BackColor = UiColors.Surface,
        };
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
        editor.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 8; i++)
        {
            editor.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        }
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        editor.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        ConfigureComboBox(_profileEditorLayoutComboBox);
        _profileEditorLayoutComboBox.Items.AddRange(Enum.GetValues<LayoutMode>().Cast<object>().ToArray());
        _profileEditorLayoutComboBox.Format += (_, e) =>
        {
            if (e.ListItem is LayoutMode mode)
            {
                e.Value = mode.ToDisplayText();
            }
        };
        _profileEditorLayoutComboBox.SelectedIndexChanged += (_, _) => MarkProfileEditorDirty();

        ConfigureComboBox(_profileEditorSortComboBox);
        _profileEditorSortComboBox.Items.AddRange(Enum.GetValues<SortMode>().Cast<object>().ToArray());
        _profileEditorSortComboBox.Format += (_, e) =>
        {
            if (e.ListItem is SortMode mode)
            {
                e.Value = mode.ToDisplayText();
            }
        };
        _profileEditorSortComboBox.SelectedIndexChanged += (_, _) => MarkProfileEditorDirty();

        ConfigureComboBox(_profileEditorScreenComboBox);
        _profileEditorScreenComboBox.SelectedIndexChanged += (_, _) => MarkProfileEditorDirty();
        ConfigureNumberBox(_profileEditorSnapshotRetentionBox, 1, 50, 10, autoUpdateProfile: false);
        _profileEditorSnapshotRetentionBox.ValueChanged += (_, _) => MarkProfileEditorDirty();

        ConfigureProfileEditorCheckBox(_profileEditorUseDesktopSpacingCheckBox, "使用 Windows 当前图标间距");
        ConfigureProfileEditorCheckBox(_profileEditorSafeArrangeCheckBox, "安全整理模式");
        ConfigureProfileEditorCheckBox(_profileEditorStartupCheckBox, "开机自动整理");
        ConfigureProfileEditorCheckBox(_profileEditorExcludeSystemIconsCheckBox, "排除系统图标");

        var saveSettings = CreateButton("保存方案设置", true, 124);
        saveSettings.Click += (_, _) => SaveProfileEditorSettings();
        var setDefault = CreateButton("设为默认方案", false, 124);
        setDefault.Click += (_, _) => SetSelectedProfileAsDefault();
        var editZones = CreateButton("编辑桌面分区", false, 124);
        editZones.Click += (_, _) => JumpToTab("桌面分区");
        var editPattern = CreateButton("编辑自定义图案", false, 132);
        editPattern.Click += (_, _) => JumpToTab("整理布局");
        var editExclusions = CreateButton("编辑排除图标", false, 124);
        editExclusions.Click += (_, _) => JumpToTab("图标管理");
        var editorButtons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = UiColors.Surface,
            Padding = new Padding(0, 10, 0, 0),
        };
        editorButtons.Controls.AddRange([saveSettings, setDefault, editZones, editPattern, editExclusions]);

        _profileEditorSummaryLabel.AutoSize = true;
        _profileEditorSummaryLabel.MaximumSize = new Size(640, 0);
        _profileEditorSummaryLabel.ForeColor = UiColors.Text;
        _profileEditorStatusLabel.AutoSize = true;
        _profileEditorStatusLabel.MaximumSize = new Size(640, 0);
        _profileEditorStatusLabel.ForeColor = UiColors.MutedText;

        AddRow(editor, "布局", _profileEditorLayoutComboBox, 0);
        AddRow(editor, "排序", _profileEditorSortComboBox, 1);
        AddRow(editor, "目标屏幕", _profileEditorScreenComboBox, 2);
        AddRow(editor, "快照保留", _profileEditorSnapshotRetentionBox, 3);
        editor.Controls.Add(_profileEditorUseDesktopSpacingCheckBox, 1, 4);
        editor.Controls.Add(_profileEditorSafeArrangeCheckBox, 1, 5);
        editor.Controls.Add(_profileEditorStartupCheckBox, 1, 6);
        editor.Controls.Add(_profileEditorExcludeSystemIconsCheckBox, 1, 7);
        editor.Controls.Add(editorButtons, 1, 8);
        editor.Controls.Add(_profileEditorSummaryLabel, 1, 9);

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = UiColors.Surface,
        };
        right.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.Controls.Add(editor, 0, 0);
        right.Controls.Add(_profileEditorStatusLabel, 0, 1);

        root.Controls.Add(left, 0, 0);
        root.Controls.Add(right, 1, 0);
        panel.Controls.Add(root);
        return panel;
    }

    private Control BuildScenesPage()
    {
        var panel = CreateSectionPanel("场景模式");
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 8,
            BackColor = UiColors.Surface,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        ConfigureComboBox(_sceneComboBox);
        _sceneComboBox.Width = 260;
        _sceneComboBox.SelectedIndexChanged += (_, _) => LoadSelectedSceneIntoEditor();

        ConfigureComboBox(_sceneLayoutComboBox);
        _sceneLayoutComboBox.Items.AddRange(Enum.GetValues<LayoutMode>()
            .Where(mode => mode is not LayoutMode.DesktopZones)
            .Cast<object>()
            .ToArray());
        _sceneLayoutComboBox.Format += (_, e) =>
        {
            if (e.ListItem is LayoutMode mode)
            {
                e.Value = mode.ToDisplayText();
            }
        };
        _sceneLayoutComboBox.SelectedIndexChanged += (_, _) => MarkSceneEditorDirty();

        ConfigureComboBox(_sceneSortComboBox);
        _sceneSortComboBox.Items.AddRange(Enum.GetValues<SortMode>().Cast<object>().ToArray());
        _sceneSortComboBox.Format += (_, e) =>
        {
            if (e.ListItem is SortMode mode)
            {
                e.Value = mode.ToDisplayText();
            }
        };
        _sceneSortComboBox.SelectedIndexChanged += (_, _) => MarkSceneEditorDirty();

        _sceneUseDesktopZonesCheckBox.Text = "使用桌面分区布局";
        _sceneUseDesktopZonesCheckBox.AutoSize = true;
        UiTheme.StyleCheckBox(_sceneUseDesktopZonesCheckBox);
        _sceneUseDesktopZonesCheckBox.CheckedChanged += (_, _) => MarkSceneEditorDirty();
        _sceneExcludeSystemIconsCheckBox.Text = "排除系统图标";
        _sceneExcludeSystemIconsCheckBox.AutoSize = true;
        UiTheme.StyleCheckBox(_sceneExcludeSystemIconsCheckBox);
        _sceneExcludeSystemIconsCheckBox.CheckedChanged += (_, _) => MarkSceneEditorDirty();
        _sceneEditorStatusLabel.AutoSize = true;
        _sceneEditorStatusLabel.ForeColor = UiColors.MutedText;
        _sceneEditorStatusLabel.MaximumSize = new Size(780, 0);

        var applyScene = CreateButton("套用到当前方案", true, 140);
        applyScene.Click += (_, _) => ApplySelectedScene();
        var saveSelectedScene = CreateButton("保存场景设置", false, 126);
        saveSelectedScene.Click += (_, _) => SaveSelectedSceneSettings();
        var captureCurrent = CreateButton("用当前方案更新场景", false, 158);
        captureCurrent.Click += (_, _) => CaptureCurrentProfileToSelectedScene();
        var saveScene = CreateButton("保存为新场景", false, 128);
        saveScene.Click += (_, _) => SaveCurrentAsScene();
        var deleteScene = CreateButton("删除场景", false, 100);
        deleteScene.Click += (_, _) => DeleteSelectedScene();
        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, BackColor = UiColors.Surface };
        buttons.Controls.AddRange([applyScene, saveSelectedScene, captureCurrent, saveScene, deleteScene]);
        AddRow(root, "场景", _sceneComboBox, 0);
        root.Controls.Add(buttons, 1, 1);
        AddRow(root, "默认布局", _sceneLayoutComboBox, 2);
        AddRow(root, "默认排序", _sceneSortComboBox, 3);
        root.Controls.Add(_sceneUseDesktopZonesCheckBox, 1, 4);
        root.Controls.Add(_sceneExcludeSystemIconsCheckBox, 1, 5);
        root.Controls.Add(new Label
        {
            Text = "场景模式会把布局、排序、分区和排除系统图标设置套用到当前方案。修改上面的设置后点击“保存场景设置”。如果要把当前方案里的自定义图案、排除项和桌面分区也保存进场景，点击“用当前方案更新场景”。",
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            ForeColor = UiColors.MutedText,
            Margin = new Padding(0, 12, 0, 0),
        }, 1, 6);
        root.Controls.Add(_sceneEditorStatusLabel, 1, 7);
        panel.Controls.Add(root);
        return panel;
    }

    private Control BuildHealthPage()
    {
        var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = UiColors.Background };
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
        split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
        var left = CreateSectionPanel("桌面健康检查");
        var leftRoot = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
        leftRoot.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftRoot.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _healthSummaryLabel.AutoSize = true;
        _healthSummaryLabel.ForeColor = UiColors.Text;
        _healthSummaryLabel.Margin = new Padding(0, 0, 0, 10);
        _healthListView.Dock = DockStyle.Fill;
        _healthListView.View = View.Details;
        UiTheme.StyleListView(_healthListView);
        _healthListView.Columns.Add("项目", 180);
        _healthListView.Columns.Add("类型", 120);
        _healthListView.Columns.Add("详情", 260);
        leftRoot.Controls.Add(_healthSummaryLabel, 0, 0);
        leftRoot.Controls.Add(_healthListView, 0, 1);
        left.Controls.Add(leftRoot);

        var right = CreateSectionPanel("收纳建议");
        _suggestionListBox.Dock = DockStyle.Fill;
        UiTheme.StyleListBox(_suggestionListBox);
        right.Controls.Add(_suggestionListBox);
        split.Controls.Add(left, 0, 0);
        split.Controls.Add(right, 1, 0);
        return split;
    }

    private Control BuildFileOrganizePage()
    {
        var panel = CreateSectionPanel("文件收纳");
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = UiColors.Surface,
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        _fileOrganizeSummaryLabel.AutoSize = true;
        _fileOrganizeSummaryLabel.ForeColor = UiColors.Text;
        _fileOrganizeSummaryLabel.Margin = new Padding(0, 0, 0, 10);
        _fileOrganizeSummaryLabel.Text = "文件收纳是独立功能，只有勾选并二次确认后才会移动真实文件。";

        _fileOrganizeListView.Dock = DockStyle.Fill;
        _fileOrganizeListView.View = View.Details;
        _fileOrganizeListView.CheckBoxes = true;
        UiTheme.StyleListView(_fileOrganizeListView);
        _fileOrganizeListView.Columns.Add("收纳", 64);
        _fileOrganizeListView.Columns.Add("名称", 240);
        _fileOrganizeListView.Columns.Add("类型", 110);
        _fileOrganizeListView.Columns.Add("大小", 90);
        _fileOrganizeListView.Columns.Add("建议目标", 240);
        _fileOrganizeListView.Columns.Add("原因", 260);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 12, 0, 0),
            BackColor = UiColors.Surface,
        };
        ConfigureComboBox(_fileOrganizeKindComboBox);
        _fileOrganizeKindComboBox.Width = 150;
        _fileOrganizeKindComboBox.Items.AddRange(new FileOrganizeKindChoice[]
        {
            new(FileOrganizeTargetKind.Image, "图片"),
            new(FileOrganizeTargetKind.Document, "文档"),
            new(FileOrganizeTargetKind.Media, "视频音频"),
            new(FileOrganizeTargetKind.ArchiveOrInstaller, "压缩包/安装包"),
        });
        _fileOrganizeKindComboBox.SelectedIndex = 0;
        var execute = CreateButton("执行收纳", true, 108);
        execute.Click += (_, _) => ExecuteFileOrganize();
        var refresh = CreateButton("刷新建议", false, 100);
        refresh.Click += (_, _) => RefreshFileOrganizeSuggestions();
        var undo = CreateButton("撤销上次文件收纳", false, 150);
        undo.Click += (_, _) => UndoLastFileOrganize();
        var selectKind = CreateButton("只勾选此类", false, 112);
        selectKind.Click += (_, _) => SelectFileOrganizeKindOnly();
        var selectRecommended = CreateButton("勾选推荐", false, 100);
        selectRecommended.Click += (_, _) => SetFileOrganizeChecks(recommendedOnly: true);
        var clear = CreateButton("清空勾选", false, 100);
        clear.Click += (_, _) => SetFileOrganizeChecks(recommendedOnly: false, clearAll: true);
        actions.Controls.AddRange([execute, refresh, undo, selectKind, _fileOrganizeKindComboBox, selectRecommended, clear]);

        root.Controls.Add(_fileOrganizeSummaryLabel, 0, 0);
        root.Controls.Add(_fileOrganizeListView, 0, 1);
        root.Controls.Add(actions, 0, 2);
        panel.Controls.Add(root);
        return panel;
    }

    private Control BuildSettingsPage()
    {
        var panel = CreateSectionPanel("设置与诊断");
        var root = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = UiColors.Surface };
        _startupCheckBox.Text = "开机自动整理";
        _startupCheckBox.AutoSize = true;
        UiTheme.StyleCheckBox(_startupCheckBox);
        _startupCheckBox.CheckedChanged += (_, _) => UpdateCurrentProfileFromControls();
        root.Controls.Add(_startupCheckBox);
        _checkUpdatesOnStartupCheckBox.Text = "启动时检查更新（只提示，不自动下载或安装）";
        _checkUpdatesOnStartupCheckBox.AutoSize = true;
        UiTheme.StyleCheckBox(_checkUpdatesOnStartupCheckBox);
        _checkUpdatesOnStartupCheckBox.CheckedChanged += (_, _) => SaveAppSettingsFromControls();
        root.Controls.Add(_checkUpdatesOnStartupCheckBox);
        _diagnosticsFullPathsCheckBox.Text = "导出诊断包时包含完整路径（默认关闭，可能包含隐私信息）";
        _diagnosticsFullPathsCheckBox.AutoSize = true;
        UiTheme.StyleCheckBox(_diagnosticsFullPathsCheckBox);
        _diagnosticsFullPathsCheckBox.CheckedChanged += (_, _) => SaveAppSettingsFromControls();
        root.Controls.Add(_diagnosticsFullPathsCheckBox);

        var updateGrid = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 2,
            Width = 780,
            BackColor = UiColors.Surface,
            Margin = new Padding(0, 6, 0, 8),
        };
        updateGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        updateGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _updateManifestUrlBox.Width = 560;
        _updateManifestUrlBox.PlaceholderText = "官网 update.json 地址，未配置时不会联网检查";
        UiTheme.StyleTextBox(_updateManifestUrlBox);
        _updateManifestUrlBox.TextChanged += (_, _) => SaveAppSettingsFromControls();
        ConfigureNumberBox(_fileOrganizeRetentionBox, 1, 50, 10, autoUpdateProfile: false);
        _fileOrganizeRetentionBox.ValueChanged += (_, _) => SaveAppSettingsFromControls();
        AddRow(updateGrid, "更新清单地址", _updateManifestUrlBox, 0);
        AddRow(updateGrid, "收纳记录保留", _fileOrganizeRetentionBox, 1);
        root.Controls.Add(updateGrid);

        _versionLabel.AutoSize = true;
        _versionLabel.ForeColor = UiColors.MutedText;
        _versionLabel.Padding = new Padding(0, 2, 0, 8);
        root.Controls.Add(_versionLabel);
        root.Controls.Add(new Label
        {
            Text = $"配置目录：{AppPaths.AppDataDirectory}\r\n日志文件：{AppPaths.LogPath}\r\n启动快捷方式：{StartupManager.ShortcutPath}",
            AutoSize = true,
            ForeColor = UiColors.MutedText,
            Padding = new Padding(0, 8, 0, 8),
        });
        var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight };
        var openConfig = CreateButton("打开配置文件夹", false, 132);
        openConfig.Click += (_, _) => OpenPath(AppPaths.AppDataDirectory);
        var openLog = CreateButton("打开日志", false, 94);
        openLog.Click += (_, _) => OpenPath(AppPaths.LogPath);
        var copyDiagnostics = CreateButton("复制诊断信息", false, 132);
        copyDiagnostics.Click += (_, _) => CopyDiagnostics();
        var exportDiagnostics = CreateButton("导出诊断包", false, 112);
        exportDiagnostics.Click += (_, _) => ExportDiagnosticPackage();
        var checkUpdates = CreateButton("检查更新", false, 100);
        checkUpdates.Click += async (_, _) => await CheckForUpdatesAsync(manual: true);
        var resetDefaults = CreateButton("恢复默认配置", false, 126);
        resetDefaults.Click += (_, _) => RestoreDefaultConfiguration();
        var safety = CreateButton("安全模式说明", false, 126);
        safety.Click += (_, _) => ShowSafetyGuide();
        buttons.Controls.AddRange([openConfig, openLog, copyDiagnostics, exportDiagnostics, checkUpdates, resetDefaults, safety]);
        root.Controls.Add(buttons);
        panel.Controls.Add(root);
        return panel;
    }

    private Control BuildActionPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            AutoSize = true,
            Padding = new Padding(0, 14, 0, 0),
            BackColor = UiColors.Background,
        };

        ConfigureActionButton(_applyButton, "应用", true);
        ConfigureActionButton(_previewButton, "预览", false);
        ConfigureActionButton(_refreshButton, "刷新", false);
        ConfigureActionButton(_saveSnapshotButton, "保存当前布局", false, 132);
        ConfigureActionButton(_restoreButton, "恢复", false);
        ConfigureActionButton(_undoButton, "撤销上次整理", false, 132);

        _applyButton.Click += (_, _) => ApplyLayoutWithConfirmation();
        _previewButton.Click += (_, _) => CalculatePreview();
        _refreshButton.Click += (_, _) => RefreshDesktopIcons();
        _saveSnapshotButton.Click += (_, _) => SaveCurrentSnapshot();
        _restoreButton.Click += (_, _) => RestoreSelectedOrLatestSnapshot();
        _undoButton.Click += (_, _) => RestoreLatestSnapshot();

        panel.Controls.AddRange([_applyButton, _previewButton, _refreshButton, _saveSnapshotButton, _restoreButton, _undoButton]);
        return panel;
    }

    private Control BuildStatusStrip()
    {
        var statusStrip = new StatusStrip
        {
            SizingGrip = false,
            BackColor = UiColors.SurfaceSubtle,
            ForeColor = UiColors.MutedText,
        };
        _statusLabel.Text = "准备就绪";
        _statusLabel.Spring = true;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusStrip.Items.Add(_statusLabel);
        return statusStrip;
    }

    private void LoadState()
    {
        _loading = true;
        _suppressAutoSave = true;
        try
        {
            _appSettings = AppSettingsStore.Load();
            _profileStore = ProfileStore.Load();
            LoadScreens();
            ReloadProfileCombo();
            ReloadSceneCombo();
            _currentProfile = _profileStore.Profiles.FirstOrDefault(profile =>
                    string.Equals(profile.Name, _profileStore.DefaultProfileName, StringComparison.OrdinalIgnoreCase))
                ?? _profileStore.Profiles[0];
            _profileComboBox.SelectedItem = _currentProfile;
            ApplyProfileToControls();
            ApplyAppSettingsToControls();
            PopulateProfileList();
        }
        finally
        {
            _suppressAutoSave = false;
            _loading = false;
        }

        RefreshDesktopIcons();
        LoadSnapshots();
        PopulateZones();
        PromptForPendingOperation();
        _desktopRefreshTimer.Start();
    }

    private void LoadScreens()
    {
        _screenComboBox.Items.Clear();
        _profileEditorScreenComboBox.Items.Clear();
        foreach (var screen in Screen.AllScreens.OrderByDescending(screen => screen.Primary).ThenBy(screen => screen.DeviceName))
        {
            _screenComboBox.Items.Add(new ScreenChoice(screen));
            _profileEditorScreenComboBox.Items.Add(new ScreenChoice(screen));
        }
    }

    private void ReloadProfileCombo()
    {
        _profileComboBox.Items.Clear();
        foreach (var profile in _profileStore.Profiles)
        {
            _profileComboBox.Items.Add(profile);
        }
        PopulateProfileList();
    }

    private void PopulateProfileList()
    {
        if (_profileListView.Columns.Count == 0)
        {
            return;
        }

        var selectedName = _profileListView.SelectedItems.Count > 0 && _profileListView.SelectedItems[0].Tag is ArrangeProfile selected
            ? selected.Name
            : _currentProfile.Name;
        _profileListView.BeginUpdate();
        try
        {
            _profileListView.Items.Clear();
            foreach (var profile in _profileStore.Profiles)
            {
                var markers = new List<string>();
                if (string.Equals(profile.Name, _profileStore.DefaultProfileName, StringComparison.OrdinalIgnoreCase))
                {
                    markers.Add("默认");
                }

                if (ReferenceEquals(profile, _currentProfile) || string.Equals(profile.Name, _currentProfile.Name, StringComparison.OrdinalIgnoreCase))
                {
                    markers.Add("当前");
                }

                var item = new ListViewItem(profile.Name)
                {
                    Tag = profile,
                };
                item.SubItems.Add(markers.Count == 0 ? "-" : string.Join(" / ", markers));
                item.SubItems.Add(profile.LayoutMode.ToDisplayText());
                item.SubItems.Add(profile.SortMode.ToDisplayText());
                item.SubItems.Add($"{profile.DesktopZones.Count}/{profile.ExcludedIconKeys.Count}");
                _profileListView.Items.Add(item);
                if (string.Equals(profile.Name, selectedName, StringComparison.OrdinalIgnoreCase))
                {
                    item.Selected = true;
                    item.Focused = true;
                }
            }
        }
        finally
        {
            _profileListView.EndUpdate();
        }

        if (_profileListView.SelectedItems.Count == 0 && _profileListView.Items.Count > 0)
        {
            _profileListView.Items[0].Selected = true;
        }

        LoadSelectedProfileIntoEditor();
    }

    private void LoadSelectedProfileIntoEditor()
    {
        if (_profileListView.SelectedItems.Count == 0 || _profileListView.SelectedItems[0].Tag is not ArrangeProfile profile)
        {
            return;
        }

        _suppressProfileEditorEvents = true;
        try
        {
            _profileEditorLayoutComboBox.SelectedItem = profile.LayoutMode;
            _profileEditorSortComboBox.SelectedItem = profile.SortMode;
            _profileEditorUseDesktopSpacingCheckBox.Checked = profile.UseCurrentDesktopSpacing;
            _profileEditorSafeArrangeCheckBox.Checked = profile.SafeArrangeEnabled;
            _profileEditorStartupCheckBox.Checked = profile.StartupEnabled;
            _profileEditorExcludeSystemIconsCheckBox.Checked = profile.ExcludeSystemIcons;
            _profileEditorSnapshotRetentionBox.Value = ClampForBox(_profileEditorSnapshotRetentionBox, profile.SnapshotRetentionCount);
            var screenChoice = _profileEditorScreenComboBox.Items.Cast<ScreenChoice>().FirstOrDefault(choice =>
                    string.Equals(choice.Screen.DeviceName, profile.TargetScreenDeviceName, StringComparison.OrdinalIgnoreCase))
                ?? _profileEditorScreenComboBox.Items.Cast<ScreenChoice>().FirstOrDefault();
            if (screenChoice is not null)
            {
                _profileEditorScreenComboBox.SelectedItem = screenChoice;
            }

            _profileEditorSummaryLabel.Text =
                $"方案“{profile.Name}”：分区 {profile.DesktopZones.Count} 个，排除 {profile.ExcludedIconKeys.Count} 个，自定义图案 {profile.CustomPattern.PatternKind.ToDisplayText()}，安全整理{(profile.SafeArrangeEnabled ? "开启" : "关闭")}。";
            _profileEditorStatusLabel.Text = "修改上面的设置后点击“保存方案设置”。复杂内容可用跳转按钮进入对应页面编辑。";
        }
        finally
        {
            _suppressProfileEditorEvents = false;
        }
    }

    private ArrangeProfile? GetSelectedProfileInCenter()
    {
        return _profileListView.SelectedItems.Count > 0
            ? _profileListView.SelectedItems[0].Tag as ArrangeProfile
            : null;
    }

    private void MarkProfileEditorDirty()
    {
        if (_suppressProfileEditorEvents || GetSelectedProfileInCenter() is not ArrangeProfile profile)
        {
            return;
        }

        _profileEditorStatusLabel.Text = $"方案“{profile.Name}”有未保存改动。点击“保存方案设置”后生效。";
    }

    private void SaveProfileEditorSettings()
    {
        if (GetSelectedProfileInCenter() is not ArrangeProfile profile)
        {
            AppDialog.ShowInfo(this, "保存方案设置", "请先选择一个方案。");
            return;
        }

        profile.LayoutMode = _profileEditorLayoutComboBox.SelectedItem is LayoutMode layout ? layout : profile.LayoutMode;
        profile.SortMode = _profileEditorSortComboBox.SelectedItem is SortMode sort ? sort : profile.SortMode;
        profile.TargetScreenDeviceName = (_profileEditorScreenComboBox.SelectedItem as ScreenChoice)?.Screen.DeviceName;
        profile.UseCurrentDesktopSpacing = _profileEditorUseDesktopSpacingCheckBox.Checked;
        profile.SafeArrangeEnabled = _profileEditorSafeArrangeCheckBox.Checked;
        profile.StartupEnabled = _profileEditorStartupCheckBox.Checked;
        profile.ExcludeSystemIcons = _profileEditorExcludeSystemIconsCheckBox.Checked;
        profile.SnapshotRetentionCount = (int)_profileEditorSnapshotRetentionBox.Value;
        _profileStore.DefaultProfileName = string.IsNullOrWhiteSpace(_profileStore.DefaultProfileName)
            ? profile.Name
            : _profileStore.DefaultProfileName;
        ProfileStore.Save(_profileStore);

        if (ReferenceEquals(profile, _currentProfile) || string.Equals(profile.Name, _currentProfile.Name, StringComparison.OrdinalIgnoreCase))
        {
            _currentProfile = profile;
            _suppressAutoSave = true;
            try
            {
                _profileComboBox.SelectedItem = profile;
                ApplyProfileToControls();
                PopulateIconList();
                PopulateZones();
            }
            finally
            {
                _suppressAutoSave = false;
            }

            CalculatePreview();
            SyncStartupShortcutFromProfile();
        }

        ReloadProfileCombo();
        SelectProfileInCenter(profile);
        SetStatus($"已保存方案设置：{profile.Name}");
    }

    private void SetSelectedProfileAsDefault()
    {
        if (GetSelectedProfileInCenter() is not ArrangeProfile profile)
        {
            AppDialog.ShowInfo(this, "设为默认方案", "请先选择一个方案。");
            return;
        }

        SelectAndPersistProfile(profile);
        SetStatus($"已设为默认方案：{profile.Name}");
    }

    private void SelectProfileInCenter(ArrangeProfile profile)
    {
        foreach (ListViewItem item in _profileListView.Items)
        {
            if (item.Tag is ArrangeProfile itemProfile &&
                string.Equals(itemProfile.Name, profile.Name, StringComparison.OrdinalIgnoreCase))
            {
                item.Selected = true;
                item.Focused = true;
                item.EnsureVisible();
                return;
            }
        }
    }

    private void ReloadSceneCombo()
    {
        var selectedName = _sceneComboBox.SelectedItem is DesktopScene selectedScene
            ? selectedScene.Name
            : _currentProfile.SceneName;
        _sceneComboBox.Items.Clear();
        foreach (var scene in _profileStore.Scenes)
        {
            _sceneComboBox.Items.Add(scene);
        }

        if (_sceneComboBox.Items.Count > 0)
        {
            var match = _sceneComboBox.Items.Cast<DesktopScene>().FirstOrDefault(scene =>
                string.Equals(scene.Name, selectedName, StringComparison.OrdinalIgnoreCase));
            _sceneComboBox.SelectedItem = match ?? _sceneComboBox.Items[0];
        }
    }

    private void LoadSelectedSceneIntoEditor()
    {
        if (_sceneComboBox.SelectedItem is not DesktopScene scene)
        {
            return;
        }

        _suppressSceneEditorEvents = true;
        try
        {
            _sceneLayoutComboBox.SelectedItem = scene.LayoutMode == LayoutMode.DesktopZones
                ? LayoutMode.CenterCompact
                : scene.LayoutMode;
            _sceneSortComboBox.SelectedItem = scene.SortMode;
            _sceneUseDesktopZonesCheckBox.Checked = scene.UseDesktopZones || scene.LayoutMode == LayoutMode.DesktopZones;
            _sceneExcludeSystemIconsCheckBox.Checked = scene.ExcludeSystemIcons;
            _sceneEditorStatusLabel.Text = scene.IsBuiltIn
                ? $"内置场景：{scene.Name}。可以修改并保存，但不能删除。"
                : $"自定义场景：{scene.Name}。可以修改、保存或删除。";
        }
        finally
        {
            _suppressSceneEditorEvents = false;
        }
    }

    private void MarkSceneEditorDirty()
    {
        if (_suppressSceneEditorEvents || _sceneComboBox.SelectedItem is not DesktopScene scene)
        {
            return;
        }

        _sceneEditorStatusLabel.Text = $"场景“{scene.Name}”有未保存改动。点击“保存场景设置”后生效。";
    }

    private void SelectProfileFromComboBox()
    {
        if (_loading || _profileComboBox.SelectedItem is not ArrangeProfile profile)
        {
            return;
        }

        _currentProfile = profile;
        _profileStore.DefaultProfileName = profile.Name;
        ProfileStore.Save(_profileStore);
        _loading = true;
        _suppressAutoSave = true;
        try
        {
            ApplyProfileToControls();
            PopulateIconList();
            PopulateZones();
            PopulateProfileList();
        }
        finally
        {
            _suppressAutoSave = false;
            _loading = false;
        }

        CalculatePreview();
        SyncStartupShortcutFromProfile();
    }

    private void ApplyProfileToControls()
    {
        _layoutComboBox.SelectedItem = _currentProfile.LayoutMode;
        _sortComboBox.SelectedItem = _currentProfile.SortMode;
        _useDesktopSpacingCheckBox.Checked = _currentProfile.UseCurrentDesktopSpacing;
        _startupCheckBox.Checked = _currentProfile.StartupEnabled;
        _safeArrangeCheckBox.Checked = _currentProfile.SafeArrangeEnabled;
        _excludeSystemIconsCheckBox.Checked = _currentProfile.ExcludeSystemIcons;
        _leftMarginBox.Value = ClampForBox(_leftMarginBox, _currentProfile.LeftMargin);
        _topMarginBox.Value = ClampForBox(_topMarginBox, _currentProfile.TopMargin);
        _rightMarginBox.Value = ClampForBox(_rightMarginBox, _currentProfile.RightMargin);
        _bottomMarginBox.Value = ClampForBox(_bottomMarginBox, _currentProfile.BottomMargin);
        _columnSpacingBox.Value = ClampForBox(_columnSpacingBox, _currentProfile.ColumnSpacing);
        _rowSpacingBox.Value = ClampForBox(_rowSpacingBox, _currentProfile.RowSpacing);
        _snapshotRetentionBox.Value = ClampForBox(_snapshotRetentionBox, _currentProfile.SnapshotRetentionCount);
        _patternKindComboBox.SelectedItem = _currentProfile.CustomPattern.PatternKind;
        _patternFillComboBox.SelectedItem = _currentProfile.CustomPattern.FillMode;
        _patternTextBox.Text = _currentProfile.CustomPattern.Text;
        _imageMaskPathBox.Text = _currentProfile.CustomPattern.ImageMaskPath ?? string.Empty;
        _patternCenterXBox.Value = ClampForBox(_patternCenterXBox, _currentProfile.CustomPattern.CenterXPercent);
        _patternCenterYBox.Value = ClampForBox(_patternCenterYBox, _currentProfile.CustomPattern.CenterYPercent);
        _patternWidthBox.Value = ClampForBox(_patternWidthBox, _currentProfile.CustomPattern.WidthPercent);
        _patternHeightBox.Value = ClampForBox(_patternHeightBox, _currentProfile.CustomPattern.HeightPercent);
        _patternRotationBox.Value = ClampForBox(_patternRotationBox, _currentProfile.CustomPattern.RotationDegrees);
        _patternSpacingBox.Value = ClampForBox(_patternSpacingBox, _currentProfile.CustomPattern.PointSpacing);
        _manualPointCountLabel.Text = $"手动点位：{_currentProfile.CustomPattern.ManualPoints.Count} 个";

        var screenChoice = _screenComboBox.Items.Cast<ScreenChoice>().FirstOrDefault(choice =>
                string.Equals(choice.Screen.DeviceName, _currentProfile.TargetScreenDeviceName, StringComparison.OrdinalIgnoreCase))
            ?? _screenComboBox.Items.Cast<ScreenChoice>().FirstOrDefault();
        if (screenChoice is not null)
        {
            _screenComboBox.SelectedItem = screenChoice;
        }

        UpdateSpacingControls();
        UpdatePatternControlsVisibility();
        UpdatePreviewText();
    }

    private void UpdateCurrentProfileFromControls()
    {
        if (_loading)
        {
            return;
        }

        UpdateCurrentProfileFromControlsSilently();
        PersistCurrentProfileSettings();
        UpdatePreviewText();
        RefreshPreviewCanvas();
    }

    private void UpdateCurrentProfileFromControlsSilently()
    {
        if (_loading)
        {
            return;
        }

        _currentProfile.LayoutMode = _layoutComboBox.SelectedItem is LayoutMode layoutMode ? layoutMode : _currentProfile.LayoutMode;
        _currentProfile.SortMode = _sortComboBox.SelectedItem is SortMode sortMode ? sortMode : _currentProfile.SortMode;
        _currentProfile.TargetScreenDeviceName = (_screenComboBox.SelectedItem as ScreenChoice)?.Screen.DeviceName;
        _currentProfile.UseCurrentDesktopSpacing = _useDesktopSpacingCheckBox.Checked;
        _currentProfile.StartupEnabled = _startupCheckBox.Checked;
        _currentProfile.SafeArrangeEnabled = _safeArrangeCheckBox.Checked;
        _currentProfile.ExcludeSystemIcons = _excludeSystemIconsCheckBox.Checked;
        _currentProfile.LeftMargin = (int)_leftMarginBox.Value;
        _currentProfile.TopMargin = (int)_topMarginBox.Value;
        _currentProfile.RightMargin = (int)_rightMarginBox.Value;
        _currentProfile.BottomMargin = (int)_bottomMarginBox.Value;
        _currentProfile.ColumnSpacing = (int)_columnSpacingBox.Value;
        _currentProfile.RowSpacing = (int)_rowSpacingBox.Value;
        _currentProfile.SnapshotRetentionCount = (int)_snapshotRetentionBox.Value;
        _currentProfile.CustomPattern.PatternKind = _patternKindComboBox.SelectedItem is PatternKind pattern ? pattern : _currentProfile.CustomPattern.PatternKind;
        _currentProfile.CustomPattern.FillMode = _patternFillComboBox.SelectedItem is PatternFillMode fill ? fill : _currentProfile.CustomPattern.FillMode;
        _currentProfile.CustomPattern.Text = _patternTextBox.Text;
        _currentProfile.CustomPattern.ImageMaskPath = string.IsNullOrWhiteSpace(_imageMaskPathBox.Text) ? null : _imageMaskPathBox.Text.Trim();
        _currentProfile.CustomPattern.CenterXPercent = (int)_patternCenterXBox.Value;
        _currentProfile.CustomPattern.CenterYPercent = (int)_patternCenterYBox.Value;
        _currentProfile.CustomPattern.WidthPercent = (int)_patternWidthBox.Value;
        _currentProfile.CustomPattern.HeightPercent = (int)_patternHeightBox.Value;
        _currentProfile.CustomPattern.RotationDegrees = (int)_patternRotationBox.Value;
        _currentProfile.CustomPattern.PointSpacing = (int)_patternSpacingBox.Value;
    }

    private void PersistCurrentProfileSettings(bool updateStartup = true)
    {
        if (_loading || _suppressAutoSave)
        {
            return;
        }

        try
        {
            _profileStore.DefaultProfileName = _currentProfile.Name;
            ProfileStore.Save(_profileStore);
            if (updateStartup)
            {
                StartupManager.SetEnabled(_currentProfile.StartupEnabled, _currentProfile.Name);
            }

            SetStatus($"已自动保存设置：{_currentProfile.Name}");
        }
        catch (Exception ex)
        {
            AppLogger.Log("自动保存方案设置失败。", ex);
            SetStatus($"自动保存失败：{ex.Message}");
        }
    }

    private void RefreshDesktopIcons()
    {
        SetBusy(true);
        try
        {
            _desktopIcons = DesktopIconArranger.GetDesktopIcons();
            _lastDesktopIconCount = _desktopIcons.Count;
            PopulateIconList();
            AnalyzeHealth();
            RefreshFileOrganizeSuggestions();
            CalculatePreview();
            SetStatus($"已读取 {_desktopIcons.Count} 个桌面图标。");
        }
        catch (Exception ex)
        {
            SetStatus($"读取失败：{ex.Message}");
            AppDialog.ShowError(this, "读取桌面失败", ex.Message, ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void RefreshDesktopIconsIfCountChanged()
    {
        if (_busy || _loading || IsDisposed)
        {
            return;
        }

        try
        {
            var count = DesktopIconArranger.GetDesktopIconCount();
            if (count != _lastDesktopIconCount)
            {
                RefreshDesktopIcons();
                SetStatus($"检测到桌面图标数量变化，已自动刷新为 {count} 个。");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log("自动检测桌面图标数量失败。", ex);
        }
    }

    private void CalculatePreview()
    {
        try
        {
            UpdateExcludedKeysFromList();
            UpdateCurrentProfileFromControlsSilently();
            _currentLayout = DesktopIconArranger.CalculateLayout(_currentProfile, _desktopIcons);
            UpdatePreviewText();
            RefreshPreviewCanvas();
            SetStatus("预览已更新，未移动任何桌面图标。");
        }
        catch (Exception ex)
        {
            _currentLayout = null;
            RefreshPreviewCanvas();
            SetStatus($"预览失败：{ex.Message}");
            AppDialog.ShowError(this, "无法生成预览", ex.Message, ex);
        }
    }

    private void PopulateIconList()
    {
        _loading = true;
        _suppressIconCheckedEvents = true;
        _iconListView.ItemChecked -= IconListView_ItemChecked;
        try
        {
            _iconListView.BeginUpdate();
            _iconListView.Items.Clear();
            var excluded = new HashSet<string>(_currentProfile.ExcludedIconKeys, StringComparer.OrdinalIgnoreCase);
            var search = _iconSearchBox.Text.Trim();
            IconCategory? categoryFilter = _categoryFilterComboBox.SelectedIndex <= 0
                ? null
                : Enum.GetValues<IconCategory>().ElementAt(_categoryFilterComboBox.SelectedIndex - 1);

            foreach (var icon in _desktopIcons)
            {
                if (!string.IsNullOrWhiteSpace(search) && !icon.DisplayName.Contains(search, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                if (categoryFilter is not null && icon.Category != categoryFilter.Value)
                {
                    continue;
                }

                var item = new ListViewItem(string.Empty)
                {
                    Checked = excluded.Contains(icon.StableKey),
                    Tag = icon,
                };
                item.SubItems.Add(icon.DisplayName);
                item.SubItems.Add(icon.Category.ToDisplayText());
                item.SubItems.Add($"{icon.Position.X}, {icon.Position.Y}");
                item.SubItems.Add(BuildIconInfoText(icon));
                _iconListView.Items.Add(item);
            }
        }
        finally
        {
            _iconListView.EndUpdate();
            _iconListView.ItemChecked += IconListView_ItemChecked;
            _suppressIconCheckedEvents = false;
            _loading = false;
        }
    }

    private static string BuildIconInfoText(DesktopIconInfo icon)
    {
        if (icon.IsShortcut)
        {
            return icon.ShortcutTargetExists == false ? "快捷方式目标不存在" : "快捷方式";
        }

        if (icon.FileSizeBytes is not null)
        {
            return $"{icon.Extension} {DesktopHealthAnalyzer.FormatSize(icon.FileSizeBytes.Value)}";
        }

        return icon.FilePath is null ? "系统或虚拟图标" : icon.FilePath;
    }

    private void IconListView_ItemChecked(object? sender, ItemCheckedEventArgs e)
    {
        if (_loading || _suppressIconCheckedEvents || e.Item?.Tag is not DesktopIconInfo)
        {
            return;
        }

        UpdateExcludedKeysFromList();
        PersistCurrentProfileSettings(updateStartup: false);
        UpdatePreviewText();
    }

    private void UpdateExcludedKeysFromList()
    {
        _currentProfile.ExcludedIconKeys ??= [];
        var current = new HashSet<string>(
            _currentProfile.ExcludedIconKeys.Where(key => !string.IsNullOrWhiteSpace(key)),
            StringComparer.OrdinalIgnoreCase);

        foreach (ListViewItem? item in _iconListView.Items)
        {
            if (item?.Tag is not DesktopIconInfo icon || string.IsNullOrWhiteSpace(icon.StableKey))
            {
                continue;
            }

            if (item.Checked)
            {
                current.Add(icon.StableKey);
            }
            else
            {
                current.Remove(icon.StableKey);
            }
        }

        _currentProfile.ExcludedIconKeys = current.ToList();
        PersistCurrentProfileSettings(updateStartup: false);
    }

    private void UpdatePreviewText()
    {
        try
        {
            var preview = DesktopIconArranger.CreatePreview(_currentProfile, _desktopIcons);
            var latest = SnapshotStore.GetLatest();
            _summaryLabel.Text =
                $"图标 {preview.IconCount} 个 | 可移动 {preview.MovableCount} 个 | 排除 {preview.ExcludedCount} 个 | " +
                $"{preview.LayoutMode.ToDisplayText()} | {(_currentProfile.SafeArrangeEnabled ? "安全整理开" : "安全整理关")}";
            _previewLabel.Text =
                $"布局：{preview.LayoutMode.ToDisplayText()}\r\n" +
                $"排序：{preview.SortMode.ToDisplayText()}\r\n" +
                $"屏幕：{preview.TargetScreenName}\r\n" +
                $"移动/排除：{preview.MovableCount} / {preview.ExcludedCount}\r\n" +
                $"最近快照：{(latest is null ? "暂无" : latest.CreatedAt.ToString("yyyy-MM-dd HH:mm"))}";
        }
        catch
        {
            _summaryLabel.Text = "等待读取桌面图标";
            _previewLabel.Text = "暂无预览";
        }
    }

    private void RefreshPreviewCanvas()
    {
        _previewCanvas.SetPreview(_desktopIcons, _currentLayout, _showBeforeCheckBox.Checked);
    }

    private void ApplyLayoutWithConfirmation()
    {
        UpdateExcludedKeysFromList();
        UpdateCurrentProfileFromControlsSilently();

        ArrangeLayout layout;
        ArrangePreview preview;
        try
        {
            layout = DesktopIconArranger.CalculateLayout(_currentProfile, _desktopIcons);
            preview = DesktopIconArranger.CreatePreview(_currentProfile, _desktopIcons);
        }
        catch (Exception ex)
        {
            AppDialog.ShowError(this, "无法计算布局", ex.Message, ex);
            return;
        }

        var rows = new List<DialogSummaryRow>
        {
            new DialogSummaryRow { Label = "方案", Value = _currentProfile.Name },
            new DialogSummaryRow { Label = "布局", Value = preview.LayoutMode.ToDisplayText() },
            new DialogSummaryRow { Label = "排序", Value = preview.SortMode.ToDisplayText() },
            new DialogSummaryRow { Label = "目标屏幕", Value = preview.TargetScreenName },
            new DialogSummaryRow { Label = "将移动", Value = $"{preview.MovableCount} 个图标" },
            new DialogSummaryRow { Label = "保持原位", Value = $"{preview.ExcludedCount} 个图标" },
            new DialogSummaryRow { Label = "安全整理", Value = preview.SafeArrangeEnabled ? "开启，会保存中断恢复记录" : "关闭" },
            new DialogSummaryRow { Label = "自动排列", Value = "如 Windows 已开启，应用时会自动关闭，否则图标无法自定义摆放" },
        };

        var message = preview.LayoutMode == LayoutMode.CustomPattern
            ? $"确认把桌面图标摆放为“{_currentProfile.CustomPattern.PatternKind.ToDisplayText()}”图案？应用前会自动保存快照。"
            : "确认应用当前整理方案？应用前会自动保存快照。";
        if (AppDialog.ShowConfirm(this, "确认应用", message, rows, "应用", "取消", DialogKind.Confirm) != AppDialogResult.Primary)
        {
            SetStatus("已取消应用。");
            return;
        }

        SetBusy(true);
        try
        {
            SnapshotStore.Add(
                DesktopIconArranger.CreateSnapshot(_currentProfile.Name, _desktopIcons, _currentProfile, preview.MovableCount, preview.ExcludedCount, "应用前自动保存"),
                _currentProfile.SnapshotRetentionCount);
            var applyResult = DesktopIconArranger.ApplyLayout(layout);
            SaveCurrentProfile(showMessage: false);
            _desktopIcons = DesktopIconArranger.GetDesktopIcons();
            _currentLayout = DesktopIconArranger.CalculateLayout(_currentProfile, _desktopIcons);
            PopulateIconList();
            AnalyzeHealth();
            RefreshFileOrganizeSuggestions();
            LoadSnapshots();
            UpdatePreviewText();
            RefreshPreviewCanvas();
            if (applyResult.FailedCount > 0)
            {
                ShowApplyVerificationWarning(applyResult);
                SetStatus($"应用后校验未完全通过：到位 {applyResult.VerifiedAtTargetCount}/{applyResult.RequestedCount}，未到位 {applyResult.FailedCount}。");
            }
            else
            {
                var autoArrangeText = applyResult.AutoArrangeDisabled ? "，已关闭桌面自动排列图标" : string.Empty;
                SetStatus($"已应用：到位 {applyResult.VerifiedAtTargetCount} 个，排除 {preview.ExcludedCount} 个图标{autoArrangeText}。");
            }
        }
        catch (Exception ex)
        {
            SetStatus($"应用失败：{ex.Message}");
            AppDialog.ShowError(this, "应用失败", ex.Message, ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ShowApplyVerificationWarning(ArrangeApplyResult result)
    {
        var rows = new List<DialogSummaryRow>
        {
            new() { Label = "已请求移动", Value = $"{result.RequestedCount} 个图标" },
            new() { Label = "确认到位", Value = $"{result.VerifiedAtTargetCount} 个图标" },
            new() { Label = "未到目标", Value = $"{result.FailedCount} 个图标" },
            new() { Label = "重试次数", Value = $"{result.AttemptCount} 次" },
            new()
            {
                Label = "自动排列",
                Value = result.AutoArrangeWasEnabled
                    ? result.AutoArrangeDisabled
                        ? "检测到已开启，软件已尝试关闭"
                        : "检测到已开启，但关闭失败"
                    : "未检测到开启",
            },
        };

        if (!string.IsNullOrWhiteSpace(result.AutoArrangeDisableError))
        {
            rows.Add(new DialogSummaryRow { Label = "关闭失败", Value = result.AutoArrangeDisableError });
        }

        var details = result.FailedIconDetails.Count == 0
            ? "没有更详细的未到位图标信息。"
            : string.Join(Environment.NewLine, result.FailedIconDetails.Take(40));
        var message = result.VerifiedAtTargetCount == 0
            ? "Windows 没有接受桌面图标移动指令。通常是桌面开启了“自动排列图标”、Explorer 桌面被系统策略限制，或安全软件拦截了跨进程桌面消息。"
            : "部分图标没有到达目标位置。可以先确认桌面右键“查看”里的“自动排列图标”已关闭，再重试。";

        var choice = AppDialog.ShowConfirm(
            this,
            "应用后未完全生效",
            message,
            rows,
            "知道了",
            "复制详情",
            DialogKind.Warning,
            details,
            secondaryIsCancel: false);
        if (choice == AppDialogResult.Secondary)
        {
            Clipboard.SetText(details);
            SetStatus("已复制应用校验详情。");
        }
    }

    private void SaveCurrentSnapshot()
    {
        try
        {
            UpdateCurrentProfileFromControlsSilently();
            var preview = DesktopIconArranger.CreatePreview(_currentProfile, _desktopIcons);
            SnapshotStore.Add(
                DesktopIconArranger.CreateSnapshot(_currentProfile.Name, _desktopIcons, _currentProfile, 0, preview.ExcludedCount, "手动保存"),
                _currentProfile.SnapshotRetentionCount);
            LoadSnapshots();
            SetStatus("已保存当前桌面布局快照。");
        }
        catch (Exception ex)
        {
            AppDialog.ShowError(this, "保存快照失败", ex.Message, ex);
        }
    }

    private void RestoreSelectedOrLatestSnapshot()
    {
        if (_snapshotListView.SelectedItems.Count > 0)
        {
            RestoreSelectedSnapshot();
            return;
        }

        RestoreLatestSnapshot();
    }

    private void RestoreLatestSnapshot()
    {
        var snapshot = SnapshotStore.GetLatest();
        if (snapshot is null)
        {
            AppDialog.ShowInfo(this, "恢复布局", "还没有可恢复的布局快照。");
            return;
        }

        RestoreSnapshot(snapshot);
    }

    private void RestoreSelectedSnapshot()
    {
        if (_snapshotListView.SelectedItems.Count == 0 || _snapshotListView.SelectedItems[0].Tag is not LayoutSnapshot snapshot)
        {
            AppDialog.ShowInfo(this, "恢复快照", "请先在快照列表中选择一条记录。");
            return;
        }

        RestoreSnapshot(snapshot);
    }

    private void RestoreSnapshot(LayoutSnapshot snapshot)
    {
        var rows = new[]
        {
            new DialogSummaryRow { Label = "时间", Value = snapshot.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") },
            new DialogSummaryRow { Label = "方案", Value = snapshot.ProfileName },
            new DialogSummaryRow { Label = "布局", Value = snapshot.LayoutMode.ToDisplayText() },
            new DialogSummaryRow { Label = "图标数量", Value = $"{snapshot.Icons.Count} 个" },
        };
        if (AppDialog.ShowConfirm(this, "确认恢复", "恢复前会先保存当前桌面布局，因此可以再次撤销。", rows, "恢复", "取消", DialogKind.Warning) != AppDialogResult.Primary)
        {
            return;
        }

        SetBusy(true);
        try
        {
            SnapshotStore.Add(
                DesktopIconArranger.CreateSnapshot(_currentProfile.Name, _desktopIcons, _currentProfile, 0, 0, "恢复前自动保存"),
                _currentProfile.SnapshotRetentionCount);
            var restoredCount = DesktopIconArranger.RestoreSnapshot(snapshot);
            _desktopIcons = DesktopIconArranger.GetDesktopIcons();
            PopulateIconList();
            AnalyzeHealth();
            RefreshFileOrganizeSuggestions();
            CalculatePreview();
            LoadSnapshots();
            SetStatus($"已恢复 {restoredCount} 个图标的位置。");
        }
        catch (Exception ex)
        {
            SetStatus($"恢复失败：{ex.Message}");
            AppDialog.ShowError(this, "恢复失败", ex.Message, ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void PromptForPendingOperation()
    {
        var pending = PendingOperationStore.Load();
        if (pending is null)
        {
            return;
        }

        var rows = new[]
        {
            new DialogSummaryRow { Label = "时间", Value = pending.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") },
            new DialogSummaryRow { Label = "原因", Value = pending.Reason },
            new DialogSummaryRow { Label = "可恢复图标", Value = $"{pending.Snapshot.Icons.Count} 个" },
        };

        if (AppDialog.ShowConfirm(this, "检测到上次整理可能未完成", "图标没有丢失，只是位置可能停在整理过程中的中间状态。", rows, "恢复到整理前", "保留当前桌面", DialogKind.Warning) == AppDialogResult.Primary)
        {
            SetBusy(true);
            try
            {
                var restoredCount = DesktopIconArranger.RestoreSnapshot(pending.Snapshot);
                PendingOperationStore.Clear();
                _desktopIcons = DesktopIconArranger.GetDesktopIcons();
                PopulateIconList();
                AnalyzeHealth();
                RefreshFileOrganizeSuggestions();
                CalculatePreview();
                SetStatus($"已从未完成整理记录恢复 {restoredCount} 个图标。");
            }
            catch (Exception ex)
            {
                SetStatus($"恢复未完成整理失败：{ex.Message}");
                AppDialog.ShowError(this, "恢复失败", ex.Message, ex);
            }
            finally
            {
                SetBusy(false);
            }
        }
        else
        {
            PendingOperationStore.Clear();
            SetStatus("已保留当前桌面，并清除未完成整理记录。");
        }
    }

    private void SaveCurrentProfile(bool showMessage = true)
    {
        UpdateExcludedKeysFromList();
        UpdateCurrentProfileFromControlsSilently();
        _profileStore.DefaultProfileName = _currentProfile.Name;
        ProfileStore.Save(_profileStore);
        SyncStartupShortcutFromProfile();

        if (showMessage)
        {
            SetStatus($"已保存方案：{_currentProfile.Name}");
        }
    }

    private void CreateProfile()
    {
        var name = AppDialog.ShowInput(this, "新建方案", "请输入方案名称。", GetUniqueProfileName("新方案"));
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (_profileStore.Profiles.Any(profile => string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            AppDialog.ShowInfo(this, "新建方案", "已存在同名方案。");
            return;
        }

        var profile = ProfileStore.CloneProfile(_currentProfile, name.Trim());
        _profileStore.Profiles.Add(profile);
        SelectAndPersistProfile(profile);
        SetStatus($"已新建方案：{profile.Name}");
    }

    private void CopyProfile()
    {
        var profile = ProfileStore.CloneProfile(_currentProfile, GetUniqueProfileName($"{_currentProfile.Name} 副本"));
        _profileStore.Profiles.Add(profile);
        SelectAndPersistProfile(profile);
        SetStatus($"已复制方案：{profile.Name}");
    }

    private void RenameProfile()
    {
        var name = AppDialog.ShowInput(this, "重命名方案", "请输入新的方案名称。", _currentProfile.Name);
        if (string.IsNullOrWhiteSpace(name) || string.Equals(name.Trim(), _currentProfile.Name, StringComparison.Ordinal))
        {
            return;
        }

        if (_profileStore.Profiles.Any(profile => !ReferenceEquals(profile, _currentProfile) &&
            string.Equals(profile.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            AppDialog.ShowInfo(this, "重命名方案", "已存在同名方案。");
            return;
        }

        _currentProfile.Name = name.Trim();
        SelectAndPersistProfile(_currentProfile);
        SetStatus($"已重命名方案：{_currentProfile.Name}");
    }

    private void DeleteProfile()
    {
        if (_profileStore.Profiles.Count <= 1)
        {
            AppDialog.ShowInfo(this, "删除方案", "至少需要保留一个方案。");
            return;
        }

        if (AppDialog.ShowConfirm(this, "删除方案", $"确认删除方案“{_currentProfile.Name}”？", null, "删除", "取消", DialogKind.Warning) != AppDialogResult.Primary)
        {
            return;
        }

        _profileStore.Profiles.Remove(_currentProfile);
        SelectAndPersistProfile(_profileStore.Profiles[0]);
        SetStatus("已删除方案。");
    }

    private void ImportProfile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "导入方案",
            Filter = "方案文件 (*.json)|*.json|所有文件 (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var profile = ProfileStore.ImportProfile(dialog.FileName);
            if (_profileStore.Profiles.Any(item => string.Equals(item.Name, profile.Name, StringComparison.OrdinalIgnoreCase)))
            {
                profile.Name = GetUniqueProfileName($"{profile.Name} 导入");
            }

            _profileStore.Profiles.Add(profile);
            SelectAndPersistProfile(profile);
            AppDialog.ShowInfo(this, "导入完成", $"已导入方案：{profile.Name}");
        }
        catch (Exception ex)
        {
            AppDialog.ShowError(this, "导入失败", ex.Message, ex);
        }
    }

    private void ExportProfile()
    {
        using var dialog = new SaveFileDialog
        {
            Title = "导出方案",
            Filter = "方案文件 (*.json)|*.json",
            FileName = $"{_currentProfile.Name}.json",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            SaveCurrentProfile(showMessage: false);
            ProfileStore.ExportProfile(_currentProfile, dialog.FileName);
            AppDialog.ShowInfo(this, "导出完成", $"方案已导出到：{dialog.FileName}");
        }
        catch (Exception ex)
        {
            AppDialog.ShowError(this, "导出失败", ex.Message, ex);
        }
    }

    private void SelectAndPersistProfile(ArrangeProfile profile)
    {
        _currentProfile = profile;
        _profileStore.DefaultProfileName = profile.Name;
        ProfileStore.Save(_profileStore);
        _suppressAutoSave = true;
        ReloadProfileCombo();
        try
        {
            _profileComboBox.SelectedItem = profile;
            ApplyProfileToControls();
            PopulateIconList();
            PopulateZones();
        }
        finally
        {
            _suppressAutoSave = false;
        }
        CalculatePreview();
        SelectProfileInCenter(profile);
    }

    private void LoadSnapshots()
    {
        _snapshotListView.BeginUpdate();
        try
        {
            _snapshotListView.Items.Clear();
            foreach (var snapshot in SnapshotStore.Load().OrderByDescending(item => item.CreatedAt))
            {
                var item = new ListViewItem(snapshot.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"))
                {
                    Tag = snapshot,
                };
                item.SubItems.Add(snapshot.ProfileName);
                item.SubItems.Add(snapshot.LayoutMode.ToDisplayText());
                item.SubItems.Add($"{snapshot.MovedCount}/{snapshot.ExcludedCount}");
                item.SubItems.Add(snapshot.Note);
                _snapshotListView.Items.Add(item);
            }
        }
        finally
        {
            _snapshotListView.EndUpdate();
        }
    }

    private void DeleteSelectedSnapshot()
    {
        if (_snapshotListView.SelectedItems.Count == 0 || _snapshotListView.SelectedItems[0].Tag is not LayoutSnapshot snapshot)
        {
            return;
        }

        SnapshotStore.Delete(snapshot.Id);
        LoadSnapshots();
        SetStatus("已删除所选快照。");
    }

    private void ClearSnapshots()
    {
        if (AppDialog.ShowConfirm(this, "清空快照历史", "确认清空所有快照记录？这不会影响桌面文件。", null, "清空", "取消", DialogKind.Warning) != AppDialogResult.Primary)
        {
            return;
        }

        SnapshotStore.Clear();
        LoadSnapshots();
        SetStatus("已清空快照历史。");
    }

    private void PopulateZones()
    {
        _zoneListView.BeginUpdate();
        try
        {
            _zoneListView.Items.Clear();
            foreach (var zone in _currentProfile.DesktopZones)
            {
                var categories = zone.Categories.Count == 0
                    ? "未匹配项"
                    : string.Join("、", zone.Categories.Select(item => item.ToDisplayText()));
                var item = new ListViewItem(DesktopZoneEngine.IsBuiltInZone(zone) ? $"{zone.Name}（内置）" : zone.Name)
                {
                    Tag = zone,
                };
                item.SubItems.Add($"{zone.XPercent}%,{zone.YPercent}% / {zone.WidthPercent}%x{zone.HeightPercent}%");
                item.SubItems.Add(zone.LayoutMode.ToDisplayText());
                item.SubItems.Add(categories);
                _zoneListView.Items.Add(item);
            }
        }
        finally
        {
            _zoneListView.EndUpdate();
        }
    }

    private void AddZone()
    {
        var name = AppDialog.ShowInput(this, "新建分区", "请输入分区名称。", GetUniqueZoneName("新区块"));
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var zone = new DesktopZone
        {
            Name = name.Trim(),
            IsBuiltIn = false,
            XPercent = 10,
            YPercent = 10,
            WidthPercent = 35,
            HeightPercent = 45,
            LayoutMode = LayoutMode.Top,
            SortMode = SortMode.TypeThenName,
        };
        _currentProfile.DesktopZones.Add(zone);
        DesktopZoneEngine.ReflowZones(_currentProfile.DesktopZones);
        PopulateZones();
        CalculatePreview();
        PersistCurrentProfileSettings(updateStartup: false);
        SetStatus($"已新建分区并自动调整所有分区位置：{zone.Name}");
    }

    private void EditSelectedZone()
    {
        if (_zoneListView.SelectedItems.Count == 0 || _zoneListView.SelectedItems[0].Tag is not DesktopZone zone)
        {
            AppDialog.ShowInfo(this, "编辑分区", "请先选择一个分区。");
            return;
        }

        using var form = new ZoneEditorForm(zone);
        if (form.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        PopulateZones();
        CalculatePreview();
        PersistCurrentProfileSettings(updateStartup: false);
        SetStatus($"已更新分区：{zone.Name}");
    }

    private void DeleteSelectedZone()
    {
        if (_zoneListView.SelectedItems.Count == 0 || _zoneListView.SelectedItems[0].Tag is not DesktopZone zone)
        {
            AppDialog.ShowInfo(this, "删除分区", "请先选择一个分区。");
            return;
        }

        if (DesktopZoneEngine.IsBuiltInZone(zone))
        {
            AppDialog.ShowInfo(this, "删除分区", $"“{zone.Name}”是默认内置分区，不能删除。你可以编辑它的规则，或新建自定义分区。");
            return;
        }

        if (AppDialog.ShowConfirm(this, "删除分区", $"确认删除分区“{zone.Name}”？这不会删除任何文件。", null, "删除", "取消", DialogKind.Warning) != AppDialogResult.Primary)
        {
            return;
        }

        _currentProfile.DesktopZones.Remove(zone);
        DesktopZoneEngine.ReflowZones(_currentProfile.DesktopZones);
        PopulateZones();
        CalculatePreview();
        PersistCurrentProfileSettings(updateStartup: false);
        SetStatus($"已删除分区并自动调整剩余分区位置：{zone.Name}");
    }

    private string GetUniqueZoneName(string prefix)
    {
        var index = 1;
        while (_currentProfile.DesktopZones.Any(zone => string.Equals(zone.Name, $"{prefix} {index}", StringComparison.OrdinalIgnoreCase)))
        {
            index++;
        }

        return $"{prefix} {index}";
    }

    private void ApplySelectedScene()
    {
        if (_sceneComboBox.SelectedItem is not DesktopScene scene)
        {
            return;
        }

        var rows = new[]
        {
            new DialogSummaryRow { Label = "场景", Value = scene.Name },
            new DialogSummaryRow { Label = "布局", Value = scene.LayoutMode.ToDisplayText() },
            new DialogSummaryRow { Label = "排序", Value = scene.SortMode.ToDisplayText() },
            new DialogSummaryRow { Label = "目标方案", Value = _currentProfile.Name },
        };
        if (AppDialog.ShowConfirm(this, "套用场景", "场景只会修改当前方案设置，不会立即移动桌面图标。点击“应用”时仍会再次确认。", rows, "套用", "取消") != AppDialogResult.Primary)
        {
            return;
        }

        _currentProfile.SceneName = scene.Name;
        _currentProfile.LayoutMode = scene.UseDesktopZones ? LayoutMode.DesktopZones : scene.LayoutMode;
        _currentProfile.SortMode = scene.SortMode;
        _currentProfile.ExcludeSystemIcons = scene.ExcludeSystemIcons;
        _currentProfile.ExcludedIconKeys = [.. scene.ExcludedIconKeys];
        _currentProfile.CustomPattern = ClonePattern(scene.CustomPattern);
        if (scene.DesktopZones.Count > 0)
        {
            _currentProfile.DesktopZones = scene.DesktopZones.Select(CloneZone).ToList();
        }

        _suppressAutoSave = true;
        try
        {
            ApplyProfileToControls();
            PopulateIconList();
            PopulateZones();
        }
        finally
        {
            _suppressAutoSave = false;
        }
        CalculatePreview();
        PersistCurrentProfileSettings();
        SetStatus($"已套用场景：{scene.Name}");
    }

    private void SaveSelectedSceneSettings()
    {
        if (_sceneComboBox.SelectedItem is not DesktopScene scene)
        {
            return;
        }

        scene.LayoutMode = _sceneLayoutComboBox.SelectedItem is LayoutMode layout ? layout : scene.LayoutMode;
        scene.SortMode = _sceneSortComboBox.SelectedItem is SortMode sort ? sort : scene.SortMode;
        scene.UseDesktopZones = _sceneUseDesktopZonesCheckBox.Checked;
        scene.ExcludeSystemIcons = _sceneExcludeSystemIconsCheckBox.Checked;
        if (scene.UseDesktopZones && scene.DesktopZones.Count == 0)
        {
            scene.DesktopZones = _currentProfile.DesktopZones.Select(CloneZone).ToList();
        }

        ProfileStore.Save(_profileStore);
        ReloadSceneCombo();
        _sceneComboBox.SelectedItem = _profileStore.Scenes.FirstOrDefault(item =>
            string.Equals(item.Name, scene.Name, StringComparison.OrdinalIgnoreCase));
        LoadSelectedSceneIntoEditor();
        SetStatus($"已保存场景设置：{scene.Name}");
    }

    private void CaptureCurrentProfileToSelectedScene()
    {
        if (_sceneComboBox.SelectedItem is not DesktopScene scene)
        {
            return;
        }

        UpdateExcludedKeysFromList();
        UpdateCurrentProfileFromControlsSilently();
        scene.ProfileName = _currentProfile.Name;
        scene.LayoutMode = _currentProfile.LayoutMode == LayoutMode.DesktopZones
            ? LayoutMode.CenterCompact
            : _currentProfile.LayoutMode;
        scene.SortMode = _currentProfile.SortMode;
        scene.UseDesktopZones = _currentProfile.LayoutMode == LayoutMode.DesktopZones;
        scene.ExcludeSystemIcons = _currentProfile.ExcludeSystemIcons;
        scene.CustomPattern = ClonePattern(_currentProfile.CustomPattern);
        scene.DesktopZones = _currentProfile.DesktopZones.Select(CloneZone).ToList();
        scene.ExcludedIconKeys = [.. _currentProfile.ExcludedIconKeys];

        ProfileStore.Save(_profileStore);
        ReloadSceneCombo();
        _sceneComboBox.SelectedItem = _profileStore.Scenes.FirstOrDefault(item =>
            string.Equals(item.Name, scene.Name, StringComparison.OrdinalIgnoreCase));
        LoadSelectedSceneIntoEditor();
        SetStatus($"已用当前方案更新场景：{scene.Name}");
    }

    private void SaveCurrentAsScene()
    {
        var name = AppDialog.ShowInput(this, "保存场景", "请输入场景名称。", GetUniqueSceneName("自定义场景"));
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        name = name.Trim();
        if (ProfileStore.IsBuiltInSceneName(name))
        {
            AppDialog.ShowInfo(this, "保存场景", "这个名称属于内置场景，请换一个名称。内置场景不能覆盖，也不能删除。");
            return;
        }

        var scene = new DesktopScene
        {
            Name = name,
            IsBuiltIn = false,
            ProfileName = _currentProfile.Name,
            LayoutMode = _currentProfile.LayoutMode,
            SortMode = _currentProfile.SortMode,
            ExcludeSystemIcons = _currentProfile.ExcludeSystemIcons,
            UseDesktopZones = _currentProfile.LayoutMode == LayoutMode.DesktopZones,
            CustomPattern = ClonePattern(_currentProfile.CustomPattern),
            DesktopZones = _currentProfile.DesktopZones.Select(CloneZone).ToList(),
            ExcludedIconKeys = [.. _currentProfile.ExcludedIconKeys],
        };
        _profileStore.Scenes.Add(scene);
        ProfileStore.Save(_profileStore);
        ReloadSceneCombo();
        _sceneComboBox.SelectedItem = scene;
        SetStatus($"已保存场景：{scene.Name}");
    }

    private void DeleteSelectedScene()
    {
        if (_sceneComboBox.SelectedItem is not DesktopScene scene)
        {
            return;
        }

        if (scene.IsBuiltIn || ProfileStore.IsBuiltInSceneName(scene.Name))
        {
            AppDialog.ShowInfo(this, "删除场景", $"“{scene.Name}”是内置场景，不能删除。只有你自己保存的新场景可以删除。");
            return;
        }

        if (AppDialog.ShowConfirm(this, "删除场景", $"确认删除场景“{scene.Name}”？", null, "删除", "取消", DialogKind.Warning) != AppDialogResult.Primary)
        {
            return;
        }

        _profileStore.Scenes.Remove(scene);
        ProfileStore.Save(_profileStore);
        ReloadSceneCombo();
        SetStatus($"已删除场景：{scene.Name}");
    }

    private string GetUniqueSceneName(string prefix)
    {
        var index = 1;
        while (_profileStore.Scenes.Any(scene => string.Equals(scene.Name, $"{prefix} {index}", StringComparison.OrdinalIgnoreCase)))
        {
            index++;
        }

        return $"{prefix} {index}";
    }

    private static CustomPatternDefinition ClonePattern(CustomPatternDefinition pattern)
    {
        return new CustomPatternDefinition
        {
            PatternKind = pattern.PatternKind,
            FillMode = pattern.FillMode,
            CenterXPercent = pattern.CenterXPercent,
            CenterYPercent = pattern.CenterYPercent,
            WidthPercent = pattern.WidthPercent,
            HeightPercent = pattern.HeightPercent,
            RotationDegrees = pattern.RotationDegrees,
            PointSpacing = pattern.PointSpacing,
            SnapToGrid = pattern.SnapToGrid,
            OverflowToGrid = pattern.OverflowToGrid,
            Text = pattern.Text,
            ImageMaskPath = pattern.ImageMaskPath,
            ManualPoints = pattern.ManualPoints.Select(point => new PatternPoint
            {
                XPercent = point.XPercent,
                YPercent = point.YPercent,
            }).ToList(),
        };
    }

    private static DesktopZone CloneZone(DesktopZone zone)
    {
        return new DesktopZone
        {
            Name = zone.Name,
            IsBuiltIn = zone.IsBuiltIn,
            XPercent = zone.XPercent,
            YPercent = zone.YPercent,
            WidthPercent = zone.WidthPercent,
            HeightPercent = zone.HeightPercent,
            ColorArgb = zone.ColorArgb,
            LayoutMode = zone.LayoutMode,
            SortMode = zone.SortMode,
            Categories = [.. zone.Categories],
            NameContains = [.. zone.NameContains],
            IconKeys = [.. zone.IconKeys],
        };
    }

    private void AnalyzeHealth()
    {
        var report = DesktopHealthAnalyzer.Analyze(_desktopIcons);
        _healthSummaryLabel.Text = $"桌面图标 {report.IconCount} 个，较大文件 {report.LargeFiles.Count} 个，失效快捷方式 {report.BrokenShortcuts.Count} 个。";
        _healthListView.BeginUpdate();
        _suggestionListBox.BeginUpdate();
        try
        {
            _healthListView.Items.Clear();
            foreach (var item in report.RecentFiles.Concat(report.LargeFiles).Concat(report.BrokenShortcuts).DistinctBy(item => item.Path))
            {
                var row = new ListViewItem(item.Name);
                row.SubItems.Add(item.Category.ToDisplayText());
                row.SubItems.Add(item.Details);
                _healthListView.Items.Add(row);
            }

            _suggestionListBox.Items.Clear();
            foreach (var suggestion in report.Suggestions)
            {
                _suggestionListBox.Items.Add(suggestion);
            }
        }
        finally
        {
            _healthListView.EndUpdate();
            _suggestionListBox.EndUpdate();
        }
    }

    private void RefreshFileOrganizeSuggestions()
    {
        try
        {
            _fileOrganizeSuggestions = FileOrganizer.GenerateSuggestions(_desktopIcons, _appSettings);
            _fileOrganizeListView.BeginUpdate();
            try
            {
                _fileOrganizeListView.Items.Clear();
                foreach (var suggestion in _fileOrganizeSuggestions)
                {
                    var item = new ListViewItem(string.Empty)
                    {
                        Checked = suggestion.DefaultSelected,
                        Tag = suggestion,
                    };
                    item.SubItems.Add(suggestion.FileName);
                    item.SubItems.Add(GetTargetKindDisplayName(suggestion.TargetKind));
                    item.SubItems.Add(DesktopHealthAnalyzer.FormatSize(suggestion.SizeBytes));
                    item.SubItems.Add(ShortPath(suggestion.SuggestedTargetPath));
                    item.SubItems.Add(suggestion.Reason);
                    _fileOrganizeListView.Items.Add(item);
                }
            }
            finally
            {
                _fileOrganizeListView.EndUpdate();
            }

            var selected = _fileOrganizeSuggestions.Count(item => item.DefaultSelected);
            _fileOrganizeSummaryLabel.Text =
                $"发现 {_fileOrganizeSuggestions.Count} 条可收纳建议，默认勾选 {selected} 条。执行前会再次确认并生成撤销记录。";
            UpdateFileOrganizeKindChoices();
        }
        catch (Exception ex)
        {
            _fileOrganizeSummaryLabel.Text = $"生成文件收纳建议失败：{ex.Message}";
            AppLogger.Log("生成文件收纳建议失败。", ex);
        }
    }

    private void SetFileOrganizeChecks(bool recommendedOnly, bool clearAll = false)
    {
        foreach (ListViewItem? item in _fileOrganizeListView.Items)
        {
            if (item?.Tag is not FileOrganizeSuggestion suggestion)
            {
                continue;
            }

            item.Checked = clearAll ? false : !recommendedOnly || suggestion.DefaultSelected;
        }

        UpdateFileOrganizeSelectionSummary();
    }

    private void SelectFileOrganizeKindOnly()
    {
        if (_fileOrganizeKindComboBox.SelectedItem is not FileOrganizeKindChoice choice)
        {
            return;
        }

        var selectedCount = 0;
        foreach (ListViewItem? item in _fileOrganizeListView.Items)
        {
            if (item?.Tag is not FileOrganizeSuggestion suggestion)
            {
                continue;
            }

            item.Checked = suggestion.TargetKind == choice.Kind;
            if (item.Checked)
            {
                selectedCount++;
            }
        }

        UpdateFileOrganizeSelectionSummary();
        SetStatus($"已只勾选 {choice.DisplayName} 类：{selectedCount} 个文件。");
    }

    private void UpdateFileOrganizeKindChoices()
    {
        if (_fileOrganizeKindComboBox.Items.Count == 0)
        {
            return;
        }

        var currentKind = (_fileOrganizeKindComboBox.SelectedItem as FileOrganizeKindChoice)?.Kind
            ?? FileOrganizeTargetKind.Image;
        _fileOrganizeKindComboBox.BeginUpdate();
        try
        {
            _fileOrganizeKindComboBox.Items.Clear();
            foreach (var kind in new[]
                     {
                         FileOrganizeTargetKind.Image,
                         FileOrganizeTargetKind.Document,
                         FileOrganizeTargetKind.Media,
                         FileOrganizeTargetKind.ArchiveOrInstaller,
                     })
            {
                var count = _fileOrganizeSuggestions.Count(item => item.TargetKind == kind);
                _fileOrganizeKindComboBox.Items.Add(new FileOrganizeKindChoice(kind, $"{GetTargetKindDisplayName(kind)} ({count})"));
            }

            var selected = _fileOrganizeKindComboBox.Items
                .Cast<FileOrganizeKindChoice>()
                .FirstOrDefault(item => item.Kind == currentKind)
                ?? _fileOrganizeKindComboBox.Items.Cast<FileOrganizeKindChoice>().FirstOrDefault();
            if (selected is not null)
            {
                _fileOrganizeKindComboBox.SelectedItem = selected;
            }
        }
        finally
        {
            _fileOrganizeKindComboBox.EndUpdate();
        }
    }

    private void UpdateFileOrganizeSelectionSummary()
    {
        var checkedItems = _fileOrganizeListView.Items
            .Cast<ListViewItem>()
            .Where(item => item.Checked && item.Tag is FileOrganizeSuggestion)
            .Select(item => (FileOrganizeSuggestion)item.Tag!)
            .ToList();

        if (checkedItems.Count == 0)
        {
            SetStatus("文件收纳：当前未勾选任何文件。");
            return;
        }

        var groups = checkedItems
            .GroupBy(item => item.TargetKind)
            .OrderBy(group => group.Key)
            .Select(group => $"{GetTargetKindDisplayName(group.Key)} {group.Count()} 个");
        SetStatus($"文件收纳：已勾选 {checkedItems.Count} 个（{string.Join("，", groups)}）。");
    }

    private void ExecuteFileOrganize()
    {
        var selected = _fileOrganizeListView.Items
            .Cast<ListViewItem>()
            .Where(item => item.Checked && item.Tag is FileOrganizeSuggestion)
            .Select(item => (FileOrganizeSuggestion)item.Tag!)
            .ToList();

        if (selected.Count == 0)
        {
            AppDialog.ShowInfo(this, "文件收纳", "请先勾选需要收纳的文件。其他类型默认不会勾选。");
            return;
        }

        var targetFolders = selected
            .Select(item => Path.GetDirectoryName(item.SuggestedTargetPath) ?? string.Empty)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(ShortPath)
            .ToList();
        var rows = new List<DialogSummaryRow>
        {
            new() { Label = "将移动文件", Value = $"{selected.Count} 个" },
            new() { Label = "目标文件夹", Value = string.Join("；", targetFolders.Take(4)) + (targetFolders.Count > 4 ? " 等" : string.Empty) },
            new() { Label = "撤销记录", Value = "会生成，可撤销仍在目标位置的文件" },
            new() { Label = "覆盖策略", Value = "遇到同名文件会跳过，不覆盖、不删除" },
        };

        var detail = string.Join(Environment.NewLine, selected.Select(item =>
            $"{item.FileName} -> {ShortPath(item.SuggestedTargetPath)}"));
        var confirm = AppDialog.ShowConfirm(
            this,
            "确认执行文件收纳",
            "这一步会移动真实桌面文件。不会删除文件，不会重命名文件；目标文件夹不存在时会自动创建。",
            rows,
            "执行收纳",
            "取消",
            DialogKind.Warning,
            detail);
        if (confirm != AppDialogResult.Primary)
        {
            SetStatus("已取消文件收纳。");
            return;
        }

        SetBusy(true);
        try
        {
            var operation = FileOrganizer.Execute(selected, _appSettings);
            var success = operation.Results.Count(result => result.Success);
            var failed = operation.Results.Count - success;
            AppDialog.ShowInfo(this, "文件收纳完成", $"已移动 {success} 个文件，跳过或失败 {failed} 个。", BuildMoveResultRows(operation));
            RefreshDesktopIcons();
            SetStatus($"文件收纳完成：已移动 {success} 个，跳过或失败 {failed} 个。");
        }
        catch (Exception ex)
        {
            AppDialog.ShowError(this, "文件收纳失败", ex.Message, ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void UndoLastFileOrganize()
    {
        var latest = FileOrganizeOperationStore.GetLatestUndoable();
        if (latest is null)
        {
            AppDialog.ShowInfo(this, "撤销文件收纳", "没有可撤销的文件收纳记录。");
            return;
        }

        var rows = new[]
        {
            new DialogSummaryRow { Label = "收纳时间", Value = latest.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss") },
            new DialogSummaryRow { Label = "可尝试恢复", Value = $"{latest.Results.Count(result => result.Success)} 个文件" },
            new DialogSummaryRow { Label = "冲突处理", Value = "原位置已有同名文件时跳过，不覆盖" },
        };
        if (AppDialog.ShowConfirm(this, "撤销上次文件收纳", "只会把仍存在于目标位置的文件移回原位置。", rows, "撤销", "取消", DialogKind.Warning) != AppDialogResult.Primary)
        {
            return;
        }

        SetBusy(true);
        try
        {
            var undo = FileOrganizer.UndoLatest(_appSettings);
            if (undo is null)
            {
                AppDialog.ShowInfo(this, "撤销文件收纳", "没有可撤销的文件收纳记录。");
                return;
            }

            var success = undo.Results.Count(result => result.Success);
            var failed = undo.Results.Count - success;
            AppDialog.ShowInfo(this, "撤销完成", $"已移回 {success} 个文件，跳过或失败 {failed} 个。", BuildMoveResultRows(undo));
            RefreshDesktopIcons();
            SetStatus($"撤销文件收纳完成：已移回 {success} 个，跳过或失败 {failed} 个。");
        }
        catch (Exception ex)
        {
            AppDialog.ShowError(this, "撤销文件收纳失败", ex.Message, ex);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static IEnumerable<DialogSummaryRow> BuildMoveResultRows(FileOrganizeOperation operation)
    {
        return operation.Results
            .GroupBy(result => string.IsNullOrWhiteSpace(result.Status) ? "未知" : result.Status)
            .Select(group => new DialogSummaryRow { Label = group.Key, Value = $"{group.Count()} 个" })
            .ToList();
    }

    private static string GetTargetKindDisplayName(FileOrganizeTargetKind kind) => kind switch
    {
        FileOrganizeTargetKind.Document => "文档",
        FileOrganizeTargetKind.Image => "图片",
        FileOrganizeTargetKind.Media => "视频音频",
        FileOrganizeTargetKind.ArchiveOrInstaller => "压缩包/安装包",
        FileOrganizeTargetKind.Other => "其他",
        _ => kind.ToString(),
    };

    private sealed record FileOrganizeKindChoice(FileOrganizeTargetKind Kind, string DisplayName)
    {
        public override string ToString() => DisplayName;
    }

    private void SetVisibleIconChecks(bool value)
    {
        _suppressIconCheckedEvents = true;
        try
        {
            foreach (ListViewItem? item in _iconListView.Items)
            {
                if (item is not null)
                {
                    item.Checked = value;
                }
            }
        }
        finally
        {
            _suppressIconCheckedEvents = false;
        }

        UpdateExcludedKeysFromList();
        PersistCurrentProfileSettings(updateStartup: false);
        UpdatePreviewText();
    }

    private void InvertVisibleIconChecks()
    {
        _suppressIconCheckedEvents = true;
        try
        {
            foreach (ListViewItem? item in _iconListView.Items)
            {
                if (item is not null)
                {
                    item.Checked = !item.Checked;
                }
            }
        }
        finally
        {
            _suppressIconCheckedEvents = false;
        }

        UpdateExcludedKeysFromList();
        PersistCurrentProfileSettings(updateStartup: false);
        UpdatePreviewText();
    }

    private void ExcludeSystemIconsInList()
    {
        _suppressIconCheckedEvents = true;
        try
        {
            foreach (ListViewItem? item in _iconListView.Items)
            {
                if (item?.Tag is DesktopIconInfo { Category: IconCategory.System })
                {
                    item.Checked = true;
                }
            }
        }
        finally
        {
            _suppressIconCheckedEvents = false;
        }

        UpdateExcludedKeysFromList();
        PersistCurrentProfileSettings(updateStartup: false);
        UpdatePreviewText();
    }

    private string GetUniqueProfileName(string prefix)
    {
        var index = 1;
        while (_profileStore.Profiles.Any(profile => string.Equals(profile.Name, $"{prefix} {index}", StringComparison.OrdinalIgnoreCase)))
        {
            index++;
        }

        return $"{prefix} {index}";
    }

    private void UpdateSpacingControls()
    {
        var enabled = !_useDesktopSpacingCheckBox.Checked;
        _columnSpacingBox.Enabled = enabled;
        _rowSpacingBox.Enabled = enabled;
    }

    private void UpdatePatternControlsVisibility()
    {
        var enabled = _layoutComboBox.SelectedItem is LayoutMode.CustomPattern;
        _patternKindComboBox.Enabled = enabled;
        _patternFillComboBox.Enabled = enabled;
        _patternTextBox.Enabled = enabled;
        _imageMaskPathBox.Enabled = enabled;
        _patternCenterXBox.Enabled = enabled;
        _patternCenterYBox.Enabled = enabled;
        _patternWidthBox.Enabled = enabled;
        _patternHeightBox.Enabled = enabled;
        _patternRotationBox.Enabled = enabled;
        _patternSpacingBox.Enabled = enabled;
    }

    private void ChooseImageMask()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "选择图片蒙版",
            Filter = "图片文件 (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件 (*.*)|*.*",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        _imageMaskPathBox.Text = dialog.FileName;
        _patternKindComboBox.SelectedItem = PatternKind.ImageMask;
        UpdateCurrentProfileFromControls();
        CalculatePreview();
    }

    private void RecordManualPatternFromDesktop()
    {
        if (_desktopIcons.Count == 0)
        {
            AppDialog.ShowInfo(this, "记录手动点位", "当前还没有读取到桌面图标。请先点击“刷新”。");
            return;
        }

        var screen = DesktopIconArranger.FindTargetScreen(_currentProfile.TargetScreenDeviceName);
        var workArea = screen.WorkingArea;
        _currentProfile.CustomPattern.ManualPoints = _desktopIcons
            .OrderBy(icon => icon.Index)
            .Select(icon => new PatternPoint
            {
                XPercent = Math.Clamp((icon.Position.X - workArea.Left) * 100 / Math.Max(1, workArea.Width), 0, 100),
                YPercent = Math.Clamp((icon.Position.Y - workArea.Top) * 100 / Math.Max(1, workArea.Height), 0, 100),
            })
            .ToList();
        _currentProfile.CustomPattern.PatternKind = PatternKind.ManualPoints;
        _layoutComboBox.SelectedItem = LayoutMode.CustomPattern;
        _patternKindComboBox.SelectedItem = PatternKind.ManualPoints;
        _manualPointCountLabel.Text = $"手动点位：{_currentProfile.CustomPattern.ManualPoints.Count} 个";
        CalculatePreview();
        PersistCurrentProfileSettings(updateStartup: false);
        SetStatus("已把当前桌面位置记录为手动图案点位。");
    }

    private void ClearManualPatternPoints()
    {
        _currentProfile.CustomPattern.ManualPoints.Clear();
        _manualPointCountLabel.Text = "手动点位：0 个";
        CalculatePreview();
        PersistCurrentProfileSettings(updateStartup: false);
        SetStatus("已清空手动图案点位。");
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        foreach (var button in EnumerateControls<Button>(this))
        {
            button.Enabled = !busy;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SaveCurrentUiStateBeforeClose();
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _desktopRefreshTimer.Stop();
        _desktopRefreshTimer.Dispose();
        base.OnFormClosed(e);
    }

    private void SaveCurrentUiStateBeforeClose()
    {
        if (_loading)
        {
            return;
        }

        try
        {
            UpdateExcludedKeysFromList();
            UpdateCurrentProfileFromControlsSilently();
            SaveAppSettingsFromControls();
            _profileStore.DefaultProfileName = _currentProfile.Name;
            ProfileStore.Save(_profileStore);
            SyncStartupShortcutFromProfile();
        }
        catch (Exception ex)
        {
            AppLogger.Log("关闭窗口前保存设置失败。", ex);
        }
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = text;
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.F5)
        {
            RefreshDesktopIcons();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.S)
        {
            SaveCurrentProfile();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.P)
        {
            CalculatePreview();
            e.Handled = true;
        }
        else if (e.Control && e.KeyCode == Keys.Enter)
        {
            ApplyLayoutWithConfirmation();
            e.Handled = true;
        }
    }

    private void CopyDiagnostics()
    {
        var diagnostics =
            $"DesktopIconManager\r\n" +
            $"Version: {UpdateChecker.GetCurrentVersionInfo().Version}\r\n" +
            $"OS: {Environment.OSVersion}\r\n" +
            $"Screens: {Screen.AllScreens.Length}\r\n" +
            $"AppData: {AppPaths.AppDataDirectory}\r\n" +
            $"Profiles: {_profileStore.Profiles.Count}\r\n" +
            $"Icons: {_desktopIcons.Count}\r\n" +
            $"Startup: {StartupManager.IsEnabled()}";
        Clipboard.SetText(diagnostics);
        SetStatus("已复制诊断信息。");
    }

    private void ApplyAppSettingsToControls()
    {
        _checkUpdatesOnStartupCheckBox.Checked = _appSettings.CheckUpdatesOnStartup;
        _diagnosticsFullPathsCheckBox.Checked = _appSettings.DiagnosticsIncludeFullPaths;
        _updateManifestUrlBox.Text = _appSettings.UpdateManifestUrl;
        _fileOrganizeRetentionBox.Value = ClampForBox(_fileOrganizeRetentionBox, _appSettings.FileOrganizeUndoRetentionCount);
        var version = UpdateChecker.GetCurrentVersionInfo();
        _versionLabel.Text =
            $"当前版本：{version.Version} | 渠道：{version.ReleaseChannel} | " +
            $"构建时间：{version.BuildTime:yyyy-MM-dd HH:mm}";
    }

    private void SaveAppSettingsFromControls()
    {
        if (_loading)
        {
            return;
        }

        _appSettings.CheckUpdatesOnStartup = _checkUpdatesOnStartupCheckBox.Checked;
        _appSettings.DiagnosticsIncludeFullPaths = _diagnosticsFullPathsCheckBox.Checked;
        _appSettings.UpdateManifestUrl = _updateManifestUrlBox.Text.Trim();
        _appSettings.FileOrganizeUndoRetentionCount = (int)_fileOrganizeRetentionBox.Value;
        AppSettingsStore.Save(_appSettings);
    }

    private void SyncStartupShortcutFromProfile()
    {
        try
        {
            StartupManager.SetEnabled(_currentProfile.StartupEnabled, _currentProfile.Name);
        }
        catch (Exception ex)
        {
            AppLogger.Log("同步开机自动整理快捷方式失败。", ex);
            SetStatus($"开机自动整理同步失败：{ex.Message}");
        }
    }

    private void RunStartupUiTasks()
    {
        if (!_appSettings.FirstRunGuideCompleted)
        {
            ShowFirstRunGuide();
        }

        if (_appSettings.CheckUpdatesOnStartup)
        {
            _ = CheckForUpdatesAsync(manual: false);
        }
    }

    private void ShowFirstRunGuide()
    {
        var rows = new[]
        {
            new DialogSummaryRow { Label = "默认行为", Value = "只整理桌面图标坐标" },
            new DialogSummaryRow { Label = "文件收纳", Value = "独立页面，二次确认后才移动真实文件" },
            new DialogSummaryRow { Label = "建议流程", Value = "先预览，再应用；应用前自动保存快照" },
        };
        AppDialog.ShowInfo(this, "首次使用说明", "桌面管理器不会默认删除、移动或重命名真实文件。普通“应用布局”只改变桌面图标位置。", rows);
        _appSettings.FirstRunGuideCompleted = true;
        AppSettingsStore.Save(_appSettings);
    }

    private void ShowSafetyGuide()
    {
        var rows = new[]
        {
            new DialogSummaryRow { Label = "布局整理", Value = "只改桌面图标坐标，应用前保存快照" },
            new DialogSummaryRow { Label = "中断恢复", Value = "安全整理开启时会记录整理前状态" },
            new DialogSummaryRow { Label = "文件收纳", Value = "只在独立页面勾选并二次确认后移动文件" },
            new DialogSummaryRow { Label = "撤销边界", Value = "原位置或目标位置发生冲突时不会覆盖" },
        };
        AppDialog.ShowInfo(this, "安全模式说明", "这些机制用于降低误操作风险，但文件收纳属于真实文件移动，执行前请确认目标路径。", rows);
    }

    private async Task CheckForUpdatesAsync(bool manual)
    {
        SaveAppSettingsFromControls();
        try
        {
            SetStatus("正在检查更新...");
            var result = await UpdateChecker.CheckAsync(_appSettings.UpdateManifestUrl);
            _appSettings.LastUpdateCheckAt = DateTime.Now;
            AppSettingsStore.Save(_appSettings);

            if (!result.IsConfigured)
            {
                if (manual)
                {
                    AppDialog.ShowInfo(this, "检查更新", result.Message);
                }

                SetStatus("未配置更新清单地址。");
                return;
            }

            if (!result.HasUpdate)
            {
                if (manual)
                {
                    AppDialog.ShowInfo(this, "检查更新", result.Message);
                }

                SetStatus(result.Message);
                return;
            }

            var manifest = result.Manifest!;
            var rows = new List<DialogSummaryRow>
            {
                new() { Label = "当前版本", Value = result.CurrentVersion },
                new() { Label = "最新版本", Value = manifest.Version },
                new() { Label = "发布日期", Value = manifest.ReleaseDate?.ToString("yyyy-MM-dd") ?? "未提供" },
                new() { Label = "清单来源", Value = string.IsNullOrWhiteSpace(result.ManifestSourceUrl) ? _appSettings.UpdateManifestUrl : result.ManifestSourceUrl },
                new() { Label = "安装包", Value = string.IsNullOrWhiteSpace(manifest.DownloadUrl) ? "未提供" : manifest.DownloadUrl },
            };
            var notes = manifest.Notes.Count == 0 ? "更新清单未提供说明。" : string.Join("\r\n", manifest.Notes.Select(note => $"• {note}"));
            var choice = AppDialog.ShowConfirm(this, "发现新版本", "应用不会自动下载或安装更新。你可以复制下载链接或打开下载页面。", rows, "打开下载页面", "复制下载链接", DialogKind.Info, notes, secondaryIsCancel: false);
            if (choice == AppDialogResult.Primary)
            {
                OpenDownloadUrl(manifest.DownloadUrl);
            }
            else if (choice == AppDialogResult.Secondary && !string.IsNullOrWhiteSpace(manifest.DownloadUrl))
            {
                Clipboard.SetText(manifest.DownloadUrl);
                SetStatus("已复制更新下载链接。");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log("检查更新失败。", ex);
            var friendlyMessage = UpdateChecker.BuildUserFriendlyFailureMessage(ex, _appSettings.UpdateManifestUrl);
            var details = UpdateChecker.BuildDiagnosticDetails(ex, _appSettings.UpdateManifestUrl);
            if (manual)
            {
                AppDialog.ShowError(this, "检查更新失败", friendlyMessage, new Exception(details, ex));
            }
            SetStatus("检查更新失败：无法读取更新清单，详情请看弹窗或日志。");
        }
    }

    private void OpenDownloadUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            AppDialog.ShowInfo(this, "下载链接", "更新清单没有提供下载地址。");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            AppDialog.ShowError(this, "打开下载页面失败", ex.Message, ex);
        }
    }

    private void ExportDiagnosticPackage()
    {
        SaveAppSettingsFromControls();
        if (_appSettings.DiagnosticsIncludeFullPaths)
        {
            var risk = AppDialog.ShowConfirm(
                this,
                "确认包含完整路径",
                "完整路径可能包含你的用户名、桌面文件名和文件夹结构。只在你明确需要排查问题时开启。",
                null,
                "继续导出",
                "取消",
                DialogKind.Warning);
            if (risk != AppDialogResult.Primary)
            {
                return;
            }
        }

        using var dialog = new SaveFileDialog
        {
            Title = "导出诊断包",
            Filter = "诊断包 (*.zip)|*.zip",
            FileName = $"DesktopIconManager-Diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.zip",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            var path = DiagnosticPackageBuilder.Export(dialog.FileName, _appSettings, _profileStore, _desktopIcons);
            AppDialog.ShowInfo(this, "诊断包已导出", $"诊断包已保存到：{path}");
            SetStatus("已导出诊断包。");
        }
        catch (Exception ex)
        {
            AppDialog.ShowError(this, "导出诊断包失败", ex.Message, ex);
        }
    }

    private void RestoreDefaultConfiguration()
    {
        var confirm = AppDialog.ShowConfirm(
            this,
            "恢复默认配置",
            "这只会重置软件方案、场景和设置；不会删除快照、日志，也不会影响桌面真实文件。",
            null,
            "恢复默认",
            "取消",
            DialogKind.Warning);
        if (confirm != AppDialogResult.Primary)
        {
            return;
        }

        _loading = true;
        _suppressAutoSave = true;
        try
        {
            _profileStore = new ProfileStoreData
            {
                DefaultProfileName = "默认方案",
                Profiles = [ProfileStore.CreateDefaultProfile()],
                Scenes = ProfileStore.CreateDefaultScenes(),
            };
            ProfileStore.Save(_profileStore);
            AppSettingsStore.Reset(preserveFirstRunGuideCompleted: true);
            _appSettings = AppSettingsStore.Load();
            ReloadProfileCombo();
            ReloadSceneCombo();
            _currentProfile = _profileStore.Profiles[0];
            _profileComboBox.SelectedItem = _currentProfile;
            ApplyProfileToControls();
            ApplyAppSettingsToControls();
        }
        finally
        {
            _suppressAutoSave = false;
            _loading = false;
        }

        PopulateIconList();
        PopulateZones();
        RefreshFileOrganizeSuggestions();
        CalculatePreview();
        SetStatus("已恢复默认配置，快照和真实文件未被修改。");
    }

    private static void OpenPath(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
            }
            else
            {
                Directory.CreateDirectory(path);
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            AppLogger.Log("打开路径失败。", ex);
        }
    }

    private void JumpToTab(string title)
    {
        if (_mainTabs is null)
        {
            return;
        }

        foreach (TabPage page in _mainTabs.TabPages)
        {
            if (string.Equals(page.Text, title, StringComparison.OrdinalIgnoreCase))
            {
                _mainTabs.SelectedTab = page;
                return;
            }
        }
    }

    private static string ShortPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var desktop = FileOrganizer.GetDesktopDirectory();
        if (path.StartsWith(desktop, StringComparison.OrdinalIgnoreCase))
        {
            var relative = path[desktop.Length..].TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.IsNullOrWhiteSpace(relative) ? "桌面" : $"桌面\\{relative}";
        }

        return path;
    }

    private static Panel CreateSectionPanel(string title)
    {
        var panel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            FillColor = UiColors.Surface,
            BorderColor = UiColors.Border,
            HeaderText = title,
            Padding = new Padding(18, 50, 18, 18),
            Margin = new Padding(0, 0, 12, 0),
            AutoScroll = true,
        };
        return panel;
    }

    private static Panel CreateSubSection(string title)
    {
        var panel = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Height = 270,
            FillColor = UiColors.SurfaceAlt,
            BorderColor = UiColors.Border,
            HeaderText = title,
            Radius = UiTheme.SmallRadius,
            Padding = new Padding(14, 40, 14, 14),
            Margin = new Padding(0, 10, 0, 0),
            AutoScroll = true,
        };
        return panel;
    }

    private static TableLayoutPanel CreateSettingsGrid(int rows, int labelWidth)
    {
        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = rows,
            BackColor = UiColors.Surface,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < rows; i++)
        {
            grid.RowStyles.Add(new RowStyle(SizeType.Absolute, i == rows - 1 ? 112 : 40));
        }

        return grid;
    }

    private static Control LabeledControl(string label, Control control)
    {
        var panel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, RowCount = 2, Padding = new Padding(0, 0, 8, 0), BackColor = Color.Transparent };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = UiColors.MutedText }, 0, 0);
        panel.Controls.Add(control, 0, 1);
        return panel;
    }

    private static void AddRow(TableLayoutPanel grid, string label, Control control, int row)
    {
        grid.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = UiColors.MutedText,
            Margin = new Padding(0, 0, 10, 0),
        }, 0, row);

        grid.Controls.Add(control, 1, row);
    }

    private static void ConfigureComboBox(ComboBox comboBox)
    {
        UiTheme.StyleComboBox(comboBox);
        comboBox.Dock = DockStyle.Fill;
        comboBox.MinimumSize = new Size(96, 30);
    }

    private void ConfigureProfileEditorCheckBox(CheckBox checkBox, string text)
    {
        checkBox.Text = text;
        checkBox.AutoSize = true;
        UiTheme.StyleCheckBox(checkBox);
        checkBox.CheckedChanged += (_, _) => MarkProfileEditorDirty();
    }

    private static void ConfigureNumberBox(NumericUpDown box, int minimum, int maximum, int value, bool autoUpdateProfile = true)
    {
        box.Minimum = minimum;
        box.Maximum = maximum;
        box.Value = Math.Clamp(value, minimum, maximum);
        box.Increment = 2;
        box.Dock = DockStyle.Left;
        box.Width = 92;
        box.TextAlign = HorizontalAlignment.Right;
        UiTheme.StyleNumberBox(box);
        box.ValueChanged += (_, _) =>
        {
            if (autoUpdateProfile && box.FindForm() is Form1 form)
            {
                form.UpdateCurrentProfileFromControls();
            }
        };
    }

    private static decimal ClampForBox(NumericUpDown box, int value)
    {
        return Math.Min(box.Maximum, Math.Max(box.Minimum, value));
    }

    private static void ConfigureActionButton(Button button, string text, bool primary, int width = 104)
    {
        button.Text = text;
        UiTheme.StyleButton(button, primary, width, 38);
        button.Margin = new Padding(10, 0, 0, 0);
    }

    private static Button CreateButton(string text, bool primary, int width)
    {
        var button = new Button();
        ConfigureActionButton(button, text, primary, width);
        button.Margin = new Padding(0, 0, 8, 8);
        button.Height = 34;
        return button;
    }

    private static void ConfigureSmallButton(Button button, string text)
    {
        button.Text = text;
        UiTheme.StyleButton(button, primary: false, width: 82, height: 34);
        button.Margin = new Padding(0, 0, 8, 8);
    }

    private static IEnumerable<T> EnumerateControls<T>(Control root)
        where T : Control
    {
        foreach (Control child in root.Controls)
        {
            if (child is T typed)
            {
                yield return typed;
            }

            foreach (var descendant in EnumerateControls<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class ScreenChoice(Screen screen)
    {
        public Screen Screen { get; } = screen;

        public override string ToString() => DesktopIconArranger.DescribeScreen(Screen);
    }

    private static class UiColors
    {
        public static readonly Color Background = UiTheme.AppBackground;
        public static readonly Color Surface = UiTheme.Surface;
        public static readonly Color SurfaceAlt = UiTheme.SurfaceAlt;
        public static readonly Color SurfaceSubtle = UiTheme.SurfaceSubtle;
        public static readonly Color Border = UiTheme.Border;
        public static readonly Color Text = UiTheme.Text;
        public static readonly Color MutedText = UiTheme.MutedText;
        public static readonly Color Accent = UiTheme.Accent;
    }
}
