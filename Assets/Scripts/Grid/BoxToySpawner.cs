using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static utility that decorates multi-cell boxes with decorative cat toys,
/// handling deterministic placement, scaling, masking, and theme tints.
/// </summary>
public static class BoxToySpawner
{
    public readonly struct Config
    {
        public readonly Sprite[] Sprites;
        public readonly int MaxToys;
        public readonly bool TintToTheme;
        public readonly float Alpha;
        public readonly float FurnitureScale;
        public readonly float LooseToyScale;

        public Config(Sprite[] sprites, int maxToys, bool tintToTheme, float alpha, float furnitureScale, float looseToyScale)
        {
            Sprites = sprites;
            MaxToys = maxToys;
            TintToTheme = tintToTheme;
            Alpha = alpha;
            FurnitureScale = furnitureScale;
            LooseToyScale = looseToyScale;
        }
    }

    private static readonly HashSet<string> LooseToyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Image_Ball2",
        "Image_Ball_Default",
        "Image_FishBones_Default",
        "Image_FishToy_Default",
        "Image_WhoolBall_Default"
    };

    public static void ClearToys(GameObject boxObj)
    {
        Transform existingContainer = boxObj.transform.Find("Toys");
        if (existingContainer != null)
        {
            UnityEngine.Object.Destroy(existingContainer.gameObject);
        }
    }

    public static void SpawnOrUpdateToys(
        GameObject boxObj,
        int id,
        Vector2Int min,
        Vector2Int max,
        CatTheme theme,
        float gridSize,
        Cell[,] cells,
        Config config)
    {
        ClearToys(boxObj);

        int widthCount = max.x - min.x + 1;
        int heightCount = max.y - min.y + 1;
        if (widthCount <= 1 && heightCount <= 1)
        {
            return;
        }

        Sprite[] sprites = config.Sprites;
        if (sprites == null || sprites.Length == 0)
        {
            sprites = Resources.LoadAll<Sprite>("Sprites/Toys");
            if (sprites == null || sprites.Length == 0)
            {
                return;
            }

        }

        // Collect all bare (non-cat) tile positions inside this box
        List<Vector2Int> bareTiles = new();
        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                if (cells != null && cells[x, y] != null && cells[x, y].IsNumber && cells[x, y].Id == id)
                {
                    continue; // Skip the cell with the cat
                }
                bareTiles.Add(new Vector2Int(x, y));
            }
        }

        if (bareTiles.Count == 0)
        {
            return;
        }


        GameObject containerObj = new("Toys");
        containerObj.transform.SetParent(boxObj.transform, false);

        // Deterministic pseudo-random seed based on id and position
        int seed = (id * 7919) + (min.x * 37) + (min.y * 17) + (widthCount * 13) + (heightCount * 101);
        System.Random rng = new(seed);

        int maxAllowed = Mathf.Min(config.MaxToys, bareTiles.Count);
        int toyCount = rng.Next(1, maxAllowed + 1);

        // Fisher-Yates shuffle
        for (int i = bareTiles.Count - 1; i > 0; i--)
        {
            int swapIdx = rng.Next(i + 1);
            (bareTiles[i], bareTiles[swapIdx]) = (bareTiles[swapIdx], bareTiles[i]);
        }

        Color toyColor = config.TintToTheme ? ThemePalette.GetToyTint(theme.name, config.Alpha) : Color.white;
        List<Sprite> pool = new(sprites);

        for (int i = 0; i < toyCount; i++)
        {
            Vector2Int tilePos = bareTiles[i];
            if (pool.Count == 0)
            {
                pool.AddRange(sprites);
            }


            int idx = rng.Next(pool.Count);
            Sprite toySprite = pool[idx];
            pool.RemoveAt(idx);
            if (toySprite == null)
            {
                continue;
            }


            GameObject toyObj = new($"Toy_{i}_{toySprite.name}");
            toyObj.transform.SetParent(containerObj.transform, false);
            toyObj.transform.position = new Vector3((tilePos.x + 0.5f) * gridSize, (tilePos.y + 0.5f) * gridSize, 0f);

            SpriteRenderer sr = toyObj.AddComponent<SpriteRenderer>();
            sr.sprite = toySprite;
            sr.color = toyColor;
            sr.sortingOrder = SortingOrderConfig.GetToyOrder(id);
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

            bool isLoose = IsLooseToy(toySprite.name);
            float scaleMultiplier = isLoose ? config.LooseToyScale : config.FurnitureScale;
            float maxDim = Mathf.Max(toySprite.bounds.size.x, toySprite.bounds.size.y);
            float scale = maxDim > 0f ? gridSize * scaleMultiplier / maxDim : 1f;

            float angle = isLoose ? (float)((rng.NextDouble() * 70.0) - 35.0) : 0f;
            toyObj.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            toyObj.transform.localScale = new Vector3(scale, scale, 1f);
            sr.flipX = !isLoose && rng.NextDouble() > 0.5;
        }
    }

    public static bool IsLooseToy(string spriteName)
    {
        return LooseToyNames.Contains(spriteName) ||
               spriteName.IndexOf("Fish", StringComparison.OrdinalIgnoreCase) >= 0 ||
               spriteName.IndexOf("Ball", StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
