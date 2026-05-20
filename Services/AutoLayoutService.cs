using GsnDiagramEditor.Models;

namespace GsnDiagramEditor.Services;

public class AutoLayoutService
{
    public void ApplySimpleTopDownLayout(IList<DiagramNode> nodes, IList<DiagramConnection> connections, double startX = 120, double startY = 80)
    {
        if (nodes.Count == 0)
        {
            return;
        }

        var incoming = nodes.ToDictionary(n => n.Id, _ => 0);
        foreach (var edge in connections)
        {
            if (incoming.ContainsKey(edge.TargetNodeId))
            {
                incoming[edge.TargetNodeId]++;
            }
        }

        var roots = nodes.Where(n => incoming[n.Id] == 0).ToList();
        if (roots.Count == 0)
        {
            roots.Add(nodes[0]);
        }

        var levels = new Dictionary<string, int>();
        var queue = new Queue<DiagramNode>();
        foreach (var root in roots)
        {
            levels[root.Id] = 0;
            queue.Enqueue(root);
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            var level = levels[node.Id];
            var children = connections
                .Where(c => c.SourceNodeId == node.Id)
                .Select(c => nodes.FirstOrDefault(n => n.Id == c.TargetNodeId))
                .Where(n => n != null)
                .Cast<DiagramNode>();

            foreach (var child in children)
            {
                var childLevel = level + 1;
                if (!levels.ContainsKey(child.Id) || levels[child.Id] < childLevel)
                {
                    levels[child.Id] = childLevel;
                    queue.Enqueue(child);
                }
            }
        }

        foreach (var node in nodes.Where(n => !levels.ContainsKey(n.Id)))
        {
            levels[node.Id] = levels.Count == 0 ? 0 : levels.Values.Max() + 1;
        }

        var grouped = nodes.GroupBy(n => levels[n.Id]).OrderBy(g => g.Key).ToList();
        const double xGap = 240;
        const double yGap = 150;

        foreach (var group in grouped)
        {
            var groupNodes = group.ToList();
            for (var i = 0; i < groupNodes.Count; i++)
            {
                var n = groupNodes[i];
                n.X = startX + i * xGap;
                n.Y = startY + group.Key * yGap;
            }
        }
    }
}
