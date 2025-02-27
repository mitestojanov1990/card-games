using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using CardGame.Core;

public class SuitSelector : MonoBehaviour
{
    private RectTransform rectTransform;
    private List<Button> suitButtons = new List<Button>();
    private static readonly string[] SUITS = { "♥", "♦", "♣", "♠" };
    private static readonly Color[] SUIT_COLORS = {
        new Color(0.9f, 0.2f, 0.2f, 1f), // Bright red for hearts
        new Color(0.9f, 0.2f, 0.2f, 1f), // Bright red for diamonds
        new Color(0.95f, 0.95f, 0.95f, 1f), // White for clubs
        new Color(0.95f, 0.95f, 0.95f, 1f)  // White for spades
    };
    
    private const float BUTTON_SIZE = 120f;
    private const float SPACING = 30f;
    private const float PANEL_PADDING = 40f;
    private const float TITLE_HEIGHT = 60f;
    private const float ANIMATION_DURATION = 0.3f;

    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private bool isAnimating = false;
    private bool isInitialized = false;

    private void Awake()
    {
        // Don't initialize in Awake, let GameManager call Initialize explicitly
    }

    public void Initialize()
    {
        if (isInitialized) return;

        try
        {
            // Ensure we have a RectTransform
            rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                rectTransform = gameObject.AddComponent<RectTransform>();
            }

            SetupUI();
            isInitialized = true;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to initialize SuitSelector: {ex.Message}");
            throw;
        }
    }

    private void SetupUI()
    {
        // Add background panel
        Image background = gameObject.AddComponent<Image>();
        background.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        // Add panel outline
        GameObject outlineObj = new GameObject("Outline");
        outlineObj.transform.SetParent(transform, false);
        Image outline = outlineObj.AddComponent<Image>();
        outline.color = new Color(1f, 1f, 1f, 0.1f);
        RectTransform outlineRT = outline.GetComponent<RectTransform>();
        outlineRT.anchorMin = Vector2.zero;
        outlineRT.anchorMax = Vector2.one;
        outlineRT.sizeDelta = Vector2.zero;

        // Create title
        CreateTitle();

        // Create suit buttons
        CreateSuitButtons();

        // Set panel size
        float panelWidth = (BUTTON_SIZE * 2) + SPACING + (PANEL_PADDING * 2);
        float panelHeight = (BUTTON_SIZE * 2) + SPACING + (PANEL_PADDING * 2) + TITLE_HEIGHT;
        rectTransform.sizeDelta = new Vector2(panelWidth, panelHeight);

        // Add CanvasGroup for fade effects
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        
        // Store original scale for animations
        originalScale = transform.localScale;
        
        // Hide initially
        gameObject.SetActive(false);
        transform.localScale = Vector3.zero;
    }

    private void CreateTitle()
    {
        try
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(transform, false);
            
            // Create background first
            Image titleBg = titleObj.AddComponent<Image>();
            titleBg.color = new Color(0, 0, 0, 0.3f);

            // Create text object
            GameObject textObj = new GameObject("TitleText");
            textObj.transform.SetParent(titleObj.transform, false);
            
            // Add basic Text component
            Text titleText = textObj.AddComponent<Text>();
            titleText.text = "SELECT SUIT";
            titleText.fontSize = 36;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = new Color(1f, 1f, 1f, 0.95f);

            // Set up RectTransforms
            RectTransform titleRT = titleObj.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0, 1);
            titleRT.anchorMax = new Vector2(1, 1);
            titleRT.sizeDelta = new Vector2(0, TITLE_HEIGHT);
            titleRT.anchoredPosition = new Vector2(0, -TITLE_HEIGHT/2);

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to create title: {ex.Message}\nStackTrace: {ex.StackTrace}");
            throw;
        }
    }

    private void CreateSuitButtons()
    {
        try
        {
            suitButtons.Clear();
            
            for (int i = 0; i < SUITS.Length; i++)
            {
                int row = i / 2;
                int col = i % 2;
                
                GameObject buttonObj = new GameObject($"SuitButton_{SUITS[i]}");
                buttonObj.transform.SetParent(transform, false);
                
                Button button = buttonObj.AddComponent<Button>();
                Image buttonImage = buttonObj.AddComponent<Image>();
                buttonImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);

                // Create text object
                GameObject textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                
                Text buttonText = textObj.AddComponent<Text>();
                buttonText.text = SUITS[i];
                buttonText.fontSize = 48;
                buttonText.alignment = TextAnchor.MiddleCenter;
                buttonText.color = SUIT_COLORS[i];

                // Position button
                RectTransform buttonRT = buttonObj.GetComponent<RectTransform>();
                buttonRT.sizeDelta = new Vector2(BUTTON_SIZE, BUTTON_SIZE);
                float x = (col * (BUTTON_SIZE + SPACING)) - ((BUTTON_SIZE + SPACING) / 2);
                float y = -(row * (BUTTON_SIZE + SPACING)) - TITLE_HEIGHT - SPACING;
                buttonRT.anchoredPosition = new Vector2(x, y);

                // Setup text transform
                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.sizeDelta = Vector2.zero;

                // Add click handler
                string suit = SUITS[i];
                button.onClick.AddListener(() => OnSuitSelected(suit));

                suitButtons.Add(button);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Failed to create suit buttons: {ex.Message}\nStackTrace: {ex.StackTrace}");
            throw;
        }
    }

    public void Show(Vector2 position)
    {
        if (isAnimating) return;

        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = position;
        
        gameObject.SetActive(true);
        StartCoroutine(ShowAnimation());
    }

    private IEnumerator ShowAnimation()
    {
        isAnimating = true;

        // Reset initial state
        canvasGroup.alpha = 0f;
        transform.localScale = Vector3.zero;
        
        // Enable buttons but make them non-interactable during animation
        foreach (var button in suitButtons)
        {
            button.gameObject.SetActive(true);
            button.interactable = false;
        }

        float elapsed = 0f;
        while (elapsed < ANIMATION_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ANIMATION_DURATION;
            
            // Use smooth step for more pleasing animation
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // Animate scale and fade
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, smoothT);
            canvasGroup.alpha = Mathf.Lerp(0, 1, smoothT);
            
            yield return null;
        }

        // Ensure final state
        transform.localScale = originalScale;
        canvasGroup.alpha = 1f;

        // Enable button interaction
        foreach (var button in suitButtons)
        {
            button.interactable = true;
        }

        isAnimating = false;
    }

    private void Hide()
    {
        if (isAnimating) return;
        StartCoroutine(HideAnimation());
    }

    private IEnumerator HideAnimation()
    {
        isAnimating = true;

        // Disable button interaction during animation
        foreach (var button in suitButtons)
        {
            button.interactable = false;
        }

        float elapsed = 0f;
        while (elapsed < ANIMATION_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ANIMATION_DURATION;
            
            // Use smooth step for more pleasing animation
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            // Animate scale and fade
            transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, smoothT);
            canvasGroup.alpha = Mathf.Lerp(1, 0, smoothT);
            
            yield return null;
        }

        // Ensure final state
        transform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;
        
        gameObject.SetActive(false);
        isAnimating = false;
    }

    private void OnSuitSelected(string suit)
    {
        // Disable buttons to prevent multiple selections
        foreach (var button in suitButtons)
        {
            button.interactable = false;
        }

        // Notify GameManager
        GameManager.Instance.DeclareSuit(suit);

        // Add visual feedback
        StartCoroutine(ShowSelectionFeedback(suit));
    }

    private IEnumerator ShowSelectionFeedback(string suit)
    {
        GameObject feedbackObj = new GameObject("Feedback");
        feedbackObj.transform.SetParent(transform, false);
        TextMeshProUGUI feedbackText = feedbackObj.AddComponent<TextMeshProUGUI>();
        feedbackText.text = $"Selected {suit}!";
        feedbackText.fontSize = 32;
        feedbackText.fontStyle = FontStyles.Bold;
        feedbackText.alignment = TextAlignmentOptions.Center;
        feedbackText.color = Color.white;

        // Add text shadow
        Shadow feedbackShadow = feedbackObj.AddComponent<Shadow>();
        feedbackShadow.effectColor = new Color(0, 0, 0, 0.5f);
        feedbackShadow.effectDistance = new Vector2(2, -2);

        RectTransform feedbackRT = feedbackText.GetComponent<RectTransform>();
        feedbackRT.anchorMin = new Vector2(0, 0);
        feedbackRT.anchorMax = new Vector2(1, 0);
        feedbackRT.sizeDelta = new Vector2(0, 40);
        feedbackRT.anchoredPosition = new Vector2(0, -40); // Start below

        // Animate feedback text
        float elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.3f;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            
            feedbackRT.anchoredPosition = Vector2.Lerp(
                new Vector2(0, -40),
                new Vector2(0, 20),
                smoothT
            );
            feedbackText.color = new Color(
                1, 1, 1,
                Mathf.Lerp(0, 1, smoothT)
            );
            
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);

        // Hide the selector
        Hide();

        // Fade out feedback
        elapsed = 0f;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.3f;
            feedbackText.color = new Color(1, 1, 1, 1 - t);
            yield return null;
        }

        Destroy(feedbackObj);

        // Re-enable buttons for next time
        foreach (var button in suitButtons)
        {
            button.interactable = true;
        }
    }

    private void OnDisable()
    {
        // Only run cleanup if we've been initialized
        if (isInitialized)
        {
            // Reset state when disabled
            StopAllCoroutines();
            isAnimating = false;
            transform.localScale = Vector3.zero;
            canvasGroup.alpha = 0f;
        }
    }
} 