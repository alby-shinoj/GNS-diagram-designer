namespace GsnDiagramEditor.Services;

public class UndoRedoService
{
    private readonly Stack<string> _undo = new();
    private readonly Stack<string> _redo = new();

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void PushUndo(string snapshot)
    {
        if (_undo.Count > 0 && _undo.Peek() == snapshot)
        {
            return;
        }

        _undo.Push(snapshot);
        _redo.Clear();
    }

    public string? Undo(string currentSnapshot)
    {
        if (!CanUndo)
        {
            return null;
        }

        _redo.Push(currentSnapshot);
        return _undo.Pop();
    }

    public string? Redo(string currentSnapshot)
    {
        if (!CanRedo)
        {
            return null;
        }

        _undo.Push(currentSnapshot);
        return _redo.Pop();
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
