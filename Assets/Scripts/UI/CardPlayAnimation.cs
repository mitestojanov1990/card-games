using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using CardGame.Core;
using CardGame.Rules;

namespace CardGame.UI
{
    public class CardPlayAnimation : MonoBehaviour
    {
        private float cardMoveSpeed = 1200f;
        private float cardRotateSpeed = 720f;
        private Vector2 discardPosition;
        private Queue<CardPlayInfo> cardsToAnimate = new Queue<CardPlayInfo>();
        private bool isAnimating = false;

        [Header("Special Effect Settings")]
        private float glowPulseSpeed = 4f;
        private float glowIntensity = 0.3f;
        private Color[] specialEffectColors = new Color[]
        {
            new Color(1f, 0.2f, 0.2f), // Draw cards (red)
            new Color(0.2f, 1f, 0.2f), // Sequential (green)
            new Color(0.2f, 0.2f, 1f), // Change suit (blue)
            new Color(1f, 1f, 0.2f),   // Skip turn (yellow)
            new Color(1f, 0.2f, 1f)    // Pop cup (purple)
        };

        private struct CardPlayInfo
        {
            public Card Card;
            public Vector2 StartPosition;
            public bool IsSpecialEffect;
        }

        public void Initialize(Vector2 discardPos)
        {
            discardPosition = discardPos;
        }

        public void QueueCardPlay(Card card, Vector2 startPos, bool isSpecialEffect = false)
        {
            cardsToAnimate.Enqueue(new CardPlayInfo 
            { 
                Card = card, 
                StartPosition = startPos,
                IsSpecialEffect = isSpecialEffect
            });

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
                var playInfo = cardsToAnimate.Dequeue();
                yield return StartCoroutine(AnimateCardPlay(playInfo));
                
                // Small pause between cards for sequential plays
                yield return new WaitForSeconds(0.1f);
            }

            isAnimating = false;
        }

        private IEnumerator AnimateCardPlay(CardPlayInfo playInfo)
        {
            GameObject cardObj = CreateCardObject(playInfo);
            
            if (playInfo.IsSpecialEffect)
            {
                StartCoroutine(AnimateSpecialEffect(cardObj, playInfo.Card));
            }

            yield return StartCoroutine(AnimateCardMovement(cardObj, playInfo));
            yield return StartCoroutine(FadeOutCard(cardObj));
            
            Destroy(cardObj);
        }

        private GameObject CreateCardObject(CardPlayInfo playInfo)
        {
            GameObject cardObj = new GameObject("PlayingCard");
            cardObj.transform.SetParent(transform, false);
            
            Image cardImage = cardObj.AddComponent<Image>();
            cardImage.color = Color.white;
            
            Text cardText = CreateCardText(cardObj, playInfo.Card);

            if (playInfo.IsSpecialEffect)
            {
                CreateSpecialEffectVisuals(cardObj, playInfo.Card);
            }

            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(100, 150);
            rt.position = playInfo.StartPosition;

            return cardObj;
        }

        private void CreateSpecialEffectVisuals(GameObject cardObj, Card card)
        {
            // Add glow effect
            Image glowImage = CreateGlowEffect(cardObj, GetSpecialEffectColor(card));
            
            // Add particle system
            var particleSystem = CreateParticleSystem(cardObj, GetSpecialEffectColor(card));
            
            // Add effect text
            CreateEffectText(cardObj, GetEffectDescription(card));
        }

        private Color GetSpecialEffectColor(Card card)
        {
            var effect = CardRules.GetCardEffect(card);
            switch (effect)
            {
                case CardRules.SpecialEffect.DrawTwo:
                case CardRules.SpecialEffect.DrawThree:
                    return specialEffectColors[0];
                case CardRules.SpecialEffect.Sequential:
                    return specialEffectColors[1];
                case CardRules.SpecialEffect.ChangeSuit:
                    return specialEffectColors[2];
                case CardRules.SpecialEffect.SkipTurn:
                    return specialEffectColors[3];
                case CardRules.SpecialEffect.PopCup:
                    return specialEffectColors[4];
                default:
                    return Color.white;
            }
        }

        private string GetEffectDescription(Card card)
        {
            var effect = CardRules.GetCardEffect(card);
            switch (effect)
            {
                case CardRules.SpecialEffect.DrawTwo:
                    return "+2";
                case CardRules.SpecialEffect.DrawThree:
                    return "+3";
                case CardRules.SpecialEffect.Sequential:
                    return "Sequential";
                case CardRules.SpecialEffect.ChangeSuit:
                    return "Change Suit";
                case CardRules.SpecialEffect.SkipTurn:
                    return "Skip";
                case CardRules.SpecialEffect.PopCup:
                    return "+5";
                default:
                    return "";
            }
        }

