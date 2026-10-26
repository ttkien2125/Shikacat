using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private SpriteRenderer _backgroundPrefab;
    [SerializeField] private Cell _cellPrefab;

    public Cell[,] Cells { get; private set; }
    private Transform _gridContainer;

    private readonly HashSet<int> _cachedOverlappingIds = new();

    public void ClearGrid()
    {
        if (_gridContainer != null)
        {
            for (int i = _gridContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_gridContainer.GetChild(i).gameObject);
            }
        }

        Cells = null;
    }

    public void SpawnGrid(Level level, float gridSize, ThemeDatabase themes)
    {
        ClearGrid();

        if (_gridContainer == null)
        {
            GameObject containerObj = new("GridContainer");
            containerObj.transform.SetParent(transform);
            _gridContainer = containerObj.transform;
        }

        SpriteRenderer background = Instantiate(_backgroundPrefab, _gridContainer);
        background.size = new Vector2(level.Rows + 0.05f, level.Cols + 0.05f) * gridSize;
        background.transform.position = gridSize * 0.5f * new Vector3(level.Rows, level.Cols, 0);

        Cells = new Cell[level.Rows, level.Cols];
        for (int x = 0; x < level.Rows; x++)
        {
            for (int y = 0; y < level.Cols; y++)
            {
                Vector2Int pos = new(x, y);
                bool isNumber = level.TryGetClue(pos, out Level.Clue clue);
                int regionId = isNumber ? clue.RegionId : Cell.EmptyId;
                int numberValue = isNumber ? clue.Value : 0;
                CatTheme theme = isNumber ? themes.GetTheme(regionId) : default;

                Cells[x, y] = Instantiate(_cellPrefab, _gridContainer);
                Cells[x, y].Init(isNumber, regionId, numberValue, theme);
                Cells[x, y].transform.position = new Vector3(x + 0.5f, y + 0.5f, 0) * gridSize;
            }
        }
    }

    public void SetRegion(Vector2Int min, Vector2Int max, int regionId)
    {
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Cells[x, y].Id = regionId;
            }
        }
    }

    public void CleanRegion(int id)
    {
        if (id < 0 || Cells == null)
        {
            return;
        }

        int rows = Cells.GetLength(0);
        int cols = Cells.GetLength(1);

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if (Cells[x, y].Id == id)
                {
                    Cells[x, y].Clear();
                }
            }
        }
    }

    public void SetAllCatsSolved(Level level)
    {
        if (level == null || level.Clues == null)
        {
            return;
        }

        foreach (Level.Clue clue in level.Clues)
        {
            Cells[clue.Position.x, clue.Position.y].SetSolved();
        }
    }

    public HashSet<int> GetOverlappingRegions(Vector2Int min, Vector2Int max, int ignoreId)
    {
        _cachedOverlappingIds.Clear();

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                int existingId = Cells[x, y].Id;
                if (existingId >= 0 && existingId != ignoreId)
                {
                    _ = _cachedOverlappingIds.Add(existingId);
                }
            }
        }

        return _cachedOverlappingIds;
    }

    public void ResetToInitialNumbers(Level level)
    {
        if (Cells == null || level == null)
        {
            return;
        }

        for (int x = 0; x < level.Rows; x++)
        {
            for (int y = 0; y < level.Cols; y++)
            {
                Cell cell = Cells[x, y];
                if (cell != null)
                {
                    cell.Clear();
                }
            }
        }
    }
}
