using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public enum SoundEffect
    {
        ButtonClick,
        PopupOpen,
        PopupClose,
        BoxPlaced,
        BoxRemoved,
        LevelCompleted
    }

    private AudioSource _musicSource;
    private AudioSource _sfxSource;

    [Header("Background Music")]
    [Tooltip("Maps to SC_BGM_01.ogg")]
    [SerializeField] private AudioClip _bgmClip;

    [Header("Sound Effect Clips")]
    [Tooltip("Maps to SC_SFX_UI_Button_01.ogg")]
    [SerializeField] private AudioClip _buttonClickClip;

    [Tooltip("Maps to SC_SFX_UI_Popup_Open_01.ogg")]
    [SerializeField] private AudioClip _popupOpenClip;

    [Tooltip("Maps to SC_SFX_UI_Popup_Close_01.ogg")]
    [SerializeField] private AudioClip _popupCloseClip;

    [Tooltip("Maps to SC_SFX_Gameplay_Box_Created_01.ogg")]
    [SerializeField] private AudioClip _boxPlacedClip;

    [Tooltip("Maps to SC_SFX_UI_Button_01.ogg or remove click")]
    [SerializeField] private AudioClip _boxRemovedClip;

    [Tooltip("Maps to SC_SFX_Gameplay_AllBoxesCreated_01.ogg or SC_SFX_UI_Win_01.ogg")]
    [SerializeField] private AudioClip _levelCompletedClip;

    private bool _musicEnabled = true;
    private bool _soundsEnabled = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitAudioSources();
        LoadSettings();
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        if (_musicEnabled && _bgmClip != null)
        {
            PlayBgm();
        }
    }

    private void InitAudioSources()
    {
        if (_musicSource == null)
        {
            _musicSource = gameObject.AddComponent<AudioSource>();
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
        }

        if (_sfxSource == null)
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
        }
    }

    private void LoadSettings()
    {
        _musicEnabled = PlayerPrefs.GetInt(GameConstants.Prefs.MusicEnabled, 1) == 1;
        _soundsEnabled = PlayerPrefs.GetInt(GameConstants.Prefs.SoundsEnabled, 1) == 1;

        _musicSource.mute = !_musicEnabled;
        _sfxSource.mute = !_soundsEnabled;
    }

    public void SetMusicEnabled(bool enabled)
    {
        _musicEnabled = enabled;
        PlayerPrefs.SetInt(GameConstants.Prefs.MusicEnabled, enabled ? 1 : 0);
        PlayerPrefs.Save();

        _musicSource.mute = !enabled;
        if (enabled && !_musicSource.isPlaying && _bgmClip != null)
        {
            PlayBgm();
        }
    }

    public void SetSoundsEnabled(bool enabled)
    {
        _soundsEnabled = enabled;
        PlayerPrefs.SetInt(GameConstants.Prefs.SoundsEnabled, enabled ? 1 : 0);
        PlayerPrefs.Save();

        _sfxSource.mute = !enabled;
    }

    public void PlayBgm(bool forceRestart = false)
    {
        if (_bgmClip == null || _musicSource == null)
        {
            return;
        }

        if (!forceRestart && _musicSource.isPlaying && _musicSource.clip == _bgmClip)
        {
            return;
        }

        _musicSource.clip = _bgmClip;
        _musicSource.mute = !_musicEnabled;
        _musicSource.Play();
    }

    public void PlaySfx(SoundEffect effect)
    {
        if (!_soundsEnabled)
        {
            return;
        }

        AudioClip clip = effect switch
        {
            SoundEffect.ButtonClick => _buttonClickClip,
            SoundEffect.PopupOpen => _popupOpenClip,
            SoundEffect.PopupClose => _popupCloseClip,
            SoundEffect.BoxPlaced => _boxPlacedClip,
            SoundEffect.BoxRemoved => _boxRemovedClip != null ? _boxRemovedClip : _buttonClickClip,
            SoundEffect.LevelCompleted => _levelCompletedClip,
            _ => null
        };

        if (clip != null)
        {
            _sfxSource.PlayOneShot(clip);
        }
    }
}