using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CardGlowEffect : MonoBehaviour
{
    private Image glowImage;
    private Color baseColor;
    private float glowIntensity = 0;
    private Coroutine glowAnimation;

    public void Initialize(Color cardColor)
    {
        GameObject glowObj = new GameObject("Glow");
        glowObj.transform.SetParent(transform, false);
        glowImage = glowObj.AddComponent<Image>();
        glowImage.sprite = CardUIUtility.CreateRoundedRectSprite(32);
        glowImage.type = Image.Type.Sliced;
        
        RectTransform glowRT = glowObj.GetComponent<RectTransform>();
        glowRT.anchorMin = Vector2.zero;
        glowRT.anchorMax = Vector2.one;
        glowRT.sizeDelta = new Vector2(20, 20);
        glowRT.anchoredPosition = Vector2.zero;

        baseColor = new Color(cardColor.r, cardColor.g, cardColor.b, 0);
        glowImage.color = baseColor;

        StartCoroutine(PulseGlow());
    }

    private IEnumerator PulseGlow()
    {
        while (true)
        {
            float elapsed = 0;
            float duration = 2f;
            float startIntensity = glowIntensity;
            float targetIntensity = startIntensity > 0.1f ? 0 : 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                glowIntensity = Mathf.Lerp(startIntensity, targetIntensity, t);
                glowImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, glowIntensity);
                yield return null;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
} 