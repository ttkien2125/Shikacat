using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinPopup : UIPopup
{
    [Header("Header Elements")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _completeText;
    [SerializeField] private Image _levelPillImage;
    [SerializeField] private Image _bannerImage;

    [Header("Artwork Elements")]
    [SerializeField] private Image _cardBgImage;
    [SerializeField] private Image _catFrontImage;

    [Header("Buttons")]
    [SerializeField] private Button _nextLevelButton;

    private Action _onNextLevel;

    private void Awake()
    {
        if (_nextLevelButton != null)
        {
            _nextLevelButton.onClick.RemoveAllListeners();
            _nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }
    }

    public void Show(int levelNumber, Action onNextLevel)
    {
        _onNextLevel = onNextLevel;

        if (_levelText != null)
        {
            if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null && LevelManager.Instance.CurrentLevel.IsTutorial)
            {
                _levelText.SetText("TUTORIAL {}", levelNumber);
            }
            else
            {
                _levelText.SetText("LEVEL {}", levelNumber);
            }
        }

        if (_nextLevelButton != null)
        {
            _nextLevelButton.interactable = true;
        }


        Open();
    }

    protected override void PlayCloseSound()
    {
        // WinPopup uses button click SFX on transition rather than popup close
    }

    private void OnNextLevelClicked()
    {
        if (_isClosing)
        {
            return;
        }


        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(AudioManager.SoundEffect.ButtonClick);
        }


        HapticsManager.Trigger();

        if (_nextLevelButton != null)
        {
            _nextLevelButton.interactable = false;
        }


        _onNextLevel?.Invoke();
    }
}
