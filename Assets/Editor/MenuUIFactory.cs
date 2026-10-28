using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class MenuUIFactory
{
    public static Sprite LoadSprite(string spriteName)
    {
        string[] folders = { "01_Branding", "02_Action_Buttons", "03_Feature_Buttons", "04_HUD_And_Panels", "06_VFX_And_Particles", "" };
        foreach (var f in folders)
        {
            string rel = string.IsNullOrEmpty(f) ? $"{spriteName}.png" : $"{f}/{spriteName}.png";
            string path = $"Assets/Resources/Sprites/GeneratedSprites/{rel}";
            Sprite spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (spr != null)
            {
                return spr;
            }
        }

        string[] guids = AssetDatabase.FindAssets($"{spriteName} t:Sprite");
        if (guids.Length > 0)
        {
            string p = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<Sprite>(p);
        }

        Debug.LogWarning($"[MenuUIFactory] Could not find Sprite '{spriteName}'!");
        return null;
    }

    public static GameObject CreateUIElement(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.layer = LayerMask.NameToLayer("UI");
        RectTransform rt = go.AddComponent<RectTransform>();
        if (parent != null)
        {
            rt.SetParent(parent, false);
        }
        return go;
    }

    public static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    public static GameObject CreateDecorImage(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = CreateUIElement(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = obj.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return obj;
    }

    public static Button CreateImageButton(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject obj = CreateUIElement(name, parent);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = obj.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        Button btn = obj.AddComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    public static void SetupTMP(TextMeshProUGUI tmp, TMP_FontAsset font, string text, float fontSize, TextAlignmentOptions align, Color color)
    {
        if (font != null)
        {
            tmp.font = font;
        }
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = color;
        tmp.raycastTarget = false;
    }
}

