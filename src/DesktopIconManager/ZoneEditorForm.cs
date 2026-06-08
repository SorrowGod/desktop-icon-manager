namespace DesktopIconManager;

public sealed class ZoneEditorForm : Form
{
    private readonly DesktopZone _zone;
    private readonly TextBox _nameBox = new();
    private readonly NumericUpDown _xBox = new();
    private readonly NumericUpDown _yBox = new();
    private readonly NumericUpDown _widthBox = new();
    private readonly NumericUpDown _heightBox = new();
    private readonly ComboBox _layoutBox = new();
    private readonly ComboBox _sortBox = new();
    private readonly CheckedListBox _categoryList = new();
    private readonly TextBox _keywordBox = new();

    public ZoneEditorForm(DesktopZone zone)
    {
        _zone = zone;
        Text = "编辑分区";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        ClientSize = new Size(520, 560);
        Font = new Font("Microsoft YaHei UI", 9F);
        BackColor = UiTheme.AppBackground;

        var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (icon is not null)
        {
            Icon = icon;
        }

        BuildLayout();
        LoadZone();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22),
            ColumnCount = 2,
            RowCount = 11,
            BackColor = UiTheme.AppBackground,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 10; i++)
        {
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, i == 8 ? 118 : 38));
        }
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        ConfigureNumber(_xBox);
        ConfigureNumber(_yBox);
        ConfigureNumber(_widthBox, 5, 100);
        ConfigureNumber(_heightBox, 5, 100);
        ConfigureCombo(_layoutBox);
        _layoutBox.Items.AddRange(Enum.GetValues<LayoutMode>()
            .Where(mode => mode is not LayoutMode.DesktopZones)
            .Cast<object>()
            .ToArray());
        _layoutBox.Format += (_, e) =>
        {
            if (e.ListItem is LayoutMode mode)
            {
                e.Value = mode.ToDisplayText();
            }
        };
        ConfigureCombo(_sortBox);
        _sortBox.Items.AddRange(Enum.GetValues<SortMode>().Cast<object>().ToArray());
        _sortBox.Format += (_, e) =>
        {
            if (e.ListItem is SortMode mode)
            {
                e.Value = mode.ToDisplayText();
            }
        };

        _categoryList.CheckOnClick = true;
        _categoryList.Dock = DockStyle.Fill;
        _categoryList.BorderStyle = BorderStyle.FixedSingle;
        _categoryList.BackColor = UiTheme.Surface;
        _categoryList.ForeColor = UiTheme.Text;
        foreach (var category in Enum.GetValues<IconCategory>())
        {
            _categoryList.Items.Add(category, false);
        }
        _categoryList.Format += (_, e) =>
        {
            if (e.ListItem is IconCategory category)
            {
                e.Value = category.ToDisplayText();
            }
        };

        _nameBox.Dock = DockStyle.Fill;
        UiTheme.StyleTextBox(_nameBox);
        _keywordBox.Dock = DockStyle.Fill;
        _keywordBox.PlaceholderText = "多个关键字用逗号分隔";
        UiTheme.StyleTextBox(_keywordBox);

        AddRow(root, "名称", _nameBox, 0);
        AddRow(root, "X 位置%", _xBox, 1);
        AddRow(root, "Y 位置%", _yBox, 2);
        AddRow(root, "宽度%", _widthBox, 3);
        AddRow(root, "高度%", _heightBox, 4);
        AddRow(root, "布局", _layoutBox, 5);
        AddRow(root, "排序", _sortBox, 6);
        AddRow(root, "名称关键字", _keywordBox, 7);
        AddRow(root, "匹配类型", _categoryList, 8);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 16, 0, 0),
            BackColor = UiTheme.AppBackground,
        };
        var ok = CreateButton("确定", true);
        ok.Click += (_, _) => SaveAndClose();
        var cancel = CreateButton("取消", false);
        cancel.DialogResult = DialogResult.Cancel;
        buttons.Controls.AddRange([ok, cancel]);
        root.Controls.Add(buttons, 1, 10);
        Controls.Add(root);
        AcceptButton = ok;
        CancelButton = cancel;
    }

    private void LoadZone()
    {
        _nameBox.Text = _zone.Name;
        _xBox.Value = Clamp(_xBox, _zone.XPercent);
        _yBox.Value = Clamp(_yBox, _zone.YPercent);
        _widthBox.Value = Clamp(_widthBox, _zone.WidthPercent);
        _heightBox.Value = Clamp(_heightBox, _zone.HeightPercent);
        _layoutBox.SelectedItem = _zone.LayoutMode;
        _sortBox.SelectedItem = _zone.SortMode;
        _keywordBox.Text = string.Join(", ", _zone.NameContains);

        for (var i = 0; i < _categoryList.Items.Count; i++)
        {
            if (_categoryList.Items[i] is IconCategory category)
            {
                _categoryList.SetItemChecked(i, _zone.Categories.Contains(category));
            }
        }
    }

    private void SaveAndClose()
    {
        if (string.IsNullOrWhiteSpace(_nameBox.Text))
        {
            AppDialog.ShowInfo(this, "编辑分区", "分区名称不能为空。");
            return;
        }

        _zone.Name = _nameBox.Text.Trim();
        _zone.XPercent = (int)_xBox.Value;
        _zone.YPercent = (int)_yBox.Value;
        _zone.WidthPercent = (int)_widthBox.Value;
        _zone.HeightPercent = (int)_heightBox.Value;
        _zone.LayoutMode = _layoutBox.SelectedItem is LayoutMode layout ? layout : LayoutMode.Top;
        _zone.SortMode = _sortBox.SelectedItem is SortMode sort ? sort : SortMode.TypeThenName;
        _zone.NameContains = _keywordBox.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        _zone.Categories = _categoryList.CheckedItems
            .Cast<IconCategory>()
            .ToList();

        DialogResult = DialogResult.OK;
        Close();
    }

    private static void AddRow(TableLayoutPanel grid, string label, Control control, int row)
    {
        grid.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Color.FromArgb(86, 96, 112),
            Margin = new Padding(0, 0, 10, 0),
        }, 0, row);
        grid.Controls.Add(control, 1, row);
    }

    private static void ConfigureNumber(NumericUpDown box, int minimum = 0, int maximum = 100)
    {
        box.Minimum = minimum;
        box.Maximum = maximum;
        box.Dock = DockStyle.Left;
        box.Width = 94;
        box.TextAlign = HorizontalAlignment.Right;
        UiTheme.StyleNumberBox(box);
    }

    private static void ConfigureCombo(ComboBox comboBox)
    {
        UiTheme.StyleComboBox(comboBox);
        comboBox.Dock = DockStyle.Fill;
        comboBox.MinimumSize = new Size(96, 30);
    }

    private static decimal Clamp(NumericUpDown box, int value)
    {
        return Math.Min(box.Maximum, Math.Max(box.Minimum, value));
    }

    private static Button CreateButton(string text, bool primary)
    {
        var button = new Button
        {
            Text = text,
            Margin = new Padding(8, 0, 0, 0),
        };
        UiTheme.StyleButton(button, primary, 86, 34);
        return button;
    }
}
