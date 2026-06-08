using System.Drawing.Drawing2D;

namespace DesktopIconManager;

public static class PatternLayoutEngine
{
    public static IReadOnlyList<ArrangedIconPosition> Calculate(
        ArrangeProfile profile,
        IReadOnlyList<DesktopIconInfo> orderedIcons,
        Rectangle workArea,
        Size spacing)
    {
        if (orderedIcons.Count == 0)
        {
            return [];
        }

        var pattern = profile.CustomPattern ?? new CustomPatternDefinition();
        var points = BuildPatternPoints(pattern, orderedIcons.Count, workArea, spacing);
        if (points.Count < orderedIcons.Count && pattern.OverflowToGrid)
        {
            points.AddRange(BuildOverflowGrid(points, orderedIcons.Count - points.Count, workArea, spacing));
        }

        if (points.Count == 0)
        {
            return DesktopIconArranger.CalculateBasicPositions(profile, orderedIcons, workArea, spacing);
        }

        points = OrderPoints(points, pattern.FillMode, GetPatternBounds(pattern, workArea));
        var positions = new List<ArrangedIconPosition>(orderedIcons.Count);
        for (var i = 0; i < orderedIcons.Count; i++)
        {
            positions.Add(new ArrangedIconPosition
            {
                Icon = orderedIcons[i],
                TargetPosition = ClampToWorkArea(points[Math.Min(i, points.Count - 1)], workArea, spacing),
            });
        }

        return positions;
    }

    public static List<Point> BuildPatternPoints(
        CustomPatternDefinition pattern,
        int requiredCount,
        Rectangle workArea,
        Size spacing)
    {
        requiredCount = Math.Max(1, requiredCount);
        var baseBounds = GetPatternBounds(pattern, workArea);
        var points = new List<Point>();
        var bounds = baseBounds;
        var minimumScale = EstimateScaleForCount(baseBounds, spacing, requiredCount);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            bounds = ExpandBounds(baseBounds, workArea, Math.Max(1 + (attempt * 0.16), minimumScale + (attempt * 0.08)));
            var grid = BuildGrid(bounds, workArea, spacing, pattern.PointSpacing);
            points = BuildRawPatternPoints(pattern, bounds, grid, workArea, spacing, requiredCount);
            if (points.Count >= requiredCount)
            {
                break;
            }
        }

        if (points.Count > requiredCount)
        {
            points = EvenlySample(points, requiredCount);
        }

