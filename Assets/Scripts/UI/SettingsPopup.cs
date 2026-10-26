using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPopup : UIPopup
{
    public event Action OnRestartRequested;

    [SerializeField] private Button _closeButton;
    [SerializeField] private ToggleSwitch _musicToggle;
    [SerializeField] private ToggleSwitch _soundsToggle;
    [SerializeField] private ToggleSwitch _hapticsToggle;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _termsButton;
    [SerializeField] private Button _privacyButton;

    private Action<string> _toastCallback;

    public void Init(Action<string> toastCallback)
    {
        _toastCallback = toastCallback;
        SetupListeners();
    }

    private void SetupListeners()
    {
        // Hook up close button
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(Close);
        }

        // Hook up toggles with PlayerPrefs persistence
        bool musicOn = PlayerPrefs.GetInt(GameConstants.Prefs.MusicEnabled, 1) == 1;
        bool soundsOn = PlayerPrefs.GetInt(GameConstants.Prefs.SoundsEnabled, 1) == 1;
        bool hapticsOn = PlayerPrefs.GetInt(GameConstants.Prefs.HapticsEnabled, 1) == 1;

        if (_musicToggle != null)
        {
            _musicToggle.SetState(musicOn, animate: false);
            _musicToggle.OnValueChanged += val =>
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetMusicEnabled(val);
                }


                _toastCallback?.Invoke(val ? "Music: ON" : "Music: OFF");
            };
        }

        if (_soundsToggle != null)
        {
            _soundsToggle.SetState(soundsOn, animate: false);
            _soundsToggle.OnValueChanged += val =>
            {
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SetSoundsEnabled(val);
                }


                _toastCallback?.Invoke(val ? "Sounds: ON" : "Sounds: OFF");
            };
        }

        if (_hapticsToggle != null)
        {
            _hapticsToggle.SetState(hapticsOn, animate: false);
            _hapticsToggle.OnValueChanged += val =>
            {
                HapticsManager.IsEnabled = val;
                _toastCallback?.Invoke(val ? "Haptics: ON" : "Haptics: OFF");
            };
        }

        // Restart button
        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(OnRestartClicked);
        }

        // Stubs: T&C and Privacy
        if (_termsButton != null)
        {
            _termsButton.onClick.RemoveAllListeners();
            _termsButton.onClick.AddListener(() =>
            {
                _toastCallback?.Invoke("Terms & Conditions (Stub)");
            });
        }

        if (_privacyButton != null)
        {
            _privacyButton.onClick.RemoveAllListeners();
            _privacyButton.onClick.AddListener(() =>
            {
                _toastCallback?.Invoke("Privacy Policy (Stub)");
            });
        }
    }

    private void OnRestartClicked()
    {
        Close();
        OnRestartRequested?.Invoke();
        _toastCallback?.Invoke("Level Restarted!");
    }
}
