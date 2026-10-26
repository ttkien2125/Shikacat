using System;
using UnityEngine;

[Serializable]
public struct CatTheme
{
    public string name;
    public Sprite boxSprite;         // Panel_Box_[Color]
    public Sprite catSprite;         // Image_Cat_[Color]
    public Sprite catSolvedSprite;   // Image_Cat_[Color]EndGame
}