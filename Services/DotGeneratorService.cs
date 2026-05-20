using System.Globalization;
using System.Text;
using GsnDiagramEditor.Models;

namespace GsnDiagramEditor.Services;

public class DotGeneratorService
{
    public string Generate(DiagramProject project)
    {
        var sb = new StringBuilder();
        sb.AppendLine("digraph GSN {");
        sb.AppendLine("  graph [rankdir=TB, bgcolor=\"transparent\", splines=ortho, nodesep=0.45, ranksep=0.65];");
        sb.AppendLine("  node [fontname=\"Segoe UI\", fontsize=12, style=\"filled\", penwidth=1.4];");
        sb.AppendLine("  edge [fontname=\"Segoe UI\", fontsize=10, color=\"#374151\", arrowsize=0.8];");
        sb.AppendLine();

        foreach (var node in project.Nodes)
        {
            var id = SafeId(node.Id);
            var label = EscapeLabel(string.IsNullOrWhiteSpace(node.Description)
                ? node.Title
                : $"{node.Title}\\n{node.Description}");

            sb.Append("  ").Append(id).Append(" [");
            sb.Append("label=\"").Append(label).Append("\", ");
            sb.Append("shape=").Append(MapShape(node.Type)).Append(", ");

            if (node.Type == NodeType.FreeText)
            {
                sb.Append("style=\"\", margin=0, ");
            }
            else
            {
                sb.Append("fillcolor=\"").Append(SafeColor(node.FillColor, "#FFFFFF")).Append("\", ");
                sb.Append("color=\"").Append(SafeColor(node.BorderColor, "#1F2937")).Append("\", ");
            }

            sb.Append("fontcolor=\"").Append(SafeColor(node.TextColor, "#111827")).Append("\", ");
            sb.Append("fontsize=").Append(node.FontSize.ToString(CultureInfo.InvariantCulture)).Append(", ");
            sb.Append("width=").Append(Math.Max(0.5, node.Width / 96.0).ToString("0.##", CultureInfo.InvariantCulture)).Append(", ");
            sb.Append("height=").Append(Math.Max(0.35, node.Height / 96.0).ToString("0.##", CultureInfo.InvariantCulture));

            if (node.Type == NodeType.UndevelopedGoal)
            {
                sb.Append(", style=\"filled,dashed\"");
            }

            sb.AppendLine("]; ");
        }

        sb.AppendLine();
        foreach (var edge in project.Connections)
        {
            if (project.Nodes.Any(n => n.Id == edge.SourceNodeId && n.Type == NodeType.FreeText) ||
                project.Nodes.Any(n => n.Id == edge.TargetNodeId && n.Type == NodeType.FreeText))
            {
                continue;
            }

            var source = SafeId(edge.SourceNodeId);
            var target = SafeId(edge.TargetNodeId);
            sb.Append("  ").Append(source).Append(" -> ").Append(target).Append(" [");
            sb.Append("label=\"").Append(EscapeLabel(edge.Label)).Append("\", ");
            sb.Append("color=\"").Append(SafeColor(edge.LineColor, "#374151")).Append("\", ");
            sb.Append("penwidth=").Append(edge.Thickness.ToString(CultureInfo.InvariantCulture)).Append(", ");
            sb.Append("arrowhead=").Append(string.IsNullOrWhiteSpace(edge.ArrowHead) ? "normal" : edge.ArrowHead);
            if (edge.IsDashed)
            {
                sb.Append(", style=dashed");
            }
            sb.AppendLine("]; ");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static string SafeId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return "node_empty";
        }

        var clean = new string(id.Select(ch => char.IsLetterOrDigit(ch) ? ch : '_').ToArray());
        return char.IsDigit(clean[0]) ? "n_" + clean : clean;
    }

    private static string EscapeLabel(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return text.Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r\n", "\\n")
            .Replace("\n", "\\n")
            .Replace("\r", "\\n");
    }

    private static string MapShape(NodeType type)
    {
        return type switch
        {
            NodeType.Goal => "box",
            NodeType.Strategy => "parallelogram",
            NodeType.Solution => "ellipse",
            NodeType.Context => "note",
            NodeType.Assumption => "note",
            NodeType.Justification => "note",
            NodeType.UndevelopedGoal => "box",
            NodeType.Module => "component",
            NodeType.FreeText => "plaintext",
            NodeType.Rectangle => "box",
            NodeType.RoundedRectangle => "box",
            NodeType.Circle => "circle",
            NodeType.Ellipse => "ellipse",
            NodeType.Diamond => "diamond",
            NodeType.Parallelogram => "parallelogram",
            NodeType.Note => "note",
            NodeType.Folder => "folder",
            NodeType.Component => "component",
            _ => "box"
        };
    }

    private static string SafeColor(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        return value.Trim();
    }
}
