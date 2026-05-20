using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace GsnDiagramEditor.Controls;

public class ConnectionVisual : FrameworkElement
{
    public static readonly DependencyProperty SourceXProperty = DependencyProperty.Register(nameof(SourceX), typeof(double), typeof(ConnectionVisual), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty SourceYProperty = DependencyProperty.Register(nameof(SourceY), typeof(double), typeof(ConnectionVisual), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty TargetXProperty = DependencyProperty.Register(nameof(TargetX), typeof(double), typeof(ConnectionVisual), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty TargetYProperty = DependencyProperty.Register(nameof(TargetY), typeof(double), typeof(ConnectionVisual), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(nameof(Label), typeof(string), typeof(ConnectionVisual), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty LineColorProperty = DependencyProperty.Register(nameof(LineColor), typeof(string), typeof(ConnectionVisual), new FrameworkPropertyMetadata("#374151", FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty ThicknessValueProperty = DependencyProperty.Register(nameof(ThicknessValue), typeof(double), typeof(ConnectionVisual), new FrameworkPropertyMetadata(1.6, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty IsDashedProperty = DependencyProperty.Register(nameof(IsDashed), typeof(bool), typeof(ConnectionVisual), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(ConnectionVisual), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public double SourceX { get => (double)GetValue(SourceXProperty); set => SetValue(SourceXProperty, value); }
    public double SourceY { get => (double)GetValue(SourceYProperty); set => SetValue(SourceYProperty, value); }
    public double TargetX { get => (double)GetValue(TargetXProperty); set => SetValue(TargetXProperty, value); }
    public double TargetY { get => (double)GetValue(TargetYProperty); set => SetValue(TargetYProperty, value); }
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string LineColor { get => (string)GetValue(LineColorProperty); set => SetValue(LineColorProperty, value); }
    public double ThicknessValue { get => (double)GetValue(ThicknessValueProperty); set => SetValue(ThicknessValueProperty, value); }
    public bool IsDashed { get => (bool)GetValue(IsDashedProperty); set => SetValue(IsDashedProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var brush = BrushFromHex(LineColor, Brushes.DarkSlateGray);
        var pen = new Pen(brush, IsSelected ? ThicknessValue + 1.2 : ThicknessValue);
        if (IsDashed)
        {
            pen.DashStyle = DashStyles.Dash;
        }

        var start = new Point(SourceX, SourceY);
        var end = new Point(TargetX, TargetY);
        dc.DrawLine(pen, start, end);
        DrawArrowHead(dc, brush, start, end);
        DrawLabel(dc, brush, start, end);
    }

    private void DrawArrowHead(DrawingContext dc, Brush brush, Point start, Point end)
    {
        var vector = start - end;
        if (vector.Length < 0.1)
        {
            return;
        }

        vector.Normalize();
        var perpendicular = new Vector(-vector.Y, vector.X);
        const double length = 12;
        const double width = 5;
        var p1 = end + vector * length + perpendicular * width;
        var p2 = end + vector * length - perpendicular * width;

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(end, true, true);
            ctx.LineTo(p1, true, false);
            ctx.LineTo(p2, true, false);
        }
        geometry.Freeze();
        dc.DrawGeometry(brush, null, geometry);
    }

    private void DrawLabel(DrawingContext dc, Brush brush, Point start, Point end)
    {
        if (string.IsNullOrWhiteSpace(Label))
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var ft = new FormattedText(Label, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, new Typeface("Segoe UI"), 12, brush, dpi)
        {
            TextAlignment = TextAlignment.Center,
            MaxTextWidth = 160,
            Trimming = TextTrimming.CharacterEllipsis
        };
        var x = (start.X + end.X) / 2 - ft.Width / 2;
        var y = (start.Y + end.Y) / 2 - ft.Height - 4;
        var bg = new SolidColorBrush(Color.FromArgb(235, 255, 255, 255));
        dc.DrawRoundedRectangle(bg, null, new Rect(x - 4, y - 2, ft.Width + 8, ft.Height + 4), 4, 4);
        dc.DrawText(ft, new Point(x, y));
    }

    private static Brush BrushFromHex(string? hex, Brush fallback)
    {
        try
        {
            return (Brush)new BrushConverter().ConvertFromString(hex ?? "")!;
        }
        catch
        {
            return fallback;
        }
    }
}
