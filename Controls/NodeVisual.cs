using System.Globalization;
using System.Windows;
using System.Windows.Media;
using GsnDiagramEditor.Models;

namespace GsnDiagramEditor.Controls;

public class NodeVisual : FrameworkElement
{
    public static readonly DependencyProperty NodeTypeProperty = DependencyProperty.Register(
        nameof(NodeType), typeof(NodeType), typeof(NodeVisual), new FrameworkPropertyMetadata(NodeType.Goal, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(NodeVisual), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(NodeVisual), new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FillColorProperty = DependencyProperty.Register(
        nameof(FillColor), typeof(string), typeof(NodeVisual), new FrameworkPropertyMetadata("#FFFFFF", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BorderColorProperty = DependencyProperty.Register(
        nameof(BorderColor), typeof(string), typeof(NodeVisual), new FrameworkPropertyMetadata("#1F2937", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TextColorProperty = DependencyProperty.Register(
        nameof(TextColor), typeof(string), typeof(NodeVisual), new FrameworkPropertyMetadata("#111827", FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty FontSizeValueProperty = DependencyProperty.Register(
        nameof(FontSizeValue), typeof(double), typeof(NodeVisual), new FrameworkPropertyMetadata(14.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty IsSelectedProperty = DependencyProperty.Register(
        nameof(IsSelected), typeof(bool), typeof(NodeVisual), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public NodeType NodeType
    {
        get => (NodeType)GetValue(NodeTypeProperty);
        set => SetValue(NodeTypeProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string FillColor
    {
        get => (string)GetValue(FillColorProperty);
        set => SetValue(FillColorProperty, value);
    }

    public string BorderColor
    {
        get => (string)GetValue(BorderColorProperty);
        set => SetValue(BorderColorProperty, value);
    }

    public string TextColor
    {
        get => (string)GetValue(TextColorProperty);
        set => SetValue(TextColorProperty, value);
    }

    public double FontSizeValue
    {
        get => (double)GetValue(FontSizeValueProperty);
        set => SetValue(FontSizeValueProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var rect = new Rect(1, 1, Math.Max(1, ActualWidth - 2), Math.Max(1, ActualHeight - 2));
        var fill = BrushFromHex(FillColor, Brushes.White);
        var border = BrushFromHex(BorderColor, Brushes.Black);
        var textBrush = BrushFromHex(TextColor, Brushes.Black);

        if (NodeType == NodeType.FreeText)
        {
            DrawText(dc, rect, textBrush, true);
            if (IsSelected)
            {
                var textSelectionPen = new Pen(new SolidColorBrush(Color.FromRgb(37, 99, 235)), 1.5) { DashStyle = DashStyles.Dot };
                dc.DrawRectangle(null, textSelectionPen, new Rect(0, 0, ActualWidth, ActualHeight));
            }
            return;
        }

        var pen = new Pen(border, IsSelected ? 2.8 : 1.4);
        if (NodeType == NodeType.UndevelopedGoal)
        {
            pen.DashStyle = DashStyles.Dash;
        }

        DrawShape(dc, rect, fill, pen);
        if (IsSelected)
        {
            var selectedPen = new Pen(new SolidColorBrush(Color.FromRgb(37, 99, 235)), 1.5) { DashStyle = DashStyles.Dot };
            dc.DrawRectangle(null, selectedPen, new Rect(0, 0, ActualWidth, ActualHeight));
        }

        DrawText(dc, rect, textBrush, false);
    }

    private void DrawShape(DrawingContext dc, Rect rect, Brush fill, Pen pen)
    {
        switch (NodeType)
        {
            case NodeType.Solution:
            case NodeType.Circle:
            case NodeType.Ellipse:
                dc.DrawEllipse(fill, pen, new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2), rect.Width / 2, rect.Height / 2);
                break;
            case NodeType.Strategy:
            case NodeType.Parallelogram:
                var shift = Math.Min(28, rect.Width * 0.18);
                DrawPolygon(dc, fill, pen, new[]
                {
                    new Point(rect.Left + shift, rect.Top),
                    new Point(rect.Right, rect.Top),
                    new Point(rect.Right - shift, rect.Bottom),
                    new Point(rect.Left, rect.Bottom)
                });
                break;
            case NodeType.Diamond:
                DrawPolygon(dc, fill, pen, new[]
                {
                    new Point(rect.Left + rect.Width / 2, rect.Top),
                    new Point(rect.Right, rect.Top + rect.Height / 2),
                    new Point(rect.Left + rect.Width / 2, rect.Bottom),
                    new Point(rect.Left, rect.Top + rect.Height / 2)
                });
                break;
            case NodeType.Context:
            case NodeType.Assumption:
            case NodeType.Justification:
            case NodeType.Note:
                DrawNote(dc, rect, fill, pen);
                break;
            case NodeType.Folder:
                DrawFolder(dc, rect, fill, pen);
                break;
            case NodeType.Module:
            case NodeType.Component:
                dc.DrawRectangle(fill, pen, rect);
                dc.DrawRectangle(null, pen, new Rect(rect.Left + 8, rect.Top - 1, 34, 10));
                break;
            case NodeType.RoundedRectangle:
            case NodeType.Goal:
            case NodeType.UndevelopedGoal:
            case NodeType.Rectangle:
            default:
                dc.DrawRoundedRectangle(fill, pen, rect, 8, 8);
                break;
        }
    }

    private static void DrawPolygon(DrawingContext dc, Brush fill, Pen pen, Point[] points)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(points[0], true, true);
            ctx.PolyLineTo(points.Skip(1).ToList(), true, true);
        }
        geometry.Freeze();
        dc.DrawGeometry(fill, pen, geometry);
    }

    private static void DrawNote(DrawingContext dc, Rect rect, Brush fill, Pen pen)
    {
        var fold = Math.Min(18, Math.Min(rect.Width, rect.Height) * 0.2);
        var points = new[]
        {
            new Point(rect.Left, rect.Top),
            new Point(rect.Right - fold, rect.Top),
            new Point(rect.Right, rect.Top + fold),
            new Point(rect.Right, rect.Bottom),
            new Point(rect.Left, rect.Bottom)
        };
        DrawPolygon(dc, fill, pen, points);
        dc.DrawLine(pen, new Point(rect.Right - fold, rect.Top), new Point(rect.Right - fold, rect.Top + fold));
        dc.DrawLine(pen, new Point(rect.Right - fold, rect.Top + fold), new Point(rect.Right, rect.Top + fold));
    }

    private static void DrawFolder(DrawingContext dc, Rect rect, Brush fill, Pen pen)
    {
        var tabHeight = Math.Min(18, rect.Height * 0.25);
        var tabWidth = Math.Min(60, rect.Width * 0.38);
        var geo = new StreamGeometry();
        using (var ctx = geo.Open())
        {
            ctx.BeginFigure(new Point(rect.Left, rect.Top + tabHeight), true, true);
            ctx.LineTo(new Point(rect.Left + 10, rect.Top), true, false);
            ctx.LineTo(new Point(rect.Left + tabWidth, rect.Top), true, false);
            ctx.LineTo(new Point(rect.Left + tabWidth + 12, rect.Top + tabHeight), true, false);
            ctx.LineTo(new Point(rect.Right, rect.Top + tabHeight), true, false);
            ctx.LineTo(new Point(rect.Right, rect.Bottom), true, false);
            ctx.LineTo(new Point(rect.Left, rect.Bottom), true, false);
        }
        geo.Freeze();
        dc.DrawGeometry(fill, pen, geo);
    }

    private void DrawText(DrawingContext dc, Rect rect, Brush textBrush, bool isFreeText)
    {
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface("Segoe UI");
        var title = string.IsNullOrWhiteSpace(Title) ? (isFreeText ? "Free text" : NodeType.ToString()) : Title;
        var body = string.IsNullOrWhiteSpace(Description) ? string.Empty : Description;
        var display = string.IsNullOrWhiteSpace(body) ? title : title + "\n" + body;

        var ft = new FormattedText(
            display,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            typeface,
            FontSizeValue,
            textBrush,
            dpi)
        {
            TextAlignment = isFreeText ? TextAlignment.Left : TextAlignment.Center,
            MaxTextWidth = Math.Max(20, rect.Width - 16),
            MaxTextHeight = Math.Max(20, rect.Height - 12),
            Trimming = TextTrimming.CharacterEllipsis
        };

        var y = isFreeText ? rect.Top + 4 : rect.Top + Math.Max(6, (rect.Height - ft.Height) / 2);
        var textPoint = new Point(rect.Left + 8, y);
        dc.DrawText(ft, textPoint);
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
