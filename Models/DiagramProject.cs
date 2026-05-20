namespace GsnDiagramEditor.Models;

public class DiagramProject
{
    public string ProjectName { get; set; } = "Untitled GSN Project";
    public List<DiagramNode> Nodes { get; set; } = new();
    public List<DiagramConnection> Connections { get; set; } = new();
}
