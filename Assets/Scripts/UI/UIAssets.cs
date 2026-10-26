using System.Collections.Generic;
using UnityEngine;

public static class UIAssets
{
    // Color Palette matching the game design
    public static readonly Color ToggleGreen = new Color32(0x4C, 0xD9, 0x36, 0xFF);        // #4CD936 (Toggle active)
    public static readonly Color ToggleGray = new Color32(0xD8, 0xD0, 0xC5, 0xFF);         // #D8D0C5 (Toggle inactive)

    private static readonly Dictionary<string, Sprite> _spriteCache = new();

    public static Sprite GetSprite(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
        {
            return null;
        }


        if (_spriteCache.TryGetValue(resourceName, out Sprite cached) && cached != null)
        {

            return cached;
        }


        string[] searchPaths = {
            "Sprites/UI/" + resourceName,
            "UI/" + resourceName,
            "Sprites/GeneratedSprites/" + resourceName,
            "Sprites/" + resourceName,
            resourceName
        };

        foreach (string path in searchPaths)
        {
            Sprite loaded = Resources.Load<Sprite>(path);
            if (loaded != null)
            {
                _spriteCache[resourceName] = loaded;
                return loaded;
            }
        }

        return null;
    }
}
