namespace DesktopIconManager;

public enum AppDialogResult
{
    Primary,
    Secondary,
    Cancel,
}

public enum DialogKind
{
    Info,
    Confirm,
    Warning,
    Error,
    Input,
}

public sealed class DialogSummaryRow
{
    public string Label { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;
}

public static class AppDialog
{
    public static AppDialogResult ShowConfirm(
        IWin32Window? owner,
        string title,
        string message,
        IEnumerable<DialogSummaryRow>? rows = null,
        string primaryText = "确认",
        string secondaryText = "取消",
        DialogKind kind = DialogKind.Confirm,
        string? detailText = null,
        bool secondaryIsCancel = true)
    {
        using var form = new AppDialogForm(title, message, rows, primaryText, secondaryText, kind, detailText, inputDefault: null, secondaryIsCancel);
        return Show(form, owner) switch
        {
            DialogResult.OK => AppDialogResult.Primary,
            DialogResult.Yes => AppDialogResult.Secondary,
            _ => AppDialogResult.Cancel,
        };
    }

    public static void ShowInfo(
        IWin32Window? owner,
        string title,
        string message,
        IEnumerable<DialogSummaryRow>? rows = null)
    {
        using var form = new AppDialogForm(title, message, rows, "知道了", null, DialogKind.Info, detailText: null, inputDefault: null, secondaryIsCancel: true);
        Show(form, owner);
    }

    public static void ShowError(
        IWin32Window? owner,
        string title,
        string message,
        Exception? exception = null)
    {
        using var form = new AppDialogForm(title, message, null, "知道了", null, DialogKind.Error, exception?.ToString(), inputDefault: null, secondaryIsCancel: true);
        Show(form, owner);
    }

    public static string? ShowInput(
        IWin32Window? owner,
        string title,
        string message,
        string defaultValue)
    {
        using var form = new AppDialogForm(title, message, null, "确定", "取消", DialogKind.Input, detailText: null, inputDefault: defaultValue, secondaryIsCancel: true);
        return Show(form, owner) == DialogResult.OK ? form.InputText : null;
    }

    private static DialogResult Show(Form form, IWin32Window? owner)
    {
        return owner is null ? form.ShowDialog() : form.ShowDialog(owner);
    }

    private sealed class AppDialogForm : Form
    {
        private readonly TextBox? _inputBox;
        private readonly TextBox? _detailsBox;
        private readonly List<Label> _wrappingLabels = [];
        private readonly List<Control> _widthManagedControls = [];
        private readonly Control? _contentPanel;
        private readonly Control? _messageLabel;

        public string InputText => _inputBox?.Text ?? string.Empty;

        public AppDialogForm(
            string title,
            string message,
            IEnumerable<DialogSummaryRow>? rows,
            string primaryText,
            string? secondaryText,
            DialogKind kind,
            string? detailText,
            string? inputDefault,
            bool secondaryIsCancel)
        {
            Text = title;
            AutoScaleMode = AutoScaleMode.Dpi;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = UiTheme.AppBackground;
            MinimumSize = new Size(640, 430);
            Size = new Size(680, detailText is null ? 460 : 560);

            var icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (icon is not null)
            {
                Icon = icon;
            }

            var accent = UiTheme.AccentFor(kind);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                ColumnCount = 1,
                RowCount = 5,
                BackColor = UiTheme.AppBackground,
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleRow = new TableLayoutPanel
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                Margin = new Padding(0, 0, 0, 10),
            };
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36));
            titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            titleRow.Controls.Add(new AccentBadge
            {
                Width = 24,
                Height = 28,
                Margin = new Padding(0, 4, 10, 0),
                FillColor = accent,
                BadgeText = kind switch
                {
                    DialogKind.Error => "!",
                    DialogKind.Warning => "!",
                    DialogKind.Input => "i",
                    _ => "i",
                },
            }, 0, 0);
            var titleLabel = new Label
            {
                Text = title,
                AutoSize = true,
                Font = new Font(Font.FontFamily, 13F, FontStyle.Bold),
                ForeColor = UiTheme.Text,
                Dock = DockStyle.Fill,
            };
            _wrappingLabels.Add(titleLabel);
            titleRow.Controls.Add(titleLabel, 1, 0);
            root.Controls.Add(titleRow, 0, 0);

            var messageLabel = new Label
            {
                Text = message,
                AutoSize = true,
                ForeColor = UiTheme.Text,
                Margin = new Padding(0, 0, 0, 12),
            };
            _messageLabel = messageLabel;
            _wrappingLabels.Add(messageLabel);
            root.Controls.Add(messageLabel, 0, 1);

