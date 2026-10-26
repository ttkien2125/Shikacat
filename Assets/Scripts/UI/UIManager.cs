using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public static event Action OnRestartRequested;

    [Header("Top Header")]
    [SerializeField] private TextMeshProUGUI _levelTitleText;
    [SerializeField] private Button _settingsButton;

    [Header("Bottom Boosters")]
    [SerializeField] private BoosterButton _hintBooster;

    [Header("Popups & Notifications")]
    [SerializeField] private SettingsPopup _settingsPopup;
    [SerializeField] private WinPopup _winPopup;
    [SerializeField] private GameObject _toastObj;
    [SerializeField] private TextMeshProUGUI _toastText;
    [SerializeField] private CanvasGroup _toastCanvasGroup;
    [SerializeField] private LoadingScreen _loadingScreen;
    [SerializeField] private float _loadingDuration = 0.5f;

    private Coroutine _toastCoroutine;
    private static readonly WaitForSecondsRealtime _toastWait = new(1.6f);

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        _ = EnsureLoadingScreen();
    }

    public LoadingScreen EnsureLoadingScreen()
    {
        if (_loadingScreen == null)
        {
            _loadingScreen = GetComponentInChildren<LoadingScreen>(true)
                ?? FindObjectOfType<LoadingScreen>(true);
        }
        return _loadingScreen;
    }

    private void Start()
    {
        // Wire up buttons
        if (_settingsButton != null && _settingsPopup != null)
        {
            _settingsButton.onClick.RemoveAllListeners();
            _settingsButton.onClick.AddListener(() => _settingsPopup.Open());
        }

        if (_hintBooster != null)
        {
            UpdateHintBadge();
            _hintBooster.Bind(() =>
            {
                if (RemainingHints <= 0)
                {
                    ShowToast("No hints remaining!");
                    return;
                }
                if (GameManager.Instance != null)
                {
                    bool used = GameManager.Instance.UseHint();
                    if (used)
                    {
                        RemainingHints--;
                        ShowToast("Hint applied!");
                    }
                    else
                    {
                        ShowToast("All placed boxes are already correct!");
                    }
                }
            });
        }

        if (_settingsPopup != null)
        {
            _settingsPopup.Init(ShowToast);
            _settingsPopup.OnRestartRequested += HandleRestartRequested;
        }

        if (_winPopup != null)
        {
            _winPopup.gameObject.SetActive(false);
        }

        _ = EnsureLoadingScreen();

        // Subscribe to game events
        LevelManager.OnLevelStarted += HandleLevelStarted;
        GameManager.OnGameWon += ShowWinPopup;

        // Initialize display
        UpdateLevelTitle();
    }

    private void OnDestroy()
    {
        GameManager.OnGameWon -= ShowWinPopup;
        LevelManager.OnLevelStarted -= HandleLevelStarted;

        if (_settingsPopup != null)
        {
            _settingsPopup.OnRestartRequested -= HandleRestartRequested;
        }
    }

    private int RemainingHints
    {
        get => PlayerPrefs.GetInt(GameConstants.Prefs.HintCount, 50);
        set
        {
            PlayerPrefs.SetInt(GameConstants.Prefs.HintCount, Mathf.Max(0, value));
            PlayerPrefs.Save();
            UpdateHintBadge();
        }
    }

    private void UpdateHintBadge()
    {
        if (_hintBooster != null)
        {
            int count = RemainingHints;
            _hintBooster.SetBadge(count.ToString(), visible: true);
            _hintBooster.SetInteractable(count > 0);
        }
    }

    private void HandleRestartRequested()
    {
        LoadingScreen ls = EnsureLoadingScreen();
        if (ls != null)
        {
            ls.ShowTransition(
                () => OnRestartRequested?.Invoke(),
                customHoldDuration: _loadingDuration
            );
        }
        else
        {
            OnRestartRequested?.Invoke();
        }
    }

    private void HandleLevelStarted()
    {
        UpdateLevelTitle();
    }

    public void UpdateLevelTitle()
    {
        if (LevelManager.Instance != null && LevelManager.Instance.CurrentLevel != null)
        {
            Level current = LevelManager.Instance.CurrentLevel;
            if (current.IsTutorial)
            {
                int tutorialNum = LevelManager.Instance.CurrentLevelIndex + 1;
                _levelTitleText.SetText("Tutorial {}", tutorialNum);
                return;
            }
        }

        int levelIndex = LevelManager.Instance != null
            ? LevelManager.Instance.CurrentLevelIndex
            : PlayerPrefs.GetInt(GameConstants.Prefs.CurrentLevel, 0);
        _levelTitleText.SetText("Level {0}", levelIndex + 1);
    }


    public void ShowWinPopup(int levelNumber, Action onNextLevel)
    {
        void transitionNextLevel()
        {
            LoadingScreen ls = EnsureLoadingScreen();
            if (ls != null)
            {
                ls.ShowTransition(() =>
                {
                    if (_winPopup != null)
                    {
                        _winPopup.gameObject.SetActive(false);
                    }
                    onNextLevel?.Invoke();
                }, customHoldDuration: _loadingDuration);
            }
            else
            {
                Debug.LogWarning("[UIManager] LoadingScreen unavailable during win transition. Advancing directly.");
                if (_winPopup != null)
                {
                    _winPopup.Close(() => onNextLevel?.Invoke());
                }
                else
                {
                    onNextLevel?.Invoke();
                }
            }
        }

        if (_winPopup != null)
        {
            _winPopup.Show(levelNumber, transitionNextLevel);
        }
        else
        {
            transitionNextLevel();
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

        // Fade in
        yield return UIAnimationHelper.FadeCanvasGroup(_toastCanvasGroup, 0f, 1f, 0.15f);

        yield return _toastWait;

        // Fade out
        yield return UIAnimationHelper.FadeCanvasGroup(_toastCanvasGroup, 1f, 0f, 0.15f);

        _toastCanvasGroup.alpha = 0f;
        _toastObj.SetActive(false);
    }
}