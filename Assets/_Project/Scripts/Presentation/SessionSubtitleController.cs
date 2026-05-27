using System.Collections;
using UnityEngine;

namespace DinoAlkkagi.Presentation
{
public class SessionSubtitleController : MonoBehaviour
{
    private static bool hasShownSubtitleThisSession;

    [SerializeField] private float bounceOffsetY = 12f;
    [SerializeField] private float visibleSeconds = 2f;
    [SerializeField] private float fadeSeconds = 0.5f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 baseAnchoredPosition;
    private Vector3 baseScale;
    private Coroutine animationCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        baseAnchoredPosition = rectTransform.anchoredPosition;
        baseScale = rectTransform.localScale;

        if (hasShownSubtitleThisSession)
        {
            Debug.Log("Text_Subtitle already shown, hide immediately");
            HideImmediately();
            return;
        }

        hasShownSubtitleThisSession = true;
        Debug.Log("Text_Subtitle shown for first time");

        canvasGroup.alpha = 1f;
        gameObject.SetActive(true);

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(PlayOnce());
    }

    private IEnumerator PlayOnce()
    {
        yield return AnimateBounce(0.16f, baseAnchoredPosition + Vector2.up * bounceOffsetY, baseScale * 1.05f);
        yield return AnimateBounce(0.12f, baseAnchoredPosition, baseScale * 0.96f);
        yield return AnimateBounce(0.12f, baseAnchoredPosition, baseScale);

        yield return new WaitForSeconds(visibleSeconds);

        float elapsed = 0f;
        while (elapsed < fadeSeconds)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / fadeSeconds));
            yield return null;
        }

        HideImmediately();
    }

    private IEnumerator AnimateBounce(float duration, Vector2 targetPosition, Vector3 targetScale)
    {
        float elapsed = 0f;
        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector3 startScale = rectTransform.localScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }
    }

    private void HideImmediately()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        canvasGroup.alpha = 0f;
        rectTransform.anchoredPosition = baseAnchoredPosition;
        rectTransform.localScale = baseScale;
        gameObject.SetActive(false);
    }
}
}
