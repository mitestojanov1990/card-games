using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using CardGame.Core;

namespace CardGame.UI
{
    public class CardDrawAnimation : MonoBehaviour
    {
        private float cardMoveSpeed = 1000f;
        private float cardRotateSpeed = 360f;
        private Vector2 deckPosition;
        private Vector2 handPosition;
        private Queue<Card> cardsToAnimate = new Queue<Card>();
        private bool isAnimating = false;

        public void Initialize(Vector2 deckPos, Vector2 handPos)
        {
            deckPosition = deckPos;
            handPosition = handPos;
        }

        public void QueueCardDraw(Card card)
        {
            cardsToAnimate.Enqueue(card);
            if (!isAnimating)
            {
                StartCoroutine(AnimateNextCard());
            }
        }

        private IEnumerator AnimateNextCard()
        {
            isAnimating = true;

            while (cardsToAnimate.Count > 0)
            {
                Card card = cardsToAnimate.Dequeue();
                yield return StartCoroutine(AnimateCardDraw(card));
                
                // Small pause between cards
                yield return new WaitForSeconds(0.1f);
            }

            isAnimating = false;
        }

        private IEnumerator AnimateCardDraw(Card card)
        {
            // Create temporary card visual
            GameObject cardObj = new GameObject("DrawingCard");
            cardObj.transform.SetParent(transform, false);
            Image cardImage = cardObj.AddComponent<Image>();
            cardImage.color = Color.white;

            // Add card text
            Text cardText = CreateCardText(cardObj, card);

            // Setup initial position
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(100, 150);
            rt.position = deckPosition;

            // Animate to hand
            float distance = Vector2.Distance(deckPosition, handPosition);
            float duration = distance / cardMoveSpeed;
            float elapsed = 0;

            Vector3 startPos = deckPosition;
            Vector3 startRotation = Vector3.zero;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Move in arc
                Vector3 currentPos = Vector3.Lerp(startPos, handPosition, t);
                currentPos.y += Mathf.Sin(t * Mathf.PI) * 100; // Arc height

                // Rotate while moving
                float currentRotation = Mathf.Lerp(0, 360, t);

                rt.position = currentPos;
                rt.rotation = Quaternion.Euler(0, 0, currentRotation);

                yield return null;
            }

            // Snap to final position
            rt.position = handPosition;
            rt.rotation = Quaternion.identity;

            // Fade out
            float fadeDuration = 0.2f;
            elapsed = 0;
            Color startColor = cardImage.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0);

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                
                cardImage.color = Color.Lerp(startColor, endColor, t);
                cardText.color = Color.Lerp(startColor, endColor, t);
                
                yield return null;
            }

            Destroy(cardObj);
        }

        private Text CreateCardText(GameObject parent, Card card)
        {
            GameObject textObj = new GameObject("CardText");
            textObj.transform.SetParent(parent.transform, false);
            
            Text text = textObj.AddComponent<Text>();
            text.text = $"{card.Rank}{card.Suit}";
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = card.Color;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            return text;
        }
    }
} 