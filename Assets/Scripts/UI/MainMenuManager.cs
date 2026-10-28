using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls the Main Menu UI, player stats display, idle breathing/bobbing animations,
/// sound toggles, and scene transitions to Gameplay.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Main Navigation")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _dailyChallengeButton;
    [SerializeField] private Button _levelsButton;
    [SerializeField] private Button _shopButton;
    [SerializeField] private Button _skinsButton;
    [SerializeField] private Button _leaderboardButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _soundButton;
    [SerializeField] private Button _noAdsButton;

    [Header("Displays & Counters")]
    [SerializeField] private TextMeshProUGUI _currentLevelText;
    [SerializeField] private TextMeshProUGUI _coinsText;
    [SerializeField] private TextMeshProUGUI _heartsText;

    [Header("Animated Elements")]
    [SerializeField] private RectTransform _logoTransform;
    [SerializeField] private RectTransform _mascotTransform;
    [SerializeField] private RectTransform _playButtonTransform;

    [Header("Popups & Notifications")]
    [SerializeField] private SettingsPopup _settingsPopup;
    [SerializeField] private LevelsPopup _levelsPopup;
    [SerializeField] private GameObject _toastObj;
    [SerializeField] private TextMeshProUGUI _toastText;
    [SerializeField] private CanvasGroup _toastCanvasGroup;

    private Coroutine _toastCoroutine;
    private static readonly WaitForSecondsRealtime _toastWait = new(1.8f);

    private Vector2 _mascotBasePos;
    private Vector2 _logoBasePos;
    private Vector3 _playButtonBaseScale;
    private bool _isStartingGame;

    private void Awake()
    {
        if (_mascotTransform != null)
        {
            _mascotBasePos = _mascotTransform.anchoredPosition;
        }


        if (_logoTransform != null)
        {
            _logoBasePos = _logoTransform.anchoredPosition;
        }

        _playButtonBaseScale = _playButtonTransform != null
            ? _playButtonTransform.localScale
            : _playButton != null ? _playButton.transform.localScale : Vector3.one;

        SetupButtons();
    }

    private void Start()
    {
        UpdatePlayerStats();

        if (_settingsPopup != null)
        {
            _settingsPopup.Init(ShowToast);
            _settingsPopup.SetRestartVisible(false);
            _settingsPopup.gameObject.SetActive(false);
        }

        if (_levelsPopup != null)
        {
            _levelsPopup.Init(ShowToast);
            _levelsPopup.gameObject.SetActive(false);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBgm();
        }

        _ = StartCoroutine(IdleFloatingRoutine());
    }

    private void SetupButtons()
    {
        if (_playButton != null)
        {
            _playButton.onClick.AddListener(OnPlayClicked);
        }


        if (_dailyChallengeButton != null)
        {
            _dailyChallengeButton.onClick.AddListener(() =>
            {
                PlayClickSfx();
                ShowToast("Daily Challenge unlocks at Level 5!");
            });
        }


        if (_levelsButton != null)
        {
            _levelsButton.onClick.AddListener(OnLevelsClicked);
        }


        if (_shopButton != null)
        {
            _shopButton.onClick.AddListener(() =>
            {
                PlayClickSfx();
                ShowToast("Cat Shop coming soon!");
            });
        }

        if (_skinsButton != null)
        {
            _skinsButton.onClick.AddListener(() =>
            {
                PlayClickSfx();
                ShowToast("Cat Wardrobe coming soon!");
            });
        }

        if (_leaderboardButton != null)
        {
            _leaderboardButton.onClick.AddListener(() =>
            {
                PlayClickSfx();
                ShowToast("Leaderboards coming soon!");
            });
        }

        if (_noAdsButton != null)
        {
            _noAdsButton.onClick.AddListener(() =>
            {
                PlayClickSfx();
                ShowToast("No Ads feature coming soon!");
            });
        }

        if (_settingsButton != null)
        {
            _settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        if (_soundButton != null)
        {
            _soundButton.onClick.AddListener(OnSoundToggleClicked);
        }

    }

    private void UpdatePlayerStats()
    {
        int levelIndex = LevelProgress.GetHighestUnlockedLevel();
        if (_currentLevelText != null)
        {
            _currentLevelText.SetText("LEVEL {0}", levelIndex + 1);
        }

        if (_coinsText != null)
        {
            int coins = PlayerPrefs.GetInt(GameConstants.Prefs.Coins, GameConstants.Defaults.StartingCoins);
            _coinsText.text = coins.ToString("N0");
        }

        if (_heartsText != null)
        {
            _heartsText.text = GameConstants.Defaults.Hearts;
        }
    }

    private void OnPlayClicked()
    {
        if (_isStartingGame)
        {
            return;
        }

        _isStartingGame = true;

        PlayClickSfx();

        int current = LevelProgress.CurrentLevelIndex;
        if (LevelProgress.IsLevelCompleted(current))
        {
            LevelProgress.CurrentLevelIndex = LevelProgress.HighestUnlockedIndex;
        }

        _ = StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        if (_playButtonTransform != null)
        {
            yield return UIAnimationHelper.PunchScale(_playButtonTransform, _playButtonBaseScale, 0.12f, 0.16f);
        }

        SceneManager.LoadScene(GameConstants.Scenes.Gameplay);
    }

    private void OnLevelsClicked()
    {
        PlayClickSfx();
        if (_levelsPopup != null)
        {
            _levelsPopup.Open();
        }
        else
        {
            int lvl = LevelProgress.GetHighestUnlockedLevel() + 1;
            ShowToast($"Current progress: Level {lvl}");
        }
    }

    private void OnSettingsClicked()
    {
        PlayClickSfx();
        if (_settingsPopup != null)
        {
            _settingsPopup.Open();
        }
        else
        {
            ShowToast("Settings popup opening...");
        }
    }

    private void OnSoundToggleClicked()
    {
        PlayClickSfx();
        if (AudioManager.Instance != null)
        {
            bool current = PlayerPrefs.GetInt(GameConstants.Prefs.SoundsEnabled, 1) == 1;
            bool newState = !current;
            AudioManager.Instance.SetSoundsEnabled(newState);
            AudioManager.Instance.SetMusicEnabled(newState);
            ShowToast(newState ? "Sound: ON" : "Sound: OFF");
        }
    }

    private void PlayClickSfx()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(AudioManager.SoundEffect.ButtonClick);
        }


        HapticsManager.Trigger();
    }

    private IEnumerator IdleFloatingRoutine()
    {
        float timer = 0f;

        while (true)
        {
            timer += Time.unscaledDeltaTime;

            // Mascot gentle breathing and subtle bobbing
            if (_mascotTransform != null)
            {
                float bobY = Mathf.Sin(timer * 2.5f) * 8f;
                float breath = 1f + (Mathf.Sin(timer * 3f) * 0.02f);
                _mascotTransform.anchoredPosition = _mascotBasePos + new Vector2(0f, bobY);
                _mascotTransform.localScale = new Vector3(breath, breath, 1f);
            }

            // Logo subtle float
            if (_logoTransform != null)
            {
                float logoBob = Mathf.Sin(timer * 1.8f) * 5f;
                _logoTransform.anchoredPosition = _logoBasePos + new Vector2(0f, logoBob);
            }

            // Play button rhythmic heartbeat pulse
            if (_playButtonTransform != null && !_isStartingGame)
            {
                float pulse = 1f + (Mathf.Sin(timer * 4.0f) * 0.035f);
                _playButtonTransform.localScale = _playButtonBaseScale * pulse;
            }

            yield return null;
        }
    }

    public void ShowToast(string message)
    {
        if (_toastObj == null || _toastText == null || _toastCanvasGroup == null)
        {
            return;
        }


        if (_toastCoroutine != null)
        {
            StopCoroutine(_toastCoroutine);
        }


        _toastCoroutine = StartCoroutine(ToastRoutine(message));
    }

    private IEnumerator ToastRoutine(string message)
    {
        _toastText.text = message;
        _toastObj.SetActive(true);

        yield return UIAnimationHelper.FadeCanvasGroup(_toastCanvasGroup, 0f, 1f, 0.15f);
        yield return _toastWait;
        yield return UIAnimationHelper.FadeCanvasGroup(_toastCanvasGroup, 1f, 0f, 0.15f);

        _toastCanvasGroup.alpha = 0f;
        _toastObj.SetActive(false);
    }
}

