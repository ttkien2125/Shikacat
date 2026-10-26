using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BoosterButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _background;
    [SerializeField] private Image _icon;
    [SerializeField] private GameObject _badgeRoot;
    [SerializeField] private TextMeshProUGUI _badgeText;
    [SerializeField] private CanvasGroup _canvasGroup;

    private Vector3 _baseScale = Vector3.one;
    private Coroutine _bounceCoroutine;

    public Button Button => _button;
    public Image Background => _background;
    public Image Icon => _icon;

    private void Awake()
    {
        _baseScale = transform.localScale;
        EnsureCanvasGroup();
    }

    private void OnDisable()
    {
        if (_bounceCoroutine != null)
        {
            StopCoroutine(_bounceCoroutine);
            _bounceCoroutine = null;
        }
        transform.localScale = _baseScale;
    }

    public void Bind(Action onClick)
    {
        if (_button == null)
        {
            _button = GetComponent<Button>();
        }


        if (_button != null && onClick != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() =>
            {
                PlayClickAnimation();
                onClick.Invoke();
            });
        }
    }

    public void SetInteractable(bool interactable)
    {
        if (_button != null)
        {
            _button.interactable = interactable;
        }


        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = interactable ? 1.0f : 0.45f;
            _canvasGroup.blocksRaycasts = interactable;
        }
    }

    public void SetBadge(string text, bool visible = true)
    {
        if (_badgeRoot != null)
        {
            _badgeRoot.SetActive(visible);
        }

        if (_badgeText != null)
        {
            _badgeText.text = text;
        }

    }

    public void PlayClickAnimation()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(AudioManager.SoundEffect.ButtonClick);
        }


        HapticsManager.Trigger();
        if (!gameObject.activeInHierarchy)
        {
            return;
        }


        if (_bounceCoroutine != null)
        {
            StopCoroutine(_bounceCoroutine);
        }


        _bounceCoroutine = StartCoroutine(BounceRoutine());
    }

    private void EnsureCanvasGroup()
    {
        if (_canvasGroup == null && !TryGetComponent(out _canvasGroup))
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private IEnumerator BounceRoutine()
    {
        // Squash
        float elapsed = 0f;
        float squashTime = 0.08f;
        while (elapsed < squashTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float factor = Mathf.Lerp(1.0f, 0.90f, elapsed / squashTime);
            transform.localScale = _baseScale * factor;
            yield return null;
        }

        // Rebound
        elapsed = 0f;
        float reboundTime = 0.12f;
        while (elapsed < reboundTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float factor = Mathf.Lerp(0.90f, 1.0f, elapsed / reboundTime);
            transform.localScale = _baseScale * factor;
            yield return null;
        }

        transform.localScale = _baseScale;
        _bounceCoroutine = null;
    }
}
