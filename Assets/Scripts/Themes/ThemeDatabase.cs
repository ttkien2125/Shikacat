using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ThemeDatabase", menuName = "Shikacat/ThemeDatabase")]
public class ThemeDatabase : ScriptableObject
{
    [SerializeField] private List<CatTheme> _themes = new();

    public int Count => _themes != null ? _themes.Count : 0;

    public CatTheme GetTheme(int id)
    {
        return _themes != null && _themes.Count > 0
            ? _themes[Mathf.Abs(id) % _themes.Count]
            : default;
    }
}
