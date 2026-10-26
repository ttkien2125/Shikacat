using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance { get; private set; }

    public static event Action OnLevelStarted;

    [Header("Levels")]
    [SerializeField] private List<Level> _levels = new();

    public Level CurrentLevel => (_levels != null && _levels.Count > 0)
        ? _levels[CurrentLevelIndex % _levels.Count]
        : null;

    public int CurrentLevelIndex { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void Init()
    {
        CurrentLevelIndex = PlayerPrefs.GetInt(GameConstants.Prefs.CurrentLevel, 0);
        if (_levels != null && _levels.Count > 0 && CurrentLevelIndex >= _levels.Count)
        {
            CurrentLevelIndex = 0;
        }

        NotifyLevelStarted();
    }

    public void AdvanceToNextLevel()
    {
        StopAllCoroutines();

        CurrentLevelIndex++;
        if (_levels != null && _levels.Count > 0 && CurrentLevelIndex >= _levels.Count)
        {
            CurrentLevelIndex = 0;
        }

        PlayerPrefs.SetInt(GameConstants.Prefs.CurrentLevel, CurrentLevelIndex);
        PlayerPrefs.Save();

        NotifyLevelStarted();
    }

    public void LoadLevelByIndex(int index)
    {
        if (_levels == null || _levels.Count == 0)
        {
            return;
        }


        StopAllCoroutines();
        CurrentLevelIndex = Mathf.Clamp(index, 0, _levels.Count - 1);
        PlayerPrefs.SetInt(GameConstants.Prefs.CurrentLevel, CurrentLevelIndex);
        PlayerPrefs.Save();

        NotifyLevelStarted();
    }

    public void RestartCurrentLevel()
    {
        StopAllCoroutines();
        NotifyLevelStarted();
    }

    private void NotifyLevelStarted()
    {
        Level level = CurrentLevel;
        if (level != null)
        {
            OnLevelStarted?.Invoke();
        }
    }
}
