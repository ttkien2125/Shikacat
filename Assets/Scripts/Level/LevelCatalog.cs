using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelCatalog", menuName = "Shikacat/LevelCatalog")]
public class LevelCatalog : ScriptableObject
{
    private static LevelCatalog _instance;

    public static LevelCatalog Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Resources.Load<LevelCatalog>("LevelCatalog");
            }
            return _instance;
        }
    }

    [Header("Level Sequence")]
    [SerializeField] private List<Level> _levels = new();

    public IReadOnlyList<Level> Levels => _levels;
    public int Count => _levels != null ? _levels.Count : 0;
    public int TotalLevels => Count;

    public Level GetLevel(int index)
    {
        if (_levels == null || _levels.Count == 0)
        {
            return null;
        }

        return _levels[Mathf.Clamp(index, 0, _levels.Count - 1)];
    }
}

