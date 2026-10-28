using UnityEngine;

/// <summary>
/// Single source of truth for persistent level progression, unlocked levels,
/// and completion states. Prevents already completed levels from getting locked out.
/// </summary>
public static class LevelProgress
{
    /// <summary>
    /// Index of the current level selected or being played.
    /// </summary>
    public static int CurrentLevelIndex
    {
        get => PlayerPrefs.GetInt(GameConstants.Prefs.CurrentLevel, 0);
        set
        {
            PlayerPrefs.SetInt(GameConstants.Prefs.CurrentLevel, Mathf.Max(0, value));
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Index of the highest level unlocked by the player.
    /// </summary>
    public static int HighestUnlockedIndex
    {
        get => GetHighestUnlockedLevel();
        set => SetHighestUnlockedLevel(value);
    }

    /// <summary>
    /// Total number of configured levels in the game.
    /// </summary>
    public static int TotalLevels
    {
        get
        {
            if (LevelCatalog.Instance != null && LevelCatalog.Instance.Count > 0)
            {
                return LevelCatalog.Instance.Count;
            }

            return GameConstants.Defaults.TotalLevels;
        }
    }

    /// <summary>
    /// Returns true if the specified level index has been completed.
    /// Automatically migrates any legacy progression.
    /// </summary>
    public static bool IsLevelCompleted(int levelIndex)
    {
        if (PlayerPrefs.GetInt(GameConstants.Prefs.LevelCompletedPrefix + levelIndex, 0) == 1)
        {
            return true;
        }

        // Migration: If player has already reached a higher level, all prior levels are marked completed
        int highest = GetHighestUnlockedLevel();
        if (levelIndex < highest)
        {
            SetLevelCompleted(levelIndex, true);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Marks a level as completed and updates the highest unlocked level accordingly.
    /// </summary>
    public static void SetLevelCompleted(int levelIndex, bool completed = true)
    {
        PlayerPrefs.SetInt(GameConstants.Prefs.LevelCompletedPrefix + levelIndex, completed ? 1 : 0);

        if (completed)
        {
            int currentHighest = PlayerPrefs.GetInt(GameConstants.Prefs.HighestUnlockedLevel, 0);
            if (levelIndex + 1 > currentHighest)
            {
                PlayerPrefs.SetInt(GameConstants.Prefs.HighestUnlockedLevel, levelIndex + 1);
            }
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Returns the highest level index unlocked for the player.
    /// </summary>
    public static int GetHighestUnlockedLevel()
    {
        int legacyCurrent = PlayerPrefs.GetInt(GameConstants.Prefs.CurrentLevel, 0);
        int storedHighest = PlayerPrefs.GetInt(GameConstants.Prefs.HighestUnlockedLevel, -1);

        if (storedHighest < 0)
        {
            storedHighest = legacyCurrent;
            PlayerPrefs.SetInt(GameConstants.Prefs.HighestUnlockedLevel, storedHighest);
            for (int i = 0; i < storedHighest; i++)
            {
                PlayerPrefs.SetInt(GameConstants.Prefs.LevelCompletedPrefix + i, 1);
            }
            PlayerPrefs.Save();
        }

        return Mathf.Max(storedHighest, legacyCurrent);
    }

    /// <summary>
    /// Updates the highest unlocked level index without allowing it to decrease.
    /// </summary>
    public static void SetHighestUnlockedLevel(int levelIndex)
    {
        int currentHighest = PlayerPrefs.GetInt(GameConstants.Prefs.HighestUnlockedLevel, 0);
        int highest = Mathf.Max(currentHighest, levelIndex);
        PlayerPrefs.SetInt(GameConstants.Prefs.HighestUnlockedLevel, highest);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Resets all level completion flags and unlocks for debugging.
    /// </summary>
    public static void ResetAllProgress()
    {
        int count = Mathf.Max(TotalLevels, 50);
        for (int i = 0; i < count; i++)
        {
            PlayerPrefs.DeleteKey(GameConstants.Prefs.LevelCompletedPrefix + i);
        }
        CurrentLevelIndex = 0;
        PlayerPrefs.SetInt(GameConstants.Prefs.HighestUnlockedLevel, 0);
        PlayerPrefs.Save();
    }
}
