using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Shikacat/Level")]
public class Level : ScriptableObject
{
    [Serializable]
    public struct Clue
    {
        public Vector2Int Position;
        public int Value;
        public int RegionId;

        public Clue(Vector2Int position, int value, int regionId)
        {
            Position = position;
            Value = value;
            RegionId = regionId;
        }
    }

    [Serializable]
    public struct SolutionBox
    {
        public int RegionId;
        public Vector2Int Min;
        public Vector2Int Max;

        public SolutionBox(int regionId, Vector2Int min, Vector2Int max)
        {
            RegionId = regionId;
            Min = min;
            Max = max;
        }
    }

    [Header("Grid Dimensions")]
    public int Rows = 5;
    public int Cols = 5;

    [Header("Tutorial Settings")]
    public bool IsTutorial = false;
    [TextArea(2, 4)]
    public string TutorialHint = "";
    public bool ShowGhostPreview = true;

    [Header("Puzzle Clues")]
    public List<Clue> Clues = new();

    [Header("Author Solution")]
    public List<SolutionBox> Solution = new();

    // Fast O(1) runtime lookup cache
    [NonSerialized] private Dictionary<Vector2Int, Clue> _clueLookup;

    public bool TryGetClue(Vector2Int pos, out Clue clue)
    {
        if (_clueLookup == null)
        {
            BuildLookupCache();
        }

        return _clueLookup.TryGetValue(pos, out clue);
    }

    public void InvalidateCache()
    {
        _clueLookup = null;
    }

    private void BuildLookupCache()
    {
        _clueLookup = new Dictionary<Vector2Int, Clue>(Clues != null ? Clues.Count : 0);
        if (Clues == null)
        {
            return;
        }


        foreach (Clue clue in Clues)
        {
            _clueLookup[clue.Position] = clue;
        }
    }

    private void OnEnable()
    {
        InvalidateCache();
    }

    private void OnValidate()
    {
        InvalidateCache();
    }
}
