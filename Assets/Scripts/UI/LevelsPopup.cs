using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelsPopup : UIPopup
{
    [SerializeField] private Transform _gridContainer;
    [SerializeField] private LevelItemUI _levelItemPrefab;
    [SerializeField] private int _totalLevels = 0;

    private readonly List<LevelItemUI> _items = new();
    private Action<string> _toastCallback;

    public int TotalLevels => _totalLevels > 0 ? _totalLevels : LevelProgress.TotalLevels;

    public void Init(Action<string> toastCallback)
    {
        _toastCallback = toastCallback;
        CollectExistingItems();
    }

    private void CollectExistingItems()
    {
        _items.Clear();
        if (_gridContainer != null)
        {
            var existing = _gridContainer.GetComponentsInChildren<LevelItemUI>(true);
            if (existing != null && existing.Length > 0)
            {
                _items.AddRange(existing);
            }
        }
    }

    public override void Open()
    {
        RefreshGrid();
        base.Open();
    }

    public void RefreshGrid()
    {
        if (_gridContainer == null)
        {
            return;
        }

        CollectExistingItems();

        int total = TotalLevels;
        int highestUnlocked = LevelProgress.HighestUnlockedIndex;

        // Instantiate additional items if needed
        if (_levelItemPrefab != null)
        {
            while (_items.Count < total)
            {
                LevelItemUI item = Instantiate(_levelItemPrefab, _gridContainer);
                _items.Add(item);
            }
        }

        int count = Mathf.Min(_items.Count, total);
        for (int i = 0; i < count; i++)
        {
            LevelState state;
            if (LevelProgress.IsLevelCompleted(i))
            {
                state = LevelState.Completed;
            }
            else if (i <= highestUnlocked)
            {
                state = LevelState.Next;
            }
            else
            {
                state = LevelState.Locked;
            }

            _items[i].gameObject.SetActive(true);
            _items[i].Setup(i, state, OnLevelSelected, _toastCallback);
        }

        // Hide any excess
        for (int i = count; i < _items.Count; i++)
        {
            _items[i].gameObject.SetActive(false);
        }
    }

    private void OnLevelSelected(int levelIndex)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(AudioManager.SoundEffect.ButtonClick);
        }

        LevelProgress.CurrentLevelIndex = levelIndex;

        Close(() =>
        {
            SceneManager.LoadScene(GameConstants.Scenes.Gameplay);
        });
    }
}
