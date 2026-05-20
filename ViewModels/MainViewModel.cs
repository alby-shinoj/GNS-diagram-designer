using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using GsnDiagramEditor.Commands;
using GsnDiagramEditor.Models;
using GsnDiagramEditor.Services;
using Microsoft.Win32;

namespace GsnDiagramEditor.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly ProjectFileService _projectFileService = new();
    private readonly DotGeneratorService _dotGeneratorService = new();
    private readonly UndoRedoService _undoRedoService = new();
    private readonly AutoLayoutService _autoLayoutService = new();

    private string _projectName = "GSN Project";
    private string _statusMessage = "Ready";
    private DiagramNode? _selectedNode;
    private DiagramConnection? _selectedConnection;
    private bool _isConnectMode;
    private bool _snapToGrid = true;
    private double _gridSize = 25;
    private double _zoom = 1.0;
    private string? _currentFilePath;
    private bool _isLoadingSnapshot;

    public MainViewModel()
    {
        Nodes.CollectionChanged += NodesOnCollectionChanged;
        Connections.CollectionChanged += ConnectionsOnCollectionChanged;

        NodeTypes = Enum.GetValues(typeof(NodeType)).Cast<NodeType>().ToArray();

        NewProjectCommand = new RelayCommand(NewProject);
        AddNodeCommand = new RelayCommand(AddNodeFromParameter);
        AddTextCommand = new RelayCommand(() => AddNodeAt(NodeType.FreeText, 260, 160));
        DeleteSelectedCommand = new RelayCommand(DeleteSelected);
        SaveProjectCommand = new RelayCommand(async () => await SaveProjectAsync());
        OpenProjectCommand = new RelayCommand(async () => await OpenProjectAsync());
        ExportDotCommand = new RelayCommand(ExportDot);
        UndoCommand = new RelayCommand(Undo);
        RedoCommand = new RelayCommand(Redo);
        AutoLayoutCommand = new RelayCommand(AutoLayout);

        SeedStarterProject();
    }

    public ObservableCollection<DiagramNode> Nodes { get; } = new();
    public ObservableCollection<DiagramConnection> Connections { get; } = new();
    public NodeType[] NodeTypes { get; }

    public ICommand NewProjectCommand { get; }
    public ICommand AddNodeCommand { get; }
    public ICommand AddTextCommand { get; }
    public ICommand DeleteSelectedCommand { get; }
    public ICommand SaveProjectCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand ExportDotCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand AutoLayoutCommand { get; }

    public double CanvasWidth { get; } = 3600;
    public double CanvasHeight { get; } = 2300;

    public string ProjectName
    {
        get => _projectName;
        set => SetProperty(ref _projectName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public DiagramNode? SelectedNode
    {
        get => _selectedNode;
        set => SetProperty(ref _selectedNode, value);
    }

    public DiagramConnection? SelectedConnection
    {
        get => _selectedConnection;
        set => SetProperty(ref _selectedConnection, value);
    }

    public bool IsConnectMode
    {
        get => _isConnectMode;
        set
        {
            if (SetProperty(ref _isConnectMode, value))
            {
                StatusMessage = value ? "Connect Mode: click source node, then target node." : "Connect Mode off.";
            }
        }
    }

    public bool SnapToGrid
    {
        get => _snapToGrid;
        set => SetProperty(ref _snapToGrid, value);
    }

    public double GridSize
    {
        get => _gridSize;
        set => SetProperty(ref _gridSize, Math.Max(5, value));
    }

    public double Zoom
    {
        get => _zoom;
        set
        {
            if (SetProperty(ref _zoom, Math.Clamp(value, 0.35, 2.2)))
            {
                OnPropertyChanged(nameof(ZoomPercent));
            }
        }
    }

    public string ZoomPercent => $"{Zoom:P0}";

    public void SelectNode(DiagramNode node)
    {
        foreach (var n in Nodes)
        {
            n.IsSelected = n.Id == node.Id;
        }

        foreach (var c in Connections)
        {
            c.IsSelected = false;
        }

        SelectedNode = node;
        SelectedConnection = null;
    }

    public void SelectConnection(DiagramConnection connection)
    {
        foreach (var n in Nodes)
        {
            n.IsSelected = false;
        }

        foreach (var c in Connections)
        {
            c.IsSelected = c.Id == connection.Id;
        }

        SelectedConnection = connection;
        SelectedNode = null;
    }

    public void ClearSelection()
    {
        foreach (var n in Nodes)
        {
            n.IsSelected = false;
        }

        foreach (var c in Connections)
        {
            c.IsSelected = false;
        }

        SelectedNode = null;
        SelectedConnection = null;
    }

    public void CaptureUndoSnapshot()
    {
        if (_isLoadingSnapshot)
        {
            return;
        }

        _undoRedoService.PushUndo(_projectFileService.ToJson(BuildProject()));
    }

    public void AddTextAt(double x, double y)
    {
        AddNodeAt(NodeType.FreeText, x, y);
    }

    public void AddConnection(DiagramNode source, DiagramNode target)
    {
        if (source.Type == NodeType.FreeText || target.Type == NodeType.FreeText)
        {
            StatusMessage = "Free text labels are not connected. Use a normal GSN shape for connections.";
            return;
        }

        if (Connections.Any(c => c.SourceNodeId == source.Id && c.TargetNodeId == target.Id))
        {
            StatusMessage = "Connection already exists.";
            return;
        }

        CaptureUndoSnapshot();
        var connection = new DiagramConnection
        {
            SourceNodeId = source.Id,
            TargetNodeId = target.Id,
            Label = string.Empty
        };
        Connections.Add(connection);
        HookConnection(connection);
        RefreshConnections();
        SelectConnection(connection);
        StatusMessage = $"Connected {source.Title} → {target.Title}.";
    }

    public void RefreshConnections()
    {
        foreach (var connection in Connections)
        {
            var source = Nodes.FirstOrDefault(n => n.Id == connection.SourceNodeId);
            var target = Nodes.FirstOrDefault(n => n.Id == connection.TargetNodeId);
            if (source == null || target == null)
            {
                continue;
            }

            connection.SourceX = source.CenterX;
            connection.SourceY = source.CenterY;
            connection.TargetX = target.CenterX;
            connection.TargetY = target.CenterY;
            connection.NotifyGeometryChanged();
        }
    }

    public void QueueRender()
    {
        // Kept so the editor canvas can call one simple method after changes.
        // The SVG/Graphviz preview was removed, so this only refreshes connection geometry.
        RefreshConnections();
    }

    private void AddNodeFromParameter(object? parameter)
    {
        var nodeType = parameter is NodeType nt ? nt : NodeType.Goal;
        var index = Nodes.Count;
        var x = 140 + (index % 4) * 230;
        var y = 100 + (index / 4) * 160;
        AddNodeAt(nodeType, x, y);
    }

    private void AddNodeAt(NodeType nodeType, double x, double y)
    {
        CaptureUndoSnapshot();

        if (SnapToGrid)
        {
            x = Math.Round(x / GridSize) * GridSize;
            y = Math.Round(y / GridSize) * GridSize;
        }

        var isText = nodeType == NodeType.FreeText;
        var node = new DiagramNode
        {
            Type = nodeType,
            Title = DefaultTitle(nodeType),
            X = Math.Max(0, x),
            Y = Math.Max(0, y),
            Width = isText ? 260 : 170,
            Height = isText ? 55 : 90,
            FillColor = isText ? "#00FFFFFF" : DefaultFill(nodeType),
            BorderColor = isText ? "#94A3B8" : "#1F2937",
            TextColor = "#111827",
            FontSize = isText ? 18 : 14
        };

        Nodes.Add(node);
        HookNode(node);
        SelectNode(node);
        RefreshConnections();
        StatusMessage = isText ? "Added free text. Drag it anywhere and edit the text in Properties." : $"Added {nodeType} node.";
    }

    private void DeleteSelected()
    {
        if (SelectedNode == null && SelectedConnection == null)
        {
            return;
        }

        CaptureUndoSnapshot();

        if (SelectedNode != null)
        {
            var id = SelectedNode.Id;
            var attached = Connections.Where(c => c.SourceNodeId == id || c.TargetNodeId == id).ToList();
            foreach (var c in attached)
            {
                Connections.Remove(c);
            }

            Nodes.Remove(SelectedNode);
            SelectedNode = null;
        }
        else if (SelectedConnection != null)
        {
            Connections.Remove(SelectedConnection);
            SelectedConnection = null;
        }

        RefreshConnections();
        StatusMessage = "Deleted selected item.";
    }

    private void NewProject()
    {
        CaptureUndoSnapshot();
        Nodes.Clear();
        Connections.Clear();
        ProjectName = $"GSN Project {DateTime.Now:yyyy-MM-dd HH-mm}";
        _currentFilePath = null;
        ClearSelection();
        StatusMessage = "New project created.";
    }

    private async Task SaveProjectAsync()
    {
        var path = _currentFilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            var dialog = new SaveFileDialog
            {
                Filter = "GSN JSON Project (*.json)|*.json",
                FileName = SanitizeFileName(ProjectName) + ".json"
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            path = dialog.FileName;
        }

        await _projectFileService.SaveAsync(path, BuildProject());
        _currentFilePath = path;
        StatusMessage = $"Saved JSON project: {path}";
    }

    private async Task OpenProjectAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "GSN JSON Project (*.json)|*.json"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var project = await _projectFileService.OpenAsync(dialog.FileName);
        LoadProject(project);
        _currentFilePath = dialog.FileName;
        StatusMessage = $"Opened project: {dialog.FileName}";
    }

    private void ExportDot()
    {
        var dialog = new SaveFileDialog
        {
            Filter = "Graphviz DOT (*.dot)|*.dot",
            FileName = SanitizeFileName(ProjectName) + ".dot"
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var dot = _dotGeneratorService.Generate(BuildProject());
        File.WriteAllText(dialog.FileName, dot);
        StatusMessage = $"Exported DOT: {dialog.FileName}";
    }

    private void AutoLayout()
    {
        var layoutNodes = Nodes.Where(n => n.Type != NodeType.FreeText).ToList();
        if (layoutNodes.Count == 0)
        {
            return;
        }

        CaptureUndoSnapshot();
        _autoLayoutService.ApplySimpleTopDownLayout(layoutNodes, Connections.ToList());
        RefreshConnections();
        StatusMessage = "Applied simple auto layout. Free text labels were kept in place.";
    }

    private void Undo()
    {
        var snapshot = _undoRedoService.Undo(_projectFileService.ToJson(BuildProject()));
        if (snapshot == null)
        {
            StatusMessage = "Nothing to undo.";
            return;
        }

        LoadProject(_projectFileService.FromJson(snapshot));
        StatusMessage = "Undo complete.";
    }

    private void Redo()
    {
        var snapshot = _undoRedoService.Redo(_projectFileService.ToJson(BuildProject()));
        if (snapshot == null)
        {
            StatusMessage = "Nothing to redo.";
            return;
        }

        LoadProject(_projectFileService.FromJson(snapshot));
        StatusMessage = "Redo complete.";
    }

    private DiagramProject BuildProject()
    {
        return new DiagramProject
        {
            ProjectName = ProjectName,
            Nodes = Nodes.Select(CloneNode).ToList(),
            Connections = Connections.Select(CloneConnection).ToList()
        };
    }

    private void LoadProject(DiagramProject project)
    {
        _isLoadingSnapshot = true;
        try
        {
            Nodes.Clear();
            Connections.Clear();
            ProjectName = project.ProjectName;

            foreach (var node in project.Nodes)
            {
                node.IsSelected = false;
                Nodes.Add(node);
                HookNode(node);
            }

            foreach (var connection in project.Connections)
            {
                connection.IsSelected = false;
                Connections.Add(connection);
                HookConnection(connection);
            }

            ClearSelection();
            RefreshConnections();
        }
        finally
        {
            _isLoadingSnapshot = false;
        }
    }

    private void SeedStarterProject()
    {
        ProjectName = $"GSN Project {DateTime.Now:yyyy-MM-dd HH-mm}";

        var g1 = new DiagramNode
        {
            Type = NodeType.Goal,
            Title = "G1: System is acceptably safe",
            X = 360,
            Y = 80,
            FillColor = "#EFF6FF"
        };
        var s1 = new DiagramNode
        {
            Type = NodeType.Strategy,
            Title = "S1: Argument over evidence",
            X = 360,
            Y = 250,
            FillColor = "#F0FDF4"
        };
        var c1 = new DiagramNode
        {
            Type = NodeType.Context,
            Title = "C1: Operating context",
            X = 660,
            Y = 80,
            FillColor = "#FEFCE8"
        };
        var e1 = new DiagramNode
        {
            Type = NodeType.Solution,
            Title = "E1: Test evidence",
            X = 360,
            Y = 430,
            FillColor = "#FDF2F8"
        };
        var label = new DiagramNode
        {
            Type = NodeType.FreeText,
            Title = "Free text label",
            X = 120,
            Y = 70,
            Width = 220,
            Height = 50,
            FillColor = "#00FFFFFF",
            BorderColor = "#94A3B8",
            FontSize = 18
        };

        Nodes.Add(label);
        Nodes.Add(g1);
        Nodes.Add(s1);
        Nodes.Add(c1);
        Nodes.Add(e1);

        Connections.Add(new DiagramConnection { SourceNodeId = g1.Id, TargetNodeId = s1.Id });
        Connections.Add(new DiagramConnection { SourceNodeId = s1.Id, TargetNodeId = e1.Id });
        Connections.Add(new DiagramConnection { SourceNodeId = g1.Id, TargetNodeId = c1.Id, Label = "context" });

        foreach (var node in Nodes) HookNode(node);
        foreach (var connection in Connections) HookConnection(connection);
        RefreshConnections();
    }

    private void NodesOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (DiagramNode node in e.NewItems)
            {
                HookNode(node);
            }
        }
        RefreshConnections();
    }

    private void ConnectionsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (DiagramConnection connection in e.NewItems)
            {
                HookConnection(connection);
            }
        }
        RefreshConnections();
    }

    private void HookNode(DiagramNode node)
    {
        node.PropertyChanged -= NodeOnPropertyChanged;
        node.PropertyChanged += NodeOnPropertyChanged;
    }

    private void HookConnection(DiagramConnection connection)
    {
        connection.PropertyChanged -= ConnectionOnPropertyChanged;
        connection.PropertyChanged += ConnectionOnPropertyChanged;
    }

    private void NodeOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoadingSnapshot || e.PropertyName == nameof(DiagramNode.IsSelected))
        {
            return;
        }

        RefreshConnections();
    }

    private void ConnectionOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_isLoadingSnapshot || e.PropertyName is nameof(DiagramConnection.IsSelected) or nameof(DiagramConnection.SourceX) or nameof(DiagramConnection.SourceY) or nameof(DiagramConnection.TargetX) or nameof(DiagramConnection.TargetY))
        {
            return;
        }
    }

    private static DiagramNode CloneNode(DiagramNode node) => new()
    {
        Id = node.Id,
        Title = node.Title,
        Description = node.Description,
        Type = node.Type,
        X = node.X,
        Y = node.Y,
        Width = node.Width,
        Height = node.Height,
        FillColor = node.FillColor,
        BorderColor = node.BorderColor,
        TextColor = node.TextColor,
        FontSize = node.FontSize
    };

    private static DiagramConnection CloneConnection(DiagramConnection connection) => new()
    {
        Id = connection.Id,
        SourceNodeId = connection.SourceNodeId,
        TargetNodeId = connection.TargetNodeId,
        Label = connection.Label,
        LineColor = connection.LineColor,
        Thickness = connection.Thickness,
        ArrowHead = connection.ArrowHead,
        IsDashed = connection.IsDashed
    };

    private static string DefaultTitle(NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.Goal => "New Goal",
            NodeType.Strategy => "New Strategy",
            NodeType.Solution => "New Evidence",
            NodeType.Context => "New Context",
            NodeType.Assumption => "New Assumption",
            NodeType.Justification => "New Justification",
            NodeType.UndevelopedGoal => "Undeveloped Goal",
            NodeType.Module => "New Module",
            NodeType.FreeText => "Free text",
            _ => nodeType.ToString()
        };
    }

    private static string DefaultFill(NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.Goal => "#EFF6FF",
            NodeType.Strategy => "#F0FDF4",
            NodeType.Solution => "#FDF2F8",
            NodeType.Context => "#FEFCE8",
            NodeType.Assumption => "#FFF7ED",
            NodeType.Justification => "#F5F3FF",
            NodeType.UndevelopedGoal => "#F3F4F6",
            NodeType.Module => "#ECFEFF",
            _ => "#FFFFFF"
        };
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(invalid, '-');
        }
        return string.IsNullOrWhiteSpace(name) ? "gsn-project" : name;
    }
}
