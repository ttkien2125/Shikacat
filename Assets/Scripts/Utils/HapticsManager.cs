using UnityEngine;

public static class HapticsManager
{
    private static bool _isEnabled = true;

    static HapticsManager()
    {
        _isEnabled = PlayerPrefs.GetInt(GameConstants.Prefs.HapticsEnabled, 1) == 1;
    }

    public static bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            PlayerPrefs.SetInt(GameConstants.Prefs.HapticsEnabled, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Light tap feedback (button clicks, UI toggles, dragging cells).</summary>
    public static void Trigger()
    {
        if (!_isEnabled)
        {
            return;
        }

#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
    }
}