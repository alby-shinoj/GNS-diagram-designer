using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GsnDiagramEditor.Controls;
using GsnDiagramEditor.Models;
using GsnDiagramEditor.ViewModels;

namespace GsnDiagramEditor.Views;

public partial class EditorCanvas : UserControl
{
    private DiagramNode? _dragNode;
    private DiagramNode? _connectSource;
    private Point _dragOffset;
    private bool _dragStarted;

    public EditorCanvas()
    {
        InitializeComponent();
    }

    private MainViewModel? Vm => DataContext as MainViewModel;

    private void NodeVisual_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Vm == null || sender is not NodeVisual visual || visual.DataContext is not DiagramNode node)
        {
            return;
        }

        e.Handled = true;
        var point = e.GetPosition(DiagramSurface);

        if (Vm.IsConnectMode)
        {
            if (node.Type == NodeType.FreeText)
            {
                Vm.SelectNode(node);
                Vm.StatusMessage = "Free text labels cannot be used as connection endpoints.";
                return;
            }

            if (_connectSource == null)
            {
                _connectSource = node;
                Vm.SelectNode(node);
                Vm.StatusMessage = $"Connection source selected: {node.Title}. Now click target node.";
                return;
            }

            if (_connectSource.Id != node.Id)
            {
                Vm.AddConnection(_connectSource, node);
            }

            _connectSource = null;
            return;
        }

        Vm.SelectNode(node);
        _dragNode = node;
        _dragOffset = new Point(point.X - node.X, point.Y - node.Y);
        _dragStarted = false;
        visual.CaptureMouse();
    }

    private void ConnectionVisual_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Vm == null || sender is not ConnectionVisual visual || visual.DataContext is not DiagramConnection connection)
        {
            return;
        }

        e.Handled = true;
        Vm.SelectConnection(connection);
    }

    private void DiagramSurface_OnMouseMove(object sender, MouseEventArgs e)
    {
        if (Vm == null || _dragNode == null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (!_dragStarted)
        {
            Vm.CaptureUndoSnapshot();
            _dragStarted = true;
        }

        var point = e.GetPosition(DiagramSurface);
        var newX = point.X - _dragOffset.X;
        var newY = point.Y - _dragOffset.Y;

        if (Vm.SnapToGrid)
        {
            newX = Math.Round(newX / Vm.GridSize) * Vm.GridSize;
            newY = Math.Round(newY / Vm.GridSize) * Vm.GridSize;
        }

        _dragNode.X = Math.Max(0, Math.Min(Vm.CanvasWidth - _dragNode.Width, newX));
        _dragNode.Y = Math.Max(0, Math.Min(Vm.CanvasHeight - _dragNode.Height, newY));
        Vm.RefreshConnections();
    }

    private void DiagramSurface_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (Vm != null && _dragNode != null)
        {
            Vm.RefreshConnections();
        }

        if (Mouse.Captured is NodeVisual captured)
        {
            captured.ReleaseMouseCapture();
        }

        _dragNode = null;
        _dragStarted = false;
    }

    private void DiagramSurface_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (Vm == null)
        {
            return;
        }

        if (e.OriginalSource == DiagramSurface)
        {
            var point = e.GetPosition(DiagramSurface);
            if (e.ClickCount == 2)
            {
                Vm.AddTextAt(point.X, point.Y);
                return;
            }

            Vm.ClearSelection();
            _connectSource = null;
        }
    }

    private void DiagramScrollViewer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Vm == null || Keyboard.Modifiers != ModifierKeys.Control)
        {
            return;
        }

        e.Handled = true;
        Vm.Zoom = Math.Clamp(Vm.Zoom + (e.Delta > 0 ? 0.08 : -0.08), 0.35, 2.2);
    }
}
