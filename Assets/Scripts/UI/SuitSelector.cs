using UnityEngine;
using UnityEngine.UI;

public class SuitSelector : MonoBehaviour
{
    private RectTransform rectTransform;
    private static readonly string[] suits = { "♥", "♦", "♣", "♠" };

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("SuitSelector requires a RectTransform component!");
            return;
        }

        // Set default size and anchors
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        
        CreateSuitButtons();
    }

    void CreateSuitButtons()
    {
        float buttonSize = 60f;
        float spacing = 10f;
        float totalWidth = (buttonSize + spacing) * 2;
        float totalHeight = (buttonSize + spacing) * 2;

        rectTransform.sizeDelta = new Vector2(totalWidth + spacing, totalHeight + spacing);

        for (int i = 0; i < suits.Length; i++)
        {
            int row = i / 2;
            int col = i % 2;

            GameObject buttonObj = new GameObject($"SuitButton_{suits[i]}");
            buttonObj.transform.SetParent(transform, false);

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = (suits[i] == "♥" || suits[i] == "♦") ? Color.red : Color.black;

            Button button = buttonObj.AddComponent<Button>();
            string suit = suits[i]; // Capture the suit for the lambda
            button.onClick.AddListener(() => OnSuitSelected(suit));

            // Create suit text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            Text suitText = textObj.AddComponent<Text>();
            suitText.text = suits[i];
            suitText.font = Font.CreateDynamicFontFromOSFont("Arial", 36);
            suitText.alignment = TextAnchor.MiddleCenter;
            suitText.color = Color.white;

            // Position the button
            RectTransform buttonRT = buttonObj.GetComponent<RectTransform>();
            buttonRT.sizeDelta = new Vector2(buttonSize, buttonSize);
            float x = (col * (buttonSize + spacing)) - (totalWidth / 4);
            float y = (row * (buttonSize + spacing)) - (totalHeight / 4);
            buttonRT.anchoredPosition = new Vector2(x, y);

            // Setup text rect transform
            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
        }
    }

    void OnSuitSelected(string suit)
    {
        GameManager.Instance.DeclareSuit(suit);
        gameObject.SetActive(false);
    }

    public void Show(Vector2 position)
    {
        rectTransform.anchoredPosition = position;
        gameObject.SetActive(true);
    }
} 