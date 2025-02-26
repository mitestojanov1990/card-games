using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CardAnimation : MonoBehaviour
{
    public static CardAnimation CreateAnimatedCard(Card card, Transform parent, Vector2 startPos, Vector2 endPos)
    {
        GameObject cardObj = new GameObject("AnimatedCard");
        cardObj.transform.SetParent(parent, false);
        
        // Add components
        Image cardImage = cardObj.AddComponent<Image>();
        cardImage.color = Color.white;
        CardAnimation anim = cardObj.AddComponent<CardAnimation>();
        
        // Setup card visual
        GameObject textObj = new GameObject("CardText");
        textObj.transform.SetParent(cardObj.transform, false);
        Text cardText = textObj.AddComponent<Text>();
        cardText.text = $"{card.Rank}{card.Suit}";
        cardText.alignment = TextAnchor.MiddleCenter;
        cardText.color = card.Color;
        cardText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        
        // Setup transforms
        RectTransform cardRT = cardObj.GetComponent<RectTransform>();
        cardRT.sizeDelta = new Vector2(100f, 150f);
        cardRT.anchoredPosition = startPos;
        
        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        // Start animation
        anim.StartAnimation(startPos, endPos);
        
        return anim;
    }

    private void StartAnimation(Vector2 startPos, Vector2 endPos)
    {
        StartCoroutine(AnimateCard(startPos, endPos));
    }

    private IEnumerator AnimateCard(Vector2 startPos, Vector2 endPos)
    {
        float duration = 0.5f;
        float elapsed = 0f;
        RectTransform rt = GetComponent<RectTransform>();

        // Add a slight arc to the movement
        Vector2 controlPoint = (startPos + endPos) / 2f + new Vector2(0, 100f);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            
            // Quadratic Bezier curve
            Vector2 position = Vector2.Lerp(
                Vector2.Lerp(startPos, controlPoint, t),
                Vector2.Lerp(controlPoint, endPos, t),
                t
            );
            
            rt.anchoredPosition = position;
            rt.rotation = Quaternion.Euler(0, 0, 360f * t); // Add rotation

            elapsed += Time.deltaTime;
            yield return null;
        }

        rt.anchoredPosition = endPos;
        Destroy(gameObject, 0.1f);
    }
} 