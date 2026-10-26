using System;
using System.Collections.Generic;
using UnityEngine;

public class UndoManager : MonoBehaviour
{
    public static event Action<bool> OnCanUndoChanged;

    public struct BoxRecord
    {
        public int Id;
        public Vector2Int Min;
        public Vector2Int Max;
    }

    private readonly Stack<List<BoxRecord>> _history = new();
    private readonly List<BoxRecord> _currentBoxes = new();

    public bool CanUndo => _history.Count > 0;

    public void RecordStateBeforeMove()
    {
        List<BoxRecord> snapshot = new(_currentBoxes);
        _history.Push(snapshot);
        OnCanUndoChanged?.Invoke(CanUndo);
    }

    public void RegisterBoxPlaced(int id, Vector2Int min, Vector2Int max)
    {
        _ = _currentBoxes.RemoveAll(box => box.Id == id);
        _currentBoxes.Add(new BoxRecord { Id = id, Min = min, Max = max });
    }

    public void RegisterBoxRemoved(int id)
    {
        _ = _currentBoxes.RemoveAll(box => box.Id == id);
    }

    public void RegisterBoxesRemoved(IEnumerable<int> ids)
    {
        HashSet<int> toRemove = new(ids);
        _ = _currentBoxes.RemoveAll(box => toRemove.Contains(box.Id));
    }

    public List<BoxRecord> PopPreviousState()
    {
        if (!CanUndo)
        {
            return null;
        }


        List<BoxRecord> previous = _history.Pop();
        _currentBoxes.Clear();
        _currentBoxes.AddRange(previous);
        OnCanUndoChanged?.Invoke(CanUndo);
        return previous;
    }

    public void ClearHistory()
    {
        _history.Clear();
        _currentBoxes.Clear();
        OnCanUndoChanged?.Invoke(CanUndo);
    }
}
