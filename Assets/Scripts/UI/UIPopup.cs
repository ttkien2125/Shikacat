using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// Reusable base class for UI popups providing backdrop fade, card bounce animations,
/// standard close/backdrop button wiring, and standardized open/close lifecycle logic.
/// </summary>
public abstract class UIPopup : MonoBehaviour
{
    [Header("Popup Hierarchy")]
    [SerializeField] protected CanvasGroup _backdrop;
    [SerializeField][FormerlySerializedAs("_content")] protected RectTransform _card;
    [SerializeField] protected Button _closeButton;

    [Header("Animation Settings")]
    [SerializeField] protected float _openDuration = 0.25f;
    [SerializeField] protected float _closeDuration = 0.16f;
    [SerializeField] protected float _startScale = 0.8f;
    [SerializeField] protected float _closeScale = 0.85f;

    protected Coroutine _animCoroutine;
    protected bool _isClosing;

    protected virtual void Awake()
    {
        SetupDefaultTriggers();
    }

    /// <summary>
    /// Automatically wires standard close button and backdrop click-to-close behavior.
    /// Can be overridden or extended by subclasses.
    /// </summary>
    protected virtual void SetupDefaultTriggers()
    {
        if (_closeButton != null)
        {
            _closeButton.onClick.RemoveAllListeners();
            _closeButton.onClick.AddListener(() => Close());
        }

        if (_backdrop != null)
        {
            Button backdropBtn = _backdrop.GetComponent<Button>();
            if (backdropBtn != null)
            {
                backdropBtn.onClick.RemoveAllListeners();
                backdropBtn.onClick.AddListener(() => Close());
            }
        }
    }

    public virtual void Open()
    {
        _isClosing = false;
        PlayOpenSound();

        gameObject.SetActive(true);

        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
        }

        _animCoroutine = StartCoroutine(AnimateOpen());
    }

    public void Close()
    {
        Close(null);
    }

    public virtual void Close(Action onComplete)
    {
        if (_isClosing)
        {
            return;
        }

        _isClosing = true;
        PlayCloseSound();

        if (_animCoroutine != null)
        {
            StopCoroutine(_animCoroutine);
        }

        _animCoroutine = StartCoroutine(AnimateClose(onComplete));
    }

    protected virtual void PlayOpenSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(AudioManager.SoundEffect.PopupOpen);
        }
    }

    protected virtual void PlayCloseSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(AudioManager.SoundEffect.PopupClose);
        }
    }

    protected virtual IEnumerator AnimateOpen()
    {
        if (_backdrop != null)
        {
            _backdrop.alpha = 0f;
        }

        if (_card != null)
        {
            _card.localScale = Vector3.one * _startScale;
        }

        float elapsed = 0f;
        while (elapsed < _openDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _openDuration);

            if (_backdrop != null)
            {
                _backdrop.alpha = Mathf.Lerp(0f, 1f, t);
            }

            if (_card != null)
            {
                float scale = Mathf.LerpUnclamped(_startScale, 1.0f, UIAnimationHelper.EaseOutBack(t));
                _card.localScale = Vector3.one * scale;
            }

            yield return null;
        }

        if (_backdrop != null)
        {
            _backdrop.alpha = 1f;
        }

        if (_card != null)
        {
            _card.localScale = Vector3.one;
        }
    }

    protected virtual IEnumerator AnimateClose(Action onComplete)
    {
        float elapsed = 0f;
        Vector3 startScale = _card != null ? _card.localScale : Vector3.one;
        float startAlpha = _backdrop != null ? _backdrop.alpha : 1f;

        while (elapsed < _closeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / _closeDuration);

            if (_backdrop != null)
            {
                _backdrop.alpha = Mathf.Lerp(startAlpha, 0f, t);
            }

            if (_card != null)
            {
                _card.localScale = Vector3.one * Mathf.Lerp(startScale.x, _closeScale, t);
            }

            yield return null;
        }

        gameObject.SetActive(false);
        _isClosing = false;
        onComplete?.Invoke();
    }
}