        return points
            .Select(point => ClampToWorkArea(point, workArea, spacing))
            .Distinct()
            .ToList();
    }

    private static List<Point> BuildRawPatternPoints(
        CustomPatternDefinition pattern,
        Rectangle bounds,
        IReadOnlyList<Point> grid,
        Rectangle workArea,
        Size spacing,
        int requiredCount)
    {
        var points = pattern.PatternKind switch
        {
            PatternKind.Circle => SelectBestByScore(grid, requiredCount, point => Math.Abs(EllipseRadius(point, bounds) - 1), point => Angle(point, bounds)),
            PatternKind.Semicircle => SelectBestByScore(grid, requiredCount, point => SemicircleScore(point, bounds), point => point.X),
            PatternKind.Heart => SelectBestByScore(grid, requiredCount, point => Math.Abs(HeartEquation(point, bounds)), point => point.Y),
            PatternKind.Star => SelectByDistanceToPolyline(grid, StarVertices(bounds), closed: true, requiredCount),
            PatternKind.Wave => SelectBestByScore(grid, requiredCount, point => WaveDistance(point, bounds), point => point.X),
            PatternKind.Diagonal => SelectByDistanceToPolyline(grid, [new PointF(bounds.Left, bounds.Top), new PointF(bounds.Right, bounds.Bottom)], closed: false, requiredCount),
            PatternKind.SquareGrid => CenteredGrid(bounds, requiredCount, spacing),
            PatternKind.Ring => SelectBestByScore(grid, requiredCount, point => Math.Abs(EllipseRadius(point, bounds) - 0.78), point => Angle(point, bounds)),
            PatternKind.VShape => SelectByDistanceToPolyline(grid, [new PointF(bounds.Left, bounds.Top), new PointF(Center(bounds).X, bounds.Bottom), new PointF(bounds.Right, bounds.Top)], closed: false, requiredCount),
            PatternKind.XShape => SelectByDistanceToPolyline(grid, [new PointF(bounds.Left, bounds.Top), new PointF(bounds.Right, bounds.Bottom), new PointF(bounds.Right, bounds.Top), new PointF(bounds.Left, bounds.Bottom)], closed: false, requiredCount, breakAtMiddle: true),
            PatternKind.Text => TextPattern(pattern.Text, bounds, grid, requiredCount),
            PatternKind.ImageMask => ImageMask(pattern.ImageMaskPath, bounds, grid, requiredCount),
            PatternKind.ManualPoints => ManualPoints(pattern.ManualPoints, workArea, spacing),
            _ => SelectBestByScore(grid, requiredCount, point => Math.Abs(EllipseRadius(point, bounds) - 1), point => Angle(point, bounds)),
        };

        if (pattern.RotationDegrees != 0 && pattern.PatternKind is not PatternKind.Text and not PatternKind.ImageMask and not PatternKind.ManualPoints)
        {
            points = Rotate(points, Center(bounds), pattern.RotationDegrees)
                .Select(point => SnapToNearestGrid(point, grid))
                .Distinct()
                .ToList();
        }

        return points.Distinct().ToList();
    }

    private static Rectangle GetPatternBounds(CustomPatternDefinition pattern, Rectangle workArea)
    {
        var width = Math.Max(96, workArea.Width * Math.Clamp(pattern.WidthPercent, 10, 100) / 100);
        var height = Math.Max(96, workArea.Height * Math.Clamp(pattern.HeightPercent, 10, 100) / 100);
        var centerX = workArea.Left + workArea.Width * Math.Clamp(pattern.CenterXPercent, 0, 100) / 100;
        var centerY = workArea.Top + workArea.Height * Math.Clamp(pattern.CenterYPercent, 0, 100) / 100;
        var x = Math.Clamp(centerX - width / 2, workArea.Left, Math.Max(workArea.Left, workArea.Right - width));
        var y = Math.Clamp(centerY - height / 2, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - height));
        return new Rectangle(x, y, width, height);
    }

    private static Rectangle ExpandBounds(Rectangle baseBounds, Rectangle workArea, double scale)
    {
        var width = Math.Min(workArea.Width, Math.Max(baseBounds.Width, (int)Math.Round(baseBounds.Width * scale)));
        var height = Math.Min(workArea.Height, Math.Max(baseBounds.Height, (int)Math.Round(baseBounds.Height * scale)));
        var center = Center(baseBounds);
        var x = Math.Clamp(center.X - width / 2, workArea.Left, Math.Max(workArea.Left, workArea.Right - width));
        var y = Math.Clamp(center.Y - height / 2, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - height));
        return new Rectangle(x, y, width, height);
    }

    private static List<Point> BuildGrid(Rectangle bounds, Rectangle workArea, Size spacing, int densityPercent)
    {
        var stepX = Math.Max(48, spacing.Width);
        var stepY = Math.Max(48, spacing.Height);
        var columns = Math.Max(1, bounds.Width / stepX + 1);
        var rows = Math.Max(1, bounds.Height / stepY + 1);
        var usedWidth = Math.Max(0, (columns - 1) * stepX);
        var usedHeight = Math.Max(0, (rows - 1) * stepY);
        var points = new List<Point>();
        var startX = bounds.Left + Math.Max(0, (bounds.Width - usedWidth) / 2);
        var startY = bounds.Top + Math.Max(0, (bounds.Height - usedHeight) / 2);
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                points.Add(ClampToWorkArea(new Point(startX + column * stepX, startY + row * stepY), workArea, spacing));
            }
        }

        return points.Distinct().ToList();
    }

    private static double EstimateScaleForCount(Rectangle bounds, Size spacing, int requiredCount)
    {
        var columns = Math.Max(1, bounds.Width / Math.Max(48, spacing.Width) + 1);
        var rows = Math.Max(1, bounds.Height / Math.Max(48, spacing.Height) + 1);
        var capacity = Math.Max(1, columns * rows);
        if (capacity >= requiredCount)
        {
            return 1;
        }

        return Math.Min(2.4, Math.Sqrt(requiredCount / (double)capacity) * 1.12);
    }

    private static List<Point> SelectBestByScore(
        IReadOnlyList<Point> grid,
        int requiredCount,
        Func<Point, double> score,
        Func<Point, double> secondary)
    {
        return grid
            .OrderBy(score)
            .ThenBy(secondary)
            .Take(requiredCount)
            .ToList();
    }

    private static double EllipseRadius(Point point, Rectangle bounds)
    {
        var center = Center(bounds);
        var dx = (point.X - center.X) / Math.Max(1.0, bounds.Width / 2.0);
        var dy = (point.Y - center.Y) / Math.Max(1.0, bounds.Height / 2.0);
        return Math.Sqrt(dx * dx + dy * dy);
    }

    private static double SemicircleScore(Point point, Rectangle bounds)
    {
        var center = Center(bounds);
        var lowerHalfPenalty = point.Y > center.Y ? 8 : 0;
        return lowerHalfPenalty + Math.Abs(EllipseRadius(point, bounds) - 1);
    }

    private static double HeartEquation(Point point, Rectangle bounds)
    {
        var center = Center(bounds);
        var x = (point.X - center.X) / Math.Max(1.0, bounds.Width / 2.0) * 1.35;
        var y = -(point.Y - center.Y) / Math.Max(1.0, bounds.Height / 2.0) * 1.35;
        return Math.Pow(x * x + y * y - 1, 3) - x * x * Math.Pow(y, 3);
    }

    private static double WaveDistance(Point point, Rectangle bounds)
    {
        var progress = (point.X - bounds.Left) / (double)Math.Max(1, bounds.Width);
        var expected = bounds.Top + bounds.Height / 2.0 + Math.Sin(progress * Math.PI * 4) * bounds.Height * 0.34;
        return Math.Abs(point.Y - expected);
    }

    private static double Angle(Point point, Rectangle bounds)
    {
        var center = Center(bounds);
        return Math.Atan2(point.Y - center.Y, point.X - center.X);
    }

    private static List<PointF> StarVertices(Rectangle bounds)
    {
        var vertices = new List<PointF>();
        var center = Center(bounds);
        var outer = Math.Min(bounds.Width, bounds.Height) / 2f;
        var inner = outer * 0.45f;
        for (var i = 0; i < 10; i++)
        {
            var angle = -Math.PI / 2 + i * Math.PI / 5;
            var radius = i % 2 == 0 ? outer : inner;
            vertices.Add(new PointF(
                center.X + (float)(Math.Cos(angle) * radius),
                center.Y + (float)(Math.Sin(angle) * radius)));
        }

        return vertices;
    }

    private static List<Point> SelectByDistanceToPolyline(
        IReadOnlyList<Point> grid,
        IReadOnlyList<PointF> vertices,
        bool closed,
        int requiredCount,
        bool breakAtMiddle = false)
    {
        var segments = BuildSegments(vertices, closed, breakAtMiddle);
        return grid
            .OrderBy(point => MinPolylineDistance(point, segments))
            .ThenBy(point => MinPolylineDistanceAlong(point, segments))
            .Take(requiredCount)
            .ToList();
    }

    private static List<Point> CenteredGrid(Rectangle bounds, int requiredCount, Size spacing)
    {
        var columns = Math.Max(1, (int)Math.Ceiling(Math.Sqrt(requiredCount)));
        var rows = (int)Math.Ceiling(requiredCount / (double)columns);
        var gridWidth = (columns - 1) * spacing.Width;
        var gridHeight = (rows - 1) * spacing.Height;
        var startX = bounds.Left + Math.Max(0, (bounds.Width - gridWidth) / 2);
        var startY = bounds.Top + Math.Max(0, (bounds.Height - gridHeight) / 2);
        var points = new List<Point>(requiredCount);
        for (var i = 0; i < requiredCount; i++)
        {
            points.Add(new Point(startX + (i % columns) * spacing.Width, startY + (i / columns) * spacing.Height));
        }

        return points;
    }

    private static List<Point> TextPattern(string text, Rectangle bounds, IReadOnlyList<Point> grid, int requiredCount)
    {
        text = string.IsNullOrWhiteSpace(text) ? "WORK" : text.Trim();
        using var bitmap = new Bitmap(Math.Max(160, bounds.Width), Math.Max(90, bounds.Height));
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var font = new Font("Microsoft YaHei UI", Math.Max(24, bitmap.Height * 0.65f), FontStyle.Bold, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.Black);
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString(text, font, brush, new RectangleF(0, 0, bitmap.Width, bitmap.Height), format);
        return SampleMaskGrid(bitmap, bounds, grid, requiredCount);
    }

    private static List<Point> ImageMask(string? path, Rectangle bounds, IReadOnlyList<Point> grid, int requiredCount)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return SelectBestByScore(grid, requiredCount, point => Math.Abs(EllipseRadius(point, bounds) - 1), point => Angle(point, bounds));
        }

        try
        {
            using var bitmap = new Bitmap(path);
            return SampleMaskGrid(bitmap, bounds, grid, requiredCount);
        }
        catch
        {
            return SelectBestByScore(grid, requiredCount, point => Math.Abs(EllipseRadius(point, bounds) - 1), point => Angle(point, bounds));
        }
    }

    private static List<Point> SampleMaskGrid(Bitmap bitmap, Rectangle bounds, IReadOnlyList<Point> grid, int requiredCount)
    {
        var selected = new List<Point>();
        foreach (var point in grid)
        {
            var x = Math.Clamp((point.X - bounds.Left) * bitmap.Width / Math.Max(1, bounds.Width), 0, bitmap.Width - 1);
            var y = Math.Clamp((point.Y - bounds.Top) * bitmap.Height / Math.Max(1, bounds.Height), 0, bitmap.Height - 1);
            var darkNeighbors = 0;
            for (var dy = -1; dy <= 1; dy++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    var px = Math.Clamp(x + dx, 0, bitmap.Width - 1);
                    var py = Math.Clamp(y + dy, 0, bitmap.Height - 1);
                    var color = bitmap.GetPixel(px, py);
                    var brightness = (color.R + color.G + color.B) / 3;
                    if (color.A > 40 && brightness < 170)
                    {
                        darkNeighbors++;
                    }
                }
            }

            if (darkNeighbors >= 2)
            {
                selected.Add(point);
            }
        }

        if (selected.Count > requiredCount)
        {
            return EvenlySample(selected, requiredCount);
        }

        return selected;
    }

    private static List<Point> ManualPoints(IReadOnlyList<PatternPoint> manualPoints, Rectangle workArea, Size spacing)
    {
        return manualPoints
            .Select(point => new Point(
                workArea.Left + workArea.Width * Math.Clamp(point.XPercent, 0, 100) / 100,
                workArea.Top + workArea.Height * Math.Clamp(point.YPercent, 0, 100) / 100))
            .Select(point => ClampToWorkArea(point, workArea, spacing))
            .Distinct()
            .ToList();
    }

    private static List<Point> BuildOverflowGrid(IReadOnlyList<Point> existing, int count, Rectangle workArea, Size spacing)
    {
        var occupied = existing.ToHashSet();
        var points = new List<Point>(count);
        var center = existing.Count == 0
            ? Center(workArea)
            : new Point((int)Math.Round(existing.Average(point => point.X)), (int)Math.Round(existing.Average(point => point.Y)));
        var columns = Math.Max(1, workArea.Width / Math.Max(48, spacing.Width));
        var rows = Math.Max(1, workArea.Height / Math.Max(48, spacing.Height));
        var candidates = new List<Point>(columns * rows);
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                var point = new Point(workArea.Left + column * spacing.Width, workArea.Top + row * spacing.Height);
                candidates.Add(ClampToWorkArea(point, workArea, spacing));
            }
        }

        foreach (var point in candidates.Distinct().OrderBy(point => Distance(point, center)))
        {
            if (occupied.Add(point))
            {
                points.Add(point);
                if (points.Count >= count)
                {
                    break;
                }
            }
        }

        return points;
    }

    private static List<Point> OrderPoints(List<Point> points, PatternFillMode fillMode, Rectangle bounds)
    {
        var center = Center(bounds);
        return fillMode switch
        {
            PatternFillMode.TopToBottom => points.OrderBy(point => point.Y).ThenBy(point => point.X).ToList(),
            PatternFillMode.CenterOut => points.OrderBy(point => Distance(point, center)).ToList(),
            PatternFillMode.Clockwise => points.OrderBy(point => Math.Atan2(point.Y - center.Y, point.X - center.X)).ToList(),
            _ => points.OrderBy(point => point.X).ThenBy(point => point.Y).ToList(),
        };
    }

    private static List<Point> EvenlySample(IReadOnlyList<Point> points, int count)
    {
        if (points.Count <= count)
        {
            return points.ToList();
        }

        var sampled = new List<Point>(count);
        var step = points.Count / (double)count;
        for (var i = 0; i < count; i++)
        {
            sampled.Add(points[Math.Min(points.Count - 1, (int)Math.Floor(i * step))]);
        }

        return sampled.Distinct().ToList();
    }

    private static List<Point> Rotate(IEnumerable<Point> points, Point center, int degrees)
    {
        var radians = degrees * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return points
            .Select(point =>
            {
                var dx = point.X - center.X;
                var dy = point.Y - center.Y;
                return new Point(
                    (int)Math.Round(center.X + dx * cos - dy * sin),
                    (int)Math.Round(center.Y + dx * sin + dy * cos));
            })
            .ToList();
    }

    private static Point SnapToNearestGrid(Point point, IReadOnlyList<Point> grid)
    {
        return grid.OrderBy(candidate => Distance(candidate, point)).FirstOrDefault();
    }

    private static Point ClampToWorkArea(Point point, Rectangle workArea, Size spacing)
    {
        return new Point(
            Math.Clamp(point.X, workArea.Left, Math.Max(workArea.Left, workArea.Right - spacing.Width)),
            Math.Clamp(point.Y, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - spacing.Height)));
    }

    private static Point Center(Rectangle bounds) => new(bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2);

    private static double Distance(Point a, Point b) => Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));

    private static double DistanceToSegment(Point point, PointF a, PointF b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
        {
            return Math.Sqrt(Math.Pow(point.X - a.X, 2) + Math.Pow(point.Y - a.Y, 2));
        }

        var t = ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / (dx * dx + dy * dy);
        t = Math.Clamp(t, 0, 1);
        var closestX = a.X + t * dx;
        var closestY = a.Y + t * dy;
        return Math.Sqrt(Math.Pow(point.X - closestX, 2) + Math.Pow(point.Y - closestY, 2));
    }

    private static List<(PointF A, PointF B)> BuildSegments(IReadOnlyList<PointF> vertices, bool closed, bool breakAtMiddle)
    {
        var segments = new List<(PointF A, PointF B)>();
        for (var i = 0; i < vertices.Count - 1; i++)
        {
            if (breakAtMiddle && i == 1)
            {
                continue;
            }

            segments.Add((vertices[i], vertices[i + 1]));
        }

        if (closed && vertices.Count > 1)
        {
            segments.Add((vertices[^1], vertices[0]));
        }

        return segments;
    }

    private static double MinPolylineDistance(Point point, IReadOnlyList<(PointF A, PointF B)> segments)
    {
        if (segments.Count == 0)
        {
            return 0;
        }

        return segments.Min(segment => DistanceToSegment(point, segment.A, segment.B));
    }

    private static double MinPolylineDistanceAlong(Point point, IReadOnlyList<(PointF A, PointF B)> segments)
    {
        var best = double.MaxValue;
        var offset = 0.0;
        var bestAlong = 0.0;
        foreach (var segment in segments)
        {
            var length = Math.Sqrt(Math.Pow(segment.B.X - segment.A.X, 2) + Math.Pow(segment.B.Y - segment.A.Y, 2));
            var distance = DistanceToSegment(point, segment.A, segment.B);
            if (distance < best)
            {
                best = distance;
                bestAlong = offset + length / 2;
            }

            offset += length;
        }

        return bestAlong;
    }
}
