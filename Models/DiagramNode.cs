using System.Text.Json.Serialization;
using GsnDiagramEditor.ViewModels;

namespace GsnDiagramEditor.Models;

public class DiagramNode : BaseViewModel
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _title = "New Goal";
    private string _description = string.Empty;
    private NodeType _type = NodeType.Goal;
    private double _x;
    private double _y;
    private double _width = 170;
    private double _height = 90;
    private string _fillColor = "#FFFFFF";
    private string _borderColor = "#1F2937";
    private string _textColor = "#111827";
    private double _fontSize = 14;
    private bool _isSelected;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public NodeType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    public double X
    {
        get => _x;
        set => SetProperty(ref _x, value);
    }

    public double Y
    {
        get => _y;
        set => SetProperty(ref _y, value);
    }

    public double Width
    {
        get => _width;
        set => SetProperty(ref _width, Math.Max(50, value));
    }

    public double Height
    {
        get => _height;
        set => SetProperty(ref _height, Math.Max(35, value));
    }

    public string FillColor
    {
        get => _fillColor;
        set => SetProperty(ref _fillColor, value);
    }

    public string BorderColor
    {
        get => _borderColor;
        set => SetProperty(ref _borderColor, value);
    }

    public string TextColor
    {
        get => _textColor;
        set => SetProperty(ref _textColor, value);
    }

    public double FontSize
    {
        get => _fontSize;
        set => SetProperty(ref _fontSize, Math.Max(8, value));
    }

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    [JsonIgnore]
    public double CenterX => X + Width / 2;

    [JsonIgnore]
    public double CenterY => Y + Height / 2;
}