            var content = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = UiTheme.AppBackground,
                MinimumSize = new Size(0, 120),
            };
            _contentPanel = content;
            var contentStack = new FlowLayoutPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Width = 1,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = UiTheme.AppBackground,
            };
            content.Controls.Add(contentStack);

            if (rows is not null)
            {
                var summary = new RoundedPanel
                {
                    AutoSize = true,
                    FillColor = UiTheme.Surface,
                    BorderColor = UiTheme.Border,
                    Radius = UiTheme.SmallRadius,
                    Padding = new Padding(12),
                    Margin = new Padding(0, 0, 0, 10),
                };
                _widthManagedControls.Add(summary);
                var summaryGrid = new TableLayoutPanel
                {
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    ColumnCount = 2,
                    BackColor = UiTheme.Surface,
                };
                summaryGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 136));
                summaryGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                var rowIndex = 0;
                foreach (var row in rows)
                {
                    summaryGrid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    summaryGrid.Controls.Add(new Label
                    {
                        Text = row.Label,
                        AutoSize = true,
                        ForeColor = UiTheme.SubtleText,
                        Margin = new Padding(0, 3, 10, 3),
                    }, 0, rowIndex);
                    var valueLabel = new Label
                    {
                        Text = row.Value,
                        AutoSize = true,
                        ForeColor = UiTheme.Text,
                        Margin = new Padding(0, 3, 0, 3),
                    };
                    _wrappingLabels.Add(valueLabel);
                    summaryGrid.Controls.Add(valueLabel, 1, rowIndex);
                    rowIndex++;
                }

                summary.Controls.Add(summaryGrid);
                contentStack.Controls.Add(summary);
            }

            if (inputDefault is not null)
            {
                _inputBox = new TextBox
                {
                    Text = inputDefault,
                    Height = 30,
                    Margin = new Padding(0, 0, 0, 10),
                };
                UiTheme.StyleTextBox(_inputBox);
                _widthManagedControls.Add(_inputBox);
                contentStack.Controls.Add(_inputBox);
            }

            if (!string.IsNullOrWhiteSpace(detailText))
            {
                var detailsButton = CreateButton("查看详情", false, accent);
                detailsButton.Width = 96;
                detailsButton.Margin = new Padding(0, 0, 8, 8);
                var copyDetailsButton = CreateButton("复制详情", false, accent);
                copyDetailsButton.Width = 96;
                copyDetailsButton.Margin = new Padding(0, 0, 8, 8);
                _detailsBox = new TextBox
                {
                    Text = detailText,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Height = 120,
                    Visible = false,
                    BackColor = UiTheme.Surface,
                    ForeColor = UiTheme.Text,
                };
                _widthManagedControls.Add(_detailsBox);
                detailsButton.Click += (_, _) =>
                {
                    _detailsBox.Visible = !_detailsBox.Visible;
                    detailsButton.Text = _detailsBox.Visible ? "隐藏详情" : "查看详情";
                    Height = _detailsBox.Visible ? Math.Max(Height, 560) : Height;
                    UpdateResponsiveWidths();
                };
                copyDetailsButton.Click += (_, _) => Clipboard.SetText(detailText);
                var detailsButtons = new FlowLayoutPanel
                {
                    AutoSize = true,
                    FlowDirection = FlowDirection.LeftToRight,
                    WrapContents = false,
                    BackColor = UiTheme.AppBackground,
                };
                detailsButtons.Controls.AddRange([detailsButton, copyDetailsButton]);
                contentStack.Controls.Add(detailsButtons);
                contentStack.Controls.Add(_detailsBox);
            }

            root.Controls.Add(content, 0, 2);

            var safety = new Label
            {
                Text = "安全提示：普通整理只改变桌面图标位置，不删除、不移动、不重命名真实文件。",
                AutoSize = true,
                ForeColor = UiTheme.MutedText,
                Margin = new Padding(0, 10, 0, 12),
            };
            _wrappingLabels.Add(safety);
            root.Controls.Add(safety, 0, 3);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                BackColor = UiTheme.AppBackground,
            };
            var primary = CreateButton(primaryText, true, accent);
            primary.DialogResult = DialogResult.OK;
            buttons.Controls.Add(primary);

            if (secondaryText is not null)
            {
                var secondary = CreateButton(secondaryText, false, accent);
                secondary.DialogResult = secondaryIsCancel ? DialogResult.Cancel : DialogResult.Yes;
                buttons.Controls.Add(secondary);
                if (secondaryIsCancel)
                {
                    CancelButton = secondary;
                }
            }

            AcceptButton = primary;
            root.Controls.Add(buttons, 0, 4);
            Controls.Add(root);
            Shown += (_, _) => UpdateResponsiveWidths();
            Resize += (_, _) => UpdateResponsiveWidths();

            if (_inputBox is not null)
            {
                Shown += (_, _) =>
                {
                    _inputBox.Focus();
                    _inputBox.SelectAll();
                };
            }
        }

        private void UpdateResponsiveWidths()
        {
            if (IsDisposed)
            {
                return;
            }

            var available = Math.Max(320, ClientSize.Width - 64);
            foreach (var control in _widthManagedControls)
            {
                if (!control.IsDisposed)
                {
                    control.Width = available;
                }
            }

            if (_contentPanel?.Controls.Count > 0 && _contentPanel.Controls[0] is FlowLayoutPanel stack)
            {
                stack.Width = Math.Max(320, _contentPanel.ClientSize.Width - 24);
            }

            foreach (var label in _wrappingLabels)
            {
                if (label.IsDisposed)
                {
                    continue;
                }

                label.MaximumSize = new Size(Math.Max(240, available - 24), 0);
            }

            if (_messageLabel is not null)
            {
                _messageLabel.Width = available;
            }
        }

        private static Button CreateButton(string text, bool primary, Color accent)
        {
            var button = new Button
            {
                Text = text,
                Width = 94,
                Height = 34,
                Margin = new Padding(8, 0, 0, 0),
            };
            UiTheme.StyleButton(button, primary, 94, 34);
            button.BackColor = primary ? accent : UiTheme.Surface;
            button.ForeColor = primary ? Color.White : UiTheme.Text;
            button.FlatAppearance.BorderColor = primary ? accent : Color.FromArgb(190, 198, 210);
            return button;
        }
    }
}
