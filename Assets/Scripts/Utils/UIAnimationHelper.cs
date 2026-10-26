using System.Collections;
using UnityEngine;

public static class UIAnimationHelper
{
    public static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        float diff = x - 1f;
        return 1f + (c3 * diff * diff * diff) + (c1 * diff * diff);
    }
    public static IEnumerator PunchScale(Transform target, Vector3 baseScale, float punchFactor = 0.1f, float duration = 0.16f)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            float scaleMultiplier = 1.0f + (Mathf.Sin(progress * Mathf.PI) * punchFactor);
            target.localScale = baseScale * scaleMultiplier;
            yield return null;
        }
        target.localScale = baseScale;
    }
    public static IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }
}
