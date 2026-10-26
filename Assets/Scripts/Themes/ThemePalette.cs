using UnityEngine;

/// <summary>
/// Central palette defining colors and tints associated with each cat theme and box style.
/// </summary>
public static class ThemePalette
{
    public static Color32 GetThemeColor(string themeName)
    {
        return themeName switch
        {
            "BabyBlue"     => new Color32(98,  199, 245, 255),
            "Blue"         => new Color32(82,  145, 252, 255),
            "ElectricBlue" => new Color32(105, 82,  252, 255),
            "Green"        => new Color32(31,  218, 31,  255),
            "Lime"         => new Color32(150, 218, 40,  255),
            "Orange"       => new Color32(255, 175, 64,  255),
            "Pink"         => new Color32(243, 118, 237, 255),
            "Purple"       => new Color32(162, 92,  240, 255),
            "Red"          => new Color32(232, 97,  97,  255),
            "Teal"         => new Color32(60,  190, 173, 255),
            "WarmPink"     => new Color32(255, 147, 176, 255),
            "Yellow"       => new Color32(237, 237, 20,  255),
            _              => new Color32(200, 200, 200, 255)
        };
    }

    public static Color GetToyTint(string themeName, float alpha)
    {
        Color32 c = GetThemeColor(themeName);
        byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
        return new Color32(c.r, c.g, c.b, a);
    }
}

