using System.Text.Json.Serialization;
using GsnDiagramEditor.ViewModels;

namespace GsnDiagramEditor.Models;

public class DiagramConnection : BaseViewModel
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _sourceNodeId = string.Empty;
    private string _targetNodeId = string.Empty;
    private string _label = string.Empty;
    private string _lineColor = "#374151";
    private double _thickness = 1.6;
    private string _arrowHead = "normal";
    private bool _isDashed;
    private bool _isSelected;
    private double _sourceX;
    private double _sourceY;
    private double _targetX;
    private double _targetY;

    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public string SourceNodeId
    {
        get => _sourceNodeId;
        set => SetProperty(ref _sourceNodeId, value);
    }

    public string TargetNodeId
    {
        get => _targetNodeId;
        set => SetProperty(ref _targetNodeId, value);
    }

    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    public string LineColor
    {
        get => _lineColor;
        set => SetProperty(ref _lineColor, value);
    }

    public double Thickness
    {
        get => _thickness;
        set => SetProperty(ref _thickness, Math.Max(0.5, value));
    }

    public string ArrowHead
    {
        get => _arrowHead;
        set => SetProperty(ref _arrowHead, value);
    }

    public bool IsDashed
    {
        get => _isDashed;
        set => SetProperty(ref _isDashed, value);
    }

    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    [JsonIgnore]
    public double SourceX
    {
        get => _sourceX;
        set => SetProperty(ref _sourceX, value);
    }

    [JsonIgnore]
    public double SourceY
    {
        get => _sourceY;
        set => SetProperty(ref _sourceY, value);
    }

    [JsonIgnore]
    public double TargetX
    {
        get => _targetX;
        set => SetProperty(ref _targetX, value);
    }

    [JsonIgnore]
    public double TargetY
    {
        get => _targetY;
        set => SetProperty(ref _targetY, value);
    }

    [JsonIgnore]
    public double LabelX => (SourceX + TargetX) / 2;

    [JsonIgnore]
    public double LabelY => (SourceY + TargetY) / 2;

    public void NotifyGeometryChanged()
    {
        OnPropertyChanged(nameof(LabelX));
        OnPropertyChanged(nameof(LabelY));
    }
}
