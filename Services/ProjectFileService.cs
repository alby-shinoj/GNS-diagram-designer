using System.IO;
using System.Text.Json;
using GsnDiagramEditor.Models;

namespace GsnDiagramEditor.Services;

public class ProjectFileService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task SaveAsync(string path, DiagramProject project)
    {
        var json = JsonSerializer.Serialize(project, _jsonOptions);
        await File.WriteAllTextAsync(path, json);
    }

    public async Task<DiagramProject> OpenAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<DiagramProject>(json, _jsonOptions) ?? new DiagramProject();
    }

    public string ToJson(DiagramProject project)
    {
        return JsonSerializer.Serialize(project, _jsonOptions);
    }

    public DiagramProject FromJson(string json)
    {
        return JsonSerializer.Deserialize<DiagramProject>(json, _jsonOptions) ?? new DiagramProject();
    }
}
