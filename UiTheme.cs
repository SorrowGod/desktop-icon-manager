using System.Drawing.Drawing2D;
using System.ComponentModel;

namespace DesktopIconManager;

internal static class UiTheme
{
    public static readonly Color AppBackground = Color.FromArgb(246, 248, 252);
    public static readonly Color Surface = Color.FromArgb(255, 255, 255);
    public static readonly Color SurfaceAlt = Color.FromArgb(250, 251, 253);
    public static readonly Color SurfaceSubtle = Color.FromArgb(243, 246, 250);
    public static readonly Color Border = Color.FromArgb(220, 226, 235);
    public static readonly Color BorderStrong = Color.FromArgb(197, 207, 220);
    public static readonly Color Text = Color.FromArgb(24, 33, 45);
    public static readonly Color MutedText = Color.FromArgb(91, 102, 117);
    public static readonly Color SubtleText = Color.FromArgb(121, 132, 148);
    public static readonly Color Accent = Color.FromArgb(0, 95, 184);
    public static readonly Color AccentHover = Color.FromArgb(0, 80, 160);
    public static readonly Color AccentSoft = Color.FromArgb(232, 242, 255);
    public static readonly Color Success = Color.FromArgb(0, 120, 96);
    public static readonly Color Warning = Color.FromArgb(180, 110, 20);
    public static readonly Color Danger = Color.FromArgb(196, 43, 43);
    public static readonly Color Shadow = Color.FromArgb(24, 39, 75);

    public const int Radius = 10;
    public const int SmallRadius = 7;

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var diameter = Math.Max(1, radius * 2);
        var path = new GraphicsPath();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static void DrawRoundedPanel(Graphics graphics, Rectangle bounds, Color fill, Color border, int radius = Radius)
    {
        using var path = RoundedRect(bounds, radius);
        using var brush = new SolidBrush(fill);
        using var pen = new Pen(border);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.FillPath(brush, path);
        graphics.DrawPath(pen, path);
    }

    public static void StyleButton(Button button, bool primary, int width, int height = 36)
    {
        button.AutoSize = false;
        button.Width = width;
        button.Height = height;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.BorderColor = primary ? Accent : BorderStrong;
        button.FlatAppearance.MouseOverBackColor = primary ? AccentHover : SurfaceSubtle;
        button.FlatAppearance.MouseDownBackColor = primary ? AccentHover : Color.FromArgb(232, 238, 247);
        button.BackColor = primary ? Accent : Surface;
        button.ForeColor = primary ? Color.White : Text;
        button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        button.UseVisualStyleBackColor = false;
        button.Cursor = Cursors.Hand;
    }

    public static void StyleComboBox(ComboBox comboBox)
    {
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.FlatStyle = FlatStyle.Standard;
        comboBox.BackColor = Surface;
        comboBox.ForeColor = Text;
        comboBox.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        comboBox.Height = 30;
        comboBox.Margin = new Padding(0, 2, 0, 8);
        comboBox.FormattingEnabled = true;
    }

    public static void StyleTextBox(TextBox textBox)
    {
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.BackColor = Surface;
        textBox.ForeColor = Text;
        textBox.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        textBox.Margin = new Padding(0, 3, 0, 7);
    }

    public static void StyleNumberBox(NumericUpDown box)
    {
        box.BorderStyle = BorderStyle.FixedSingle;
        box.BackColor = Surface;
        box.ForeColor = Text;
        box.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        box.Margin = new Padding(0, 3, 0, 7);
    }

    public static void StyleCheckBox(CheckBox checkBox)
    {
        checkBox.ForeColor = Text;
        checkBox.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        checkBox.Margin = new Padding(0, 7, 0, 7);
    }

    public static void StyleListView(ListView listView)
    {
        listView.BorderStyle = BorderStyle.FixedSingle;
        listView.BackColor = Surface;
        listView.ForeColor = Text;
        listView.GridLines = false;
        listView.FullRowSelect = true;
        listView.HideSelection = false;
        listView.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    }

    public static void StyleListBox(ListBox listBox)
    {
        listBox.BorderStyle = BorderStyle.FixedSingle;
        listBox.BackColor = Surface;
        listBox.ForeColor = Text;
        listBox.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
    }

    public static Color AccentFor(DialogKind kind) => kind switch
    {
        DialogKind.Error => Danger,
        DialogKind.Warning => Warning,
        DialogKind.Info => Success,
        _ => Accent,
    };
}

internal sealed class RoundedPanel : Panel
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int Radius { get; set; } = UiTheme.Radius;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = UiTheme.Border;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor { get; set; } = UiTheme.Surface;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string HeaderText { get; set; } = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Font? HeaderFont { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HeaderColor { get; set; } = UiTheme.Text;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int HeaderHeight { get; set; } = 44;

    public RoundedPanel()
    {
        DoubleBuffered = true;
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var bounds = ClientRectangle;
        bounds.Width -= 1;
        bounds.Height -= 1;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        UiTheme.DrawRoundedPanel(e.Graphics, bounds, FillColor, BorderColor, Radius);
        if (!string.IsNullOrWhiteSpace(HeaderText))
        {
            using var brush = new SolidBrush(HeaderColor);
            using var font = HeaderFont ?? new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            e.Graphics.DrawString(HeaderText, font, brush, Padding.Left, 14);
        }
    }
}

internal sealed class AccentBadge : Control
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FillColor { get; set; } = UiTheme.Accent;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string BadgeText { get; set; } = string.Empty;

    public AccentBadge()
    {
        DoubleBuffered = true;
        Size = new Size(34, 34);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(1, 1, Width - 3, Height - 3);
        using var path = UiTheme.RoundedRect(bounds, 9);
        using var brush = new SolidBrush(FillColor);
        e.Graphics.FillPath(brush, path);

        if (!string.IsNullOrEmpty(BadgeText))
        {
            using var textBrush = new SolidBrush(Color.White);
            using var font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
            var size = e.Graphics.MeasureString(BadgeText, font);
            e.Graphics.DrawString(BadgeText, font, textBrush, bounds.Left + (bounds.Width - size.Width) / 2, bounds.Top + (bounds.Height - size.Height) / 2);
        }
    }
}
