namespace DesktopIconManager;

public sealed class PreviewCanvas : Control
{
    private IReadOnlyList<DesktopIconInfo> _icons = [];
    private ArrangeLayout? _layout;
    private Rectangle _workArea;
    private bool _showBefore;

    public PreviewCanvas()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Surface;
        MinimumSize = new Size(280, 180);
    }

    public void SetPreview(IReadOnlyList<DesktopIconInfo> icons, ArrangeLayout? layout, bool showBefore)
    {
        _icons = icons;
        _layout = layout;
        _showBefore = showBefore;
        _workArea = layout?.WorkArea ?? (Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1920, 1080));
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.Clear(UiTheme.Surface);
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var canvas = GetCanvasRectangle();
        DrawDesktopFrame(e.Graphics, canvas);

        if (_layout is null)
        {
            DrawCenteredText(e.Graphics, canvas, "点击“预览”后显示桌面布局");
            return;
        }

        DrawZones(e.Graphics, canvas);
        DrawIcons(e.Graphics, canvas);
        DrawLegend(e.Graphics, canvas);
    }

    private Rectangle GetCanvasRectangle()
    {
        var padding = 16;
        var available = new Rectangle(padding, padding, Math.Max(10, Width - padding * 2), Math.Max(10, Height - padding * 2 - 28));
        var aspect = _workArea.Width / (double)Math.Max(1, _workArea.Height);
        var width = available.Width;
        var height = (int)Math.Round(width / aspect);
        if (height > available.Height)
        {
            height = available.Height;
            width = (int)Math.Round(height * aspect);
        }

        return new Rectangle(
            available.Left + (available.Width - width) / 2,
            available.Top + (available.Height - height) / 2,
            width,
            height);
    }

    private void DrawZones(Graphics graphics, Rectangle canvas)
    {
        if (_layout?.Profile.LayoutMode != LayoutMode.DesktopZones)
        {
            return;
        }

        foreach (var zone in _layout.Profile.DesktopZones)
        {
            var rect = Scale(zone.ToRectangle(_workArea), canvas);
            var color = Color.FromArgb(zone.ColorArgb);
            using var brush = new SolidBrush(Color.FromArgb(32, color));
            using var pen = new Pen(Color.FromArgb(170, color), 1.4F);
            graphics.FillRectangle(brush, rect);
            graphics.DrawRectangle(pen, rect);
            using var textBrush = new SolidBrush(UiTheme.Text);
            graphics.DrawString(zone.Name, Font, textBrush, rect.Left + 6, rect.Top + 5);
        }
    }

    private void DrawIcons(Graphics graphics, Rectangle canvas)
    {
        var targetByKey = _layout?.Positions
            .GroupBy(position => position.Icon.StableKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase)
            ?? [];

        foreach (var icon in _icons)
        {
            var excluded = !targetByKey.ContainsKey(icon.StableKey);
            var point = _showBefore || excluded
                ? icon.Position
                : targetByKey[icon.StableKey].TargetPosition;
            var scaled = Scale(point, canvas);
            var color = excluded ? Color.FromArgb(150, 160, 174) : UiTheme.Accent;
            if (!_showBefore && !excluded && Distance(icon.Position, point) > 2)
            {
                var from = Scale(icon.Position, canvas);
                using var linePen = new Pen(Color.FromArgb(75, UiTheme.Accent), 1);
                graphics.DrawLine(linePen, from, scaled);
            }

            using var brush = new SolidBrush(color);
            using var outline = new Pen(Color.White, 1.2F);
            graphics.FillEllipse(brush, scaled.X - 4, scaled.Y - 4, 9, 9);
            graphics.DrawEllipse(outline, scaled.X - 4, scaled.Y - 4, 9, 9);
        }
    }

    private void DrawLegend(Graphics graphics, Rectangle canvas)
    {
        var text = _showBefore ? "整理前位置" : "整理后预览：蓝点会移动，灰点保持原位";
        using var brush = new SolidBrush(UiTheme.MutedText);
        graphics.DrawString(text, Font, brush, canvas.Left, Math.Min(Height - 22, canvas.Bottom + 8));
    }

    private void DrawCenteredText(Graphics graphics, Rectangle canvas, string text)
    {
        using var brush = new SolidBrush(UiTheme.MutedText);
        var size = graphics.MeasureString(text, Font);
        graphics.DrawString(text, Font, brush, canvas.Left + (canvas.Width - size.Width) / 2, canvas.Top + (canvas.Height - size.Height) / 2);
    }

    private static void DrawDesktopFrame(Graphics graphics, Rectangle canvas)
    {
        using var path = UiTheme.RoundedRect(canvas, 12);
        using var background = new SolidBrush(Color.FromArgb(240, 245, 252));
        using var border = new Pen(UiTheme.BorderStrong, 1.2F);
        graphics.FillPath(background, path);
        graphics.DrawPath(border, path);

        var taskbar = new Rectangle(canvas.Left + 1, canvas.Bottom - Math.Max(10, canvas.Height / 28), canvas.Width - 2, Math.Max(8, canvas.Height / 28));
        using var taskbarBrush = new SolidBrush(Color.FromArgb(222, 230, 242));
        graphics.FillRectangle(taskbarBrush, taskbar);

        using var gridPen = new Pen(Color.FromArgb(42, UiTheme.BorderStrong), 1);
        var columns = 8;
        for (var i = 1; i < columns; i++)
        {
            var x = canvas.Left + canvas.Width * i / columns;
            graphics.DrawLine(gridPen, x, canvas.Top + 1, x, taskbar.Top);
        }

        var rows = 5;
        for (var i = 1; i < rows; i++)
        {
            var y = canvas.Top + canvas.Height * i / rows;
            graphics.DrawLine(gridPen, canvas.Left + 1, y, canvas.Right - 1, y);
        }
    }

    private Rectangle Scale(Rectangle rectangle, Rectangle canvas)
    {
        var left = Scale(new Point(rectangle.Left, rectangle.Top), canvas);
        var right = Scale(new Point(rectangle.Right, rectangle.Bottom), canvas);
        return Rectangle.FromLTRB(left.X, left.Y, right.X, right.Y);
    }

    private Point Scale(Point point, Rectangle canvas)
    {
        var x = canvas.Left + (point.X - _workArea.Left) * canvas.Width / Math.Max(1, _workArea.Width);
        var y = canvas.Top + (point.Y - _workArea.Top) * canvas.Height / Math.Max(1, _workArea.Height);
        return new Point(x, y);
    }

    private static double Distance(Point a, Point b)
    {
        return Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
    }
}