        private ParticleSystem CreateParticleSystem(GameObject parent, Color effectColor)
        {
            GameObject particleObj = new GameObject("Particles");
            particleObj.transform.SetParent(parent.transform, false);
            
            ParticleSystem ps = particleObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor = effectColor;
            main.startSize = 5f;
            main.startSpeed = 20f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.duration = 1f;
            main.loop = true;

            var emission = ps.emission;
            emission.rateOverTime = 20;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            shape.scale = new Vector3(100, 150, 1);

            return ps;
        }

        private void CreateEffectText(GameObject parent, string effectText)
        {
            GameObject textObj = new GameObject("EffectText");
            textObj.transform.SetParent(parent.transform, false);
            
            Text text = textObj.AddComponent<Text>();
            text.text = effectText;
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            text.alignment = TextAnchor.UpperCenter;
            text.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 1);
            textRT.anchorMax = new Vector2(1, 1);
            textRT.sizeDelta = new Vector2(0, 30);
            textRT.anchoredPosition = new Vector2(0, 10);

            // Add outline
            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);
        }

        private IEnumerator AnimateSpecialEffect(GameObject cardObj, Card card)
        {
            var glowImage = cardObj.GetComponentInChildren<Image>();
            var particleSystem = cardObj.GetComponentInChildren<ParticleSystem>();
            var effectColor = GetSpecialEffectColor(card);
            float elapsed = 0;

            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 1f;

                // Pulse glow
                float pulse = Mathf.Sin(t * glowPulseSpeed * Mathf.PI) * glowIntensity + (1 - glowIntensity);
                if (glowImage != null)
                {
                    glowImage.color = new Color(effectColor.r, effectColor.g, effectColor.b, pulse);
                }

                // Scale effect
                float scale = 1f + Mathf.Sin(t * Mathf.PI * 2) * 0.1f;
                cardObj.transform.localScale = Vector3.one * scale;

                yield return null;
            }

            if (particleSystem != null)
            {
                particleSystem.Stop();
            }
        }

        private IEnumerator AnimateCardMovement(GameObject cardObj, CardPlayInfo playInfo)
        {
            // Animate to discard pile
            float distance = Vector2.Distance(playInfo.StartPosition, discardPosition);
            float duration = distance / cardMoveSpeed;
            float elapsed = 0;

            Vector3 startPos = playInfo.StartPosition;
            Vector3 startRotation = Vector3.zero;
            
            // Create arc path
            Vector2 midPoint = Vector2.Lerp(startPos, discardPosition, 0.5f);
            midPoint.y += 100f; // Arc height

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Bezier curve movement
                Vector2 currentPos = Vector2.Lerp(
                    Vector2.Lerp(startPos, midPoint, t),
                    Vector2.Lerp(midPoint, discardPosition, t),
                    t
                );

                // Rotate while moving
                float currentRotation = t * cardRotateSpeed;

                RectTransform rt = cardObj.GetComponent<RectTransform>();
                rt.position = currentPos;
                rt.rotation = Quaternion.Euler(0, 0, currentRotation);

                yield return null;
            }

            // Snap to final position
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.position = discardPosition;
            rt.rotation = Quaternion.identity;

            yield return null;
        }

        private IEnumerator FadeOutCard(GameObject cardObj)
        {
            float fadeDuration = 0.2f;
            float elapsed = 0;
            Color startColor = cardObj.GetComponent<Image>().color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0);

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                
                Image cardImage = cardObj.GetComponent<Image>();
                cardImage.color = Color.Lerp(startColor, endColor, t);
                Text cardText = cardObj.GetComponentInChildren<Text>();
                cardText.color = Color.Lerp(startColor, endColor, t);
                
                yield return null;
            }
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

        private Image CreateGlowEffect(GameObject parent, Color cardColor)
        {
            GameObject glowObj = new GameObject("Glow");
            glowObj.transform.SetParent(parent.transform, false);
            
            Image glow = glowObj.AddComponent<Image>();
            glow.sprite = CardUIUtility.CreateRoundedRectSprite(32);
            glow.type = Image.Type.Sliced;
            
            Color glowColor = new Color(cardColor.r, cardColor.g, cardColor.b, 0.5f);
            glow.color = glowColor;

            RectTransform glowRT = glowObj.GetComponent<RectTransform>();
            glowRT.anchorMin = Vector2.zero;
            glowRT.anchorMax = Vector2.one;
            glowRT.sizeDelta = new Vector2(20, 20);
            glowRT.anchoredPosition = Vector2.zero;

            return glow;
        }
    }
} 