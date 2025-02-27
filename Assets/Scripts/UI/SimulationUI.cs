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

    [Header("Effect Notifications")]
    private float notificationDuration = 3f;
    private GameObject notificationPanel;
    private Text notificationText;
    private GameObject drawCountPanel;
    private Text drawCountText;

    private void Start()
    {
        CreateSimulationButton();
        CreateStatsPanel();
        CreateNotificationPanel();
        CreateDrawCountPanel();
        
        // Subscribe to game events
        var gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            gameManager.OnDrawCardsEffect += HandleDrawCardsEffect;
            gameManager.OnSequentialPlayChanged += HandleSequentialPlayChanged;
            gameManager.OnSuitDeclared += HandleSuitDeclared;
            gameManager.OnSkipTurn += HandleSkipTurn;
        }
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
            var winner = GameManager.Instance.Players.First(p => p.Hand.Count == 0);
            if (winner != null)
            {
                ShowNotification($"Game Over!\n{winner.Name} wins!", 5f); // Show for longer
            }
            else
            {
                // Handle deck empty case
                var winnerByCards = GameManager.Instance.Players.OrderBy(p => p.Hand.Count).First();
                ShowNotification($"Game Over - Deck empty!\n{winnerByCards.Name} wins with fewest cards!", 5f);
            }
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
        var gameOverText = gameManager.CurrentState == GameManager.GameState.GameOver ? 
            "<color=yellow>Game Over!</color>\n\n" : "";

        return $"<size=32><b>Game Statistics</b></size>\n\n" +
               gameOverText +
               $"<size=24>Total Turns: {gameManager.TurnCount}</size>\n" +
               $"<size=24>Deck Cards: {gameManager.DeckCount}</size>\n\n" +
               $"<size=28><b>Players:</b></size>\n" +
               string.Join("\n", gameManager.Players.Select(p => 
                   $"  <size=24>{p.Name}: {p.Hand.Count} cards</size>" +
                   (p.Hand.Count == 0 ? " <color=yellow>Winner!</color>" : ""))) +
               $"\n\n<size=24>Current Player: {gameManager.CurrentPlayer.Name}</size>";
    }

    private void CreateNotificationPanel()
    {
        notificationPanel = new GameObject("NotificationPanel");
        notificationPanel.transform.SetParent(transform, false);

        Image panelImage = notificationPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);

        RectTransform panelRT = notificationPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.8f);
        panelRT.anchorMax = new Vector2(0.5f, 0.8f);
        panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(400, 80);

        GameObject textObj = new GameObject("NotificationText");
        textObj.transform.SetParent(notificationPanel.transform, false);
        notificationText = textObj.AddComponent<Text>();
        notificationText.font = Font.CreateDynamicFontFromOSFont("Arial", 28);
        notificationText.alignment = TextAnchor.MiddleCenter;
        notificationText.color = Color.white;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;
        textRT.offsetMin = new Vector2(10, 10);
        textRT.offsetMax = new Vector2(-10, -10);

        notificationPanel.SetActive(false);
    }

    private void CreateDrawCountPanel()
    {
        drawCountPanel = new GameObject("DrawCountPanel");
        drawCountPanel.transform.SetParent(transform, false);

        Image panelImage = drawCountPanel.AddComponent<Image>();
        panelImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);

        RectTransform panelRT = drawCountPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0, 0);
        panelRT.anchorMax = new Vector2(0, 0);
        panelRT.pivot = new Vector2(0, 0);
        panelRT.sizeDelta = new Vector2(200, 60);
        panelRT.anchoredPosition = new Vector2(20, 20);

        GameObject textObj = new GameObject("DrawCountText");
        textObj.transform.SetParent(drawCountPanel.transform, false);
        drawCountText = textObj.AddComponent<Text>();
        drawCountText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        drawCountText.alignment = TextAnchor.MiddleCenter;
        drawCountText.color = Color.white;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.sizeDelta = Vector2.zero;

        drawCountPanel.SetActive(false);
    }

    private void HandleDrawCardsEffect(int drawCount)
    {
        if (drawCount > 0)
        {
            drawCountPanel.SetActive(true);
            drawCountText.text = $"Draw {drawCount} cards\nor play counter card!";
            
            ShowNotification($"Draw {drawCount} cards effect active!");
        }
        else
        {
            drawCountPanel.SetActive(false);
        }
    }

    private void HandleSequentialPlayChanged(bool active)
    {
        if (active)
        {
            ShowNotification("Sequential play active!\nPlay same rank card!");
        }
    }

    private void HandleSuitDeclared(string suit)
    {
        ShowNotification($"Suit changed to {suit}!");
    }

    private void HandleSkipTurn()
    {
        ShowNotification("Turn skipped!");
    }

    private void ShowNotification(string message, float duration = 3f)
    {
        notificationText.text = message;
        notificationPanel.SetActive(true);
        CancelInvoke(nameof(HideNotification));
        Invoke(nameof(HideNotification), duration);
    }

    private void HideNotification()
    {
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnDrawCardsEffect -= HandleDrawCardsEffect;
            GameManager.Instance.OnSequentialPlayChanged -= HandleSequentialPlayChanged;
            GameManager.Instance.OnSuitDeclared -= HandleSuitDeclared;
            GameManager.Instance.OnSkipTurn -= HandleSkipTurn;
        }
    }
} 