using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class CardHoverHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rt;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private bool isHovering = false;
    private Coroutine hoverAnimation;

    public void Initialize(RectTransform rectTransform)
    {
        rt = rectTransform;
        originalScale = rt.localScale;
        originalPosition = rt.localPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverAnimation != null) StopCoroutine(hoverAnimation);
        hoverAnimation = StartCoroutine(AnimateHover(true));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverAnimation != null) StopCoroutine(hoverAnimation);
        hoverAnimation = StartCoroutine(AnimateHover(false));
    }

    private IEnumerator AnimateHover(bool hovering)
    {
        float elapsed = 0;
        float duration = 0.2f;
        Vector3 startScale = rt.localScale;
        Vector3 targetScale = hovering ? originalScale * 1.1f : originalScale;
        Vector3 startPos = rt.localPosition;
        Vector3 targetPos = hovering ? originalPosition + Vector3.up * 20 : originalPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            rt.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
            rt.localPosition = Vector3.Lerp(startPos, targetPos, smoothT);
            
            yield return null;
        }

        rt.localScale = targetScale;
        rt.localPosition = targetPos;
        isHovering = hovering;
    }
} 