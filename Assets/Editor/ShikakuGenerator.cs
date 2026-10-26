using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class ShikakuGenerator
{
    public struct GenerationResult
    {
        public int Rows;
        public int Cols;
        public int[,] Grid;
        public bool[,] Clues;
        public List<Level.Clue> ClueList;
        public List<Level.SolutionBox> SolutionList;
    }

    private struct CutOption
    {
        public bool AlongWidth;
        public int Cut;
        public int Weight;
    }

    /// <summary>
    /// Generates a valid Shikaku grid partition according to design constraints:
    /// - Clue count limited to around 20% of the grid.
    /// - Diverse area distribution with minimal 2- and 3-area pieces.
    /// - Strictly no 1-area pieces (all areas >= 2).
    /// </summary>
    public static GenerationResult Generate(int rows, int cols, int? targetClueCount = null, int? seed = null)
    {
        rows = Mathf.Clamp(rows, 2, 20);
        cols = Mathf.Clamp(cols, 2, 20);

        int totalCells = rows * cols;
        int targetCount = targetClueCount.HasValue
            ? Mathf.Clamp(targetClueCount.Value, 2, totalCells / 2)
            : Mathf.Max(2, Mathf.RoundToInt(totalCells * 0.19f));

        System.Random rand = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

        List<RectInt> rects = new() { new RectInt(0, 0, rows, cols) };
        HashSet<int> existingAreas = new();

        int safetyLimit = 600;
        int iteration = 0;

        while (rects.Count < targetCount && iteration++ < safetyLimit)
        {
            // Find candidates that can be split such that BOTH sub-rectangles have area >= 2 (no area 1!)
            List<int> candidates = new();
            for (int i = 0; i < rects.Count; i++)
            {
                RectInt r = rects[i];
                bool canSplitW = (r.width >= 2 && (r.height >= 2 || r.width >= 4));
                bool canSplitH = (r.height >= 2 && (r.width >= 2 || r.height >= 4));
                if (canSplitW || canSplitH)
                {
                    candidates.Add(i);
                }
            }

            if (candidates.Count == 0) break;

            // Sort candidates by area descending to prioritize splitting larger rectangles
            candidates.Sort((a, b) =>
            {
                int areaA = rects[a].width * rects[a].height;
                int areaB = rects[b].width * rects[b].height;
                return areaB.CompareTo(areaA);
            });

            int pickLimit = Mathf.Max(1, Mathf.RoundToInt(candidates.Count * 0.4f));
            int targetIdx = candidates[rand.Next(pickLimit)];
            RectInt target = rects[targetIdx];

            existingAreas.Clear();
            foreach (var r in rects)
            {
                existingAreas.Add(r.width * r.height);
            }

            List<CutOption> cuts = new();

            // Evaluate cuts along width
            if (target.width >= 2)
            {
                for (int c = 1; c < target.width; c++)
                {
                    int a1 = c * target.height;
                    int a2 = (target.width - c) * target.height;
                    // Strict Rule: No 1-area regions (both must be >= 2)
                    if (a1 >= 2 && a2 >= 2)
                    {
                        int weight = 10;
                        // Penalize creating 2 or 3 area pieces to favor richer variety
                        if (a1 <= 3 || a2 <= 3) weight = 2;
                        // Reward creating unique, novel area sizes
                        if (!existingAreas.Contains(a1)) weight += 8;
                        if (!existingAreas.Contains(a2)) weight += 8;
                        cuts.Add(new CutOption { AlongWidth = true, Cut = c, Weight = weight });
                    }
                }
            }

            // Evaluate cuts along height
            if (target.height >= 2)
            {
                for (int c = 1; c < target.height; c++)
                {
                    int a1 = target.width * c;
                    int a2 = target.width * (target.height - c);
                    // Strict Rule: No 1-area regions (both must be >= 2)
                    if (a1 >= 2 && a2 >= 2)
                    {
                        int weight = 10;
                        if (a1 <= 3 || a2 <= 3) weight = 2;
                        if (!existingAreas.Contains(a1)) weight += 8;
                        if (!existingAreas.Contains(a2)) weight += 8;
                        cuts.Add(new CutOption { AlongWidth = false, Cut = c, Weight = weight });
                    }
                }
            }

            if (cuts.Count == 0) break;

            // Weighted random selection
            int totalWeight = 0;
            foreach (var k in cuts) totalWeight += k.Weight;
            int roll = rand.Next(totalWeight);
            CutOption chosen = cuts[0];
            int accum = 0;
            foreach (var k in cuts)
            {
                accum += k.Weight;
                if (roll < accum)
                {
                    chosen = k;
                    break;
                }
            }

            RectInt r1, r2;
            if (chosen.AlongWidth)
            {
                r1 = new RectInt(target.x, target.y, chosen.Cut, target.height);
                r2 = new RectInt(target.x + chosen.Cut, target.y, target.width - chosen.Cut, target.height);
            }
            else
            {
                r1 = new RectInt(target.x, target.y, target.width, chosen.Cut);
                r2 = new RectInt(target.x, target.y + chosen.Cut, target.width, target.height - chosen.Cut);
            }

            rects[targetIdx] = r1;
            rects.Add(r2);
        }

        // Build data structures
        int[,] grid = new int[rows, cols];
        bool[,] clueGrid = new bool[rows, cols];
        List<Level.Clue> clues = new();
        List<Level.SolutionBox> solutions = new();

        for (int i = 0; i < rects.Count; i++)
        {
            RectInt r = rects[i];
            int regionId = i;
            int area = r.width * r.height;

            for (int x = r.x; x < r.x + r.width; x++)
            {
                for (int y = r.y; y < r.y + r.height; y++)
                {
                    grid[x, y] = regionId;
                }
            }

            // Pick a clue position inside this rectangle
            int clueX = r.x + rand.Next(r.width);
            int clueY = r.y + rand.Next(r.height);
            clueGrid[clueX, clueY] = true;

            clues.Add(new Level.Clue(new Vector2Int(clueX, clueY), area, regionId));
            solutions.Add(new Level.SolutionBox(
                regionId,
                new Vector2Int(r.x, r.y),
                new Vector2Int(r.x + r.width - 1, r.y + r.height - 1)
            ));
        }

        return new GenerationResult
        {
            Rows = rows,
            Cols = cols,
            Grid = grid,
            Clues = clueGrid,
            ClueList = clues,
            SolutionList = solutions
        };
    }

    /// <summary>
    /// Creates a Level ScriptableObject asset from generation result.
    /// </summary>
    public static Level CreateLevelAsset(GenerationResult genResult, string assetPath)
    {
        string directory = Path.GetDirectoryName(assetPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        Level level = AssetDatabase.LoadAssetAtPath<Level>(assetPath);
        if (level == null)
        {
            level = ScriptableObject.CreateInstance<Level>();
            AssetDatabase.CreateAsset(level, assetPath);
        }

        level.Rows = genResult.Rows;
        level.Cols = genResult.Cols;
        level.Clues = new List<Level.Clue>(genResult.ClueList);
        level.Solution = new List<Level.SolutionBox>(genResult.SolutionList);

        EditorUtility.SetDirty(level);
        return level;
    }

    /// <summary>
    /// Generates 20 levels following the guidelines:
    /// - ~20% clue density
    /// - High area diversity (4, 5, 6, 8, 9, 10, 12, etc.)
    /// - Zero 1-area regions (all >= 2)
    /// </summary>
    public static List<Level> BatchGenerateLevels(string targetFolder, int count = 20)
    {
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        List<Level> generated = new();

        for (int i = 1; i <= count; i++)
        {
            int rows, cols, targetClues;

            // Progressive difficulty curve
            if (i <= 5)
            {
                rows = 5;
                cols = 5;
                targetClues = 5; // exactly 20.0%
            }
            else if (i <= 10)
            {
                rows = 6;
                cols = 6;
                targetClues = 7; // 19.4%
            }
            else if (i <= 14)
            {
                rows = 7;
                cols = 7;
                targetClues = 9; // 18.4%
            }
            else if (i <= 17)
            {
                rows = 8;
                cols = 8;
                targetClues = 12; // 18.8%
            }
            else
            {
                rows = 10;
                cols = 10;
                targetClues = 18; // 18.0%
            }

            GenerationResult result = Generate(rows, cols, targetClues, seed: i * 7919 + 101);
            string fileName = $"Level_{i:D2}.asset";
            string assetPath = Path.Combine(targetFolder, fileName).Replace('\\', '/');

            Level level = CreateLevelAsset(result, assetPath);
            generated.Add(level);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return generated;
    }
}
