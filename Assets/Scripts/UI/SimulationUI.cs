using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class SimulationUI : MonoBehaviour
{
    [Header("UI Settings")]
    private float buttonWidth = 200f;
    private float buttonHeight = 60f;
    private int fontSize = 24;

    [Header("Stats Panel")]
    private float panelWidth = 400f;
    private float panelHeight = 600f;
    private int statsFontSize = 20;
    private float lineSpacing = 1.2f;

    private Text statsText;
    private ScrollRect scrollRect;

    private void Start()
    {
        CreateSimulationButton();
        CreateStatsPanel();
    }

    private void CreateSimulationButton()
    {
        GameObject buttonObj = new GameObject("SimulationButton");
        buttonObj.transform.SetParent(transform, false);

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.2f, 0.2f);

        Button button = buttonObj.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.1f);
        button.colors = colors;

        RectTransform buttonRT = buttonObj.GetComponent<RectTransform>();
        buttonRT.anchorMin = new Vector2(1, 1);
        buttonRT.anchorMax = new Vector2(1, 1);
        buttonRT.pivot = new Vector2(1, 1);
        buttonRT.sizeDelta = new Vector2(buttonWidth, buttonHeight);
        buttonRT.anchoredPosition = new Vector2(-20, -20);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = "Start Simulation";
        buttonText.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.color = Color.white;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        button.onClick.AddListener(StartSimulation);
    }

    private void CreateStatsPanel()
    {
        GameObject panelObj = new GameObject("StatsPanel");
        panelObj.transform.SetParent(transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.9f);

        RectTransform panelRT = panelObj.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1, 0);
        panelRT.anchorMax = new Vector2(1, 1);
        panelRT.pivot = new Vector2(1, 0.5f);
        panelRT.sizeDelta = new Vector2(panelWidth, 0);
        panelRT.anchoredPosition = new Vector2(-20, 0);

        // Add scroll view
        GameObject scrollObj = new GameObject("ScrollView");
        scrollObj.transform.SetParent(panelObj.transform, false);
        scrollRect = scrollObj.AddComponent<ScrollRect>();
        
        RectTransform scrollRT = scrollObj.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.sizeDelta = Vector2.zero;
        scrollRT.offsetMin = new Vector2(10, 10);
        scrollRT.offsetMax = new Vector2(-10, -10);

        // Add content
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(scrollObj.transform, false);
        RectTransform contentRT = contentObj.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        
        // Add stats text
        statsText = contentObj.AddComponent<Text>();
        statsText.font = Font.CreateDynamicFontFromOSFont("Arial", statsFontSize);
        statsText.color = Color.white;
        statsText.lineSpacing = lineSpacing;
        statsText.supportRichText = true;

        scrollRect.content = contentRT;
        scrollRect.viewport = scrollRT;
        scrollRect.vertical = true;
        scrollRect.horizontal = false;

        // Initially hide the panel
        panelObj.SetActive(false);
    }

    private void StartSimulation()
    {
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.InitializeSimulation(3, 0.5f, true);
            
            // Subscribe to statistics updates
            GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver)
        {
            UpdateStatsDisplay();
        }
    }

    private void UpdateStatsDisplay()
    {
        if (statsText != null)
        {
            statsText.text = FormatStatsText();
            
            // Adjust content height based on text
            var contentRT = statsText.GetComponent<RectTransform>();
            contentRT.sizeDelta = new Vector2(0, statsText.preferredHeight);
            
            // Show the panel
            statsText.transform.parent.parent.gameObject.SetActive(true);
        }
    }

    private string FormatStatsText()
    {
        var gameManager = GameManager.Instance;
        return $"<size=32><b>Game Statistics</b></size>\n\n" +
               $"<size=24>Total Turns: {gameManager.TurnCount}</size>\n" +
               $"<size=24>Deck Cards: {gameManager.DeckCount}</size>\n\n" +
               $"<size=28><b>Players:</b></size>\n" +
               string.Join("\n", gameManager.Players.Select(p => 
                   $"  <size=24>{p.Name}: {p.Hand.Count} cards</size>")) +
               $"\n\n<size=24>Current Player: {gameManager.CurrentPlayer.Name}</size>";
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }
    }
} 