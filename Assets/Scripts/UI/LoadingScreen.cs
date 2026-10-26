using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Fullscreen loading transition screen displayed between level swaps and restarts.
/// Features a smooth fade, gentle breathing animation on the center illustration, and animated loading text.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class LoadingScreen : MonoBehaviour
{
    [Header("UI Hierarchy")]
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _centerContainer;
    [SerializeField] private TextMeshProUGUI _loadingText;

    [Header("Timing")]
    [SerializeField] private float _fadeInDuration = 0.22f;
    [SerializeField] private float _holdDuration = 0.45f;
    [SerializeField] private float _fadeOutDuration = 0.25f;

    private Coroutine _transitionCoroutine;
    private Coroutine _bobCoroutine;

    private void Awake()
    {
        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

    }

    public void ShowTransition(Action onCovered, Action onFinished = null, float customHoldDuration = -1f)
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = true;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();

        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
        }


        _transitionCoroutine = StartCoroutine(TransitionRoutine(onCovered, onFinished, customHoldDuration));
    }

    private IEnumerator TransitionRoutine(Action onCovered, Action onFinished, float customHoldDuration)
    {
        if (_bobCoroutine != null)
        {
            StopCoroutine(_bobCoroutine);
        }


        _bobCoroutine = StartCoroutine(BobAnimationRoutine());

        // 1. Fade in
        yield return FadeCanvas(0f, 1f, _fadeInDuration);

        // 2. Perform the swap action (load level, reset grid, etc.) while fully covered
        try
        {
            onCovered?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

        // 3. Hold for artificial delay (e.g. 2.0s when continuing to next level)
        float hold = customHoldDuration >= 0f ? customHoldDuration : _holdDuration;
        yield return new WaitForSecondsRealtime(hold);

        // 4. Fade out
        yield return FadeCanvas(1f, 0f, _fadeOutDuration);

        if (_canvasGroup != null)
        {
            _canvasGroup.blocksRaycasts = false;
        }


        StopBobbing();

        gameObject.SetActive(false);
        _transitionCoroutine = null;
        onFinished?.Invoke();
    }

    private IEnumerator FadeCanvas(float start, float target, float duration)
    {
        if (_canvasGroup == null)
        {
            yield break;
        }


        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        _canvasGroup.alpha = target;
    }

    private IEnumerator BobAnimationRoutine()
    {
        float timer = 0f;
        string[] dots = { "Loading.", "Loading..", "Loading..." };

        while (true)
        {
            timer += Time.unscaledDeltaTime;

            if (_centerContainer != null)
            {
                float breath = 1f + (Mathf.Sin(timer * 2.8f) * 0.02f);
                _centerContainer.localScale = new Vector3(breath, breath, 1f);
            }

            if (_loadingText != null)
            {
                int dotIndex = (int)(timer * 3.5f) % dots.Length;
                _loadingText.text = dots[dotIndex];
            }

            yield return null;
        }
    }

    private void StopBobbing()
    {
        if (_bobCoroutine != null)
        {
            StopCoroutine(_bobCoroutine);
            _bobCoroutine = null;
        }

        if (_centerContainer != null)
        {
            _centerContainer.localScale = Vector3.one;
        }

    }

    private void OnDisable()
    {
        StopBobbing();
        _transitionCoroutine = null;
    }
}
