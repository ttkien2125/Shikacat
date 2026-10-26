using System.Collections.Generic;
using UnityEngine;

public static class ShikakuValidator
{
    private static readonly Dictionary<int, int> _cellCounts = new();

    public static bool TryGetSingleEnclosedNumber(
        Cell[,] cells,
        Vector2Int min,
        Vector2Int max,
        out int numberId
    )
    {
        int numberCells = 0;
        numberId = Cell.PreviewId;

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                if (cells[x, y].IsNumber)
                {
                    numberCells++;
                    numberId = cells[x, y].Id;
                }
            }
        }

        if (numberCells == 1)
        {
            return true;
        }

        numberId = Cell.PreviewId;
        return false;
    }

    public static bool CheckWin(Cell[,] cells, Level level)
    {
        if (cells == null || level == null)
        {
            return false;
        }

        _cellCounts.Clear();

        for (int x = 0; x < level.Rows; x++)
        {
            for (int y = 0; y < level.Cols; y++)
            {
                int id = cells[x, y].Id;
                if (id < 0)
                {
                    return false;
                }

                _ = _cellCounts.TryGetValue(id, out int count);
                _cellCounts[id] = count + 1;
            }
        }

        if (level.Clues != null)
        {
            foreach (Level.Clue clue in level.Clues)
            {
                Cell numberCell = cells[clue.Position.x, clue.Position.y];
                if (!_cellCounts.TryGetValue(numberCell.Id, out int count) ||
                    count != clue.Value)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
