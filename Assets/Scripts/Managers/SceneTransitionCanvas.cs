using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景过渡画面控制器
/// 用于实现场景内容切换时的淡入淡出效果
/// </summary>
public class SceneTransitionCanvas : MonoBehaviour
{
    [Header("过渡设置")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color fadeColor = Color.black;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        if (fadeImage != null)
        {
            fadeImage.color = fadeColor;
        }

        // 初始不可见
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    /// <summary>
    /// 淡入过渡画面
    /// </summary>
    public void FadeIn()
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeInRoutine());
    }

    /// <summary>
    /// 淡出过渡画面
    /// </summary>
    public void FadeOut()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOutRoutine());
    }

    /// <summary>
    /// 淡入协程
    /// </summary>
    private IEnumerator FadeInRoutine()
    {
        canvasGroup.blocksRaycasts = true;

        float startTime = Time.time;
        float startAlpha = canvasGroup.alpha;

        while (Time.time < startTime + fadeDuration)
        {
            float normalizedTime = (Time.time - startTime) / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, normalizedTime);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 淡出协程
    /// </summary>
    private IEnumerator FadeOutRoutine()
    {
        float startTime = Time.time;
        float startAlpha = canvasGroup.alpha;

        while (Time.time < startTime + fadeDuration)
        {
            float normalizedTime = (Time.time - startTime) / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalizedTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        gameObject.SetActive(false);
    }
}