using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum LevelState
{
    Completed,
    Next,
    Locked
}

public class LevelItemUI : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _background;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Image _tickIcon;
    [SerializeField] private Image _lockIcon;

    private int _levelIndex;
    private Action<int> _onClick;

    public void Setup(int levelIndex, LevelState state, Action<int> onClick, Action<string> onLockedClick = null)
    {
        _levelIndex = levelIndex;
        _onClick = onClick;

        int levelNumber = levelIndex + 1;
        if (_levelText != null)
        {
            _levelText.text = levelNumber.ToString();
        }

        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
        }

        switch (state)
        {
            case LevelState.Completed:
                // Completed: Grayed out with completion tick
                if (_background != null)
                {
                    _background.color = new Color(0.78f, 0.74f, 0.70f, 0.95f);
                }
                if (_levelText != null)
                {
                    _levelText.gameObject.SetActive(true);
                    _levelText.color = new Color(0.42f, 0.36f, 0.32f, 1f);
                }
                if (_tickIcon != null)
                {
                    _tickIcon.gameObject.SetActive(true);
                }
                if (_lockIcon != null)
                {
                    _lockIcon.gameObject.SetActive(false);
                }
                if (_button != null)
                {
                    _button.interactable = true;
                    _button.onClick.AddListener(() => _onClick?.Invoke(_levelIndex));
                }
                break;

            case LevelState.Next:
                // Next level: Highlighted (vibrant gold/amber)
                if (_background != null)
                {
                    _background.color = new Color(1f, 0.72f, 0.16f, 1f);
                }
                if (_levelText != null)
                {
                    _levelText.gameObject.SetActive(true);
                    _levelText.color = Color.white;
                }
                if (_tickIcon != null)
                {
                    _tickIcon.gameObject.SetActive(false);
                }
                if (_lockIcon != null)
                {
                    _lockIcon.gameObject.SetActive(false);
                }
                if (_button != null)
                {
                    _button.interactable = true;
                    _button.onClick.AddListener(() => _onClick?.Invoke(_levelIndex));
                }
                break;

            case LevelState.Locked:
                // Later levels: Locked with padlock icon
                if (_background != null)
                {
                    _background.color = new Color(0.55f, 0.50f, 0.46f, 0.65f);
                }
                if (_levelText != null)
                {
                    _levelText.gameObject.SetActive(false);
                }
                if (_tickIcon != null)
                {
                    _tickIcon.gameObject.SetActive(false);
                }
                if (_lockIcon != null)
                {
                    _lockIcon.gameObject.SetActive(true);
                }
                if (_button != null)
                {
                    _button.interactable = true;
                    _button.onClick.AddListener(() => onLockedClick?.Invoke($"Level {levelNumber} is locked!"));
                }
                break;
        }
    }
}

