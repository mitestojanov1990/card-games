using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class CardSimulation : MonoBehaviour
{
    [Header("Player Hand Settings")]
    private List<GameObject> cardObjects = new List<GameObject>();
    private List<Card> playerHand = new List<Card>();

    // Reference to the hand panel.
    private RectTransform handPanel;

    [Header("Layout Settings")]
    public float raisedYOffset = 30f;
    public float spacing = 80f;
    public float maxRotationAngle = 10f;
    public float cardWidth = 100f;
    public float cardHeight = 150f;

    [Header("Deck Settings")]
    public RectTransform deckArea;
    public RectTransform discardArea;
    private GameObject drawnCard;
    private bool isCardDrawn = false;

    private Dictionary<Player, Text> playerInfoTexts = new Dictionary<Player, Text>();

    private CPUHandVisualizer cpuVisualizer;

    private TableDecorator tableDecorator;

    private Canvas canvas;  // Add this field

    private Text gameStatusText;
    private float statusDisplayTime = 2f;
    private float turnDelay = 0.5f;

    private GameObject statusPanel;
    private Text gameStateText;
    private Text turnCountText;
    private Text deckCountText;
    private Text specialEffectText;

    private void Start()
    {
        // Create GameManager if it doesn't exist
        if (FindFirstObjectByType<GameManager>() == null)
        {
            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<GameManager>();
        }
        
        SetupCanvasAndHandPanel();
        GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        GameManager.Instance.OnCardDiscarded += HandleCardDiscarded;
        GameManager.Instance.OnSuitDeclared += HandleSuitDeclared;
        GameManager.Instance.OnPlayerChanged += HandlePlayerChanged;
        GameManager.Instance.OnCardPlayed += HandleCardPlayed;
        GameManager.Instance.OnCardDrawn += HandleCardDrawn;
        SetupStatusText();
        SetupStatusPanel();
    }

    private void HandleGameStateChanged(GameManager.GameState newState)
    {
        switch (newState)
        {
            case GameManager.GameState.WaitingToStart:
                StartCoroutine(ShowStatusMessage("Welcome to Macau!", 3f));
                break;
            
            case GameManager.GameState.PlayerTurn:
                if (cardObjects.Count == 0)  // Initial deal
                {
                    StartCoroutine(ShowStatusMessage("Dealing cards..."));
                    for (int i = 0; i < GameManager.Instance.PlayerHandSize; i++)
                    {
                        Card card = GameManager.Instance.DrawCard();
                        if (card != null)
                        {
                            AddCardToHand(card);
                        }
                    }
                    ArrangeCards();
                }
                break;
            
            case GameManager.GameState.GameOver:
                string winner = GameManager.Instance.CurrentPlayer.Name;
                StartCoroutine(ShowStatusMessage($"Game Over! {winner} wins!", 5f));
                break;
        }
        UpdateStatusPanel();
    }

    private void HandleCardDiscarded(Card card)
    {
        StartCoroutine(ShowStatusMessage($"Top card: {card.Rank}{card.Suit}"));
        UpdateDiscardPileVisual(card);
        UpdateStatusPanel();
    }

    private void HandleSuitDeclared(string suit)
    {
        StartCoroutine(ShowStatusMessage($"Suit changed to {suit}!", 2f));
        
        // Create a temporary text notification
        GameObject notificationObj = new GameObject("SuitNotification");
        notificationObj.transform.SetParent(handPanel.parent);
        Text notificationText = notificationObj.AddComponent<Text>();
        notificationText.text = $"Suit changed to {suit}";
        notificationText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        notificationText.alignment = TextAnchor.MiddleCenter;
        notificationText.color = suit == "♥" || suit == "♦" ? Color.red : Color.black;

        RectTransform notificationRT = notificationObj.GetComponent<RectTransform>();
        notificationRT.anchorMin = new Vector2(0.5f, 0.5f);
        notificationRT.anchorMax = new Vector2(0.5f, 0.5f);
        notificationRT.sizeDelta = new Vector2(300, 50);
        notificationRT.anchoredPosition = Vector2.zero;

        // Destroy after 2 seconds
        Destroy(notificationObj, 2f);
        UpdateStatusPanel();
    }

    private void HandlePlayerChanged(Player player)
    {
        foreach (var text in playerInfoTexts.Values)
        {
            text.color = Color.black;
        }
        
        if (playerInfoTexts.ContainsKey(player))
        {
            playerInfoTexts[player].color = Color.green;
            StartCoroutine(ShowStatusMessage($"{player.Name}'s turn"));
        }
        UpdateStatusPanel();
    }

    private void AddCardToHand(Card card)
    {
        GameObject cardObj = CreateCardUI(card);
        cardObj.transform.SetParent(handPanel, false);
        cardObjects.Add(cardObj);
        GameManager.Instance.AddCardToHand(card);
    }

    // Creates a card UI element
    GameObject CreateCardUI(Card card)
    {
        GameObject cardObj = new GameObject("Card");
        Image bg = cardObj.AddComponent<Image>();
        bg.color = Color.white;
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(cardWidth, cardHeight);
        rt.pivot = new Vector2(0.5f, 0);

        Button button = cardObj.AddComponent<Button>();
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.onClick.AddListener(() => OnCardClicked(cardObj, card));

        GameObject textObj = new GameObject("CardText");
        textObj.transform.SetParent(cardObj.transform, false);
        Text cardText = textObj.AddComponent<Text>();
        cardText.text = $"{card.Rank}{card.Suit}";
        cardText.alignment = TextAnchor.MiddleCenter;
        cardText.color = card.Color;
        cardText.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
        cardText.resizeTextForBestFit = true;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        return cardObj;
    }

    void OnDeckClicked()
    {
        Debug.Log($"Deck clicked. Current state: {GameManager.Instance.CurrentState}");
        
        if (GameManager.Instance.CurrentState != GameManager.GameState.PlayerTurn)
        {
            Debug.Log("Cannot draw - game not in PlayerTurn state");
            return;
        }

        if (isCardDrawn)
        {
            Debug.Log("Cannot draw - card already drawn");
            return;
        }

        StartCoroutine(ShowStatusMessage("Drawing card..."));
        Card card = GameManager.Instance.DrawCard();
        if (card != null)
        {
            Debug.Log($"Drew card: {card.Rank}{card.Suit}");
            
            // Check if the card can be played
            if (GameManager.Instance.IsValidPlay(card))
            {
                // Show the card with Keep/Discard options
                drawnCard = CreateCardUI(card);
                drawnCard.transform.SetParent(handPanel.parent);
                
                RectTransform rt = drawnCard.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = deckArea.anchoredPosition + new Vector2(0, cardHeight * 0.5f);

                GameObject keepBtn = CreateActionButton("Keep", new Vector2(-25, -cardHeight/2), () => KeepCard(card));
                GameObject discardBtn = CreateActionButton("Discard", new Vector2(25, -cardHeight/2), () => DiscardDrawnCard(card));
                
                keepBtn.transform.SetParent(drawnCard.transform, false);
                discardBtn.transform.SetParent(drawnCard.transform, false);
            }
            else
            {
                StartCoroutine(HandleAutoKeepCard(card));
            }
            
            isCardDrawn = true;
        }
        else
        {
            Debug.LogError("Failed to draw card - card is null");
        }
    }

    void OnCardClicked(GameObject cardObj, Card card)
    {
        if (GameManager.Instance.IsValidPlay(card))
        {
            StartCoroutine(HandleCardPlay(cardObj, card));
        }
        else
        {
            StartCoroutine(ShowStatusMessage("Cannot play this card!", 1f));
        }
    }

    private IEnumerator HandleCardPlay(GameObject cardObj, Card card)
    {
        StartCoroutine(ShowStatusMessage($"Playing {card.Rank}{card.Suit}"));
        
        GameManager.Instance.RemoveCardFromHand(card);
        cardObjects.Remove(cardObj);
        GameManager.Instance.DiscardCard(card);
        
        // Move card to discard pile
        cardObj.transform.SetParent(handPanel.parent);
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        rt.anchoredPosition = discardArea.anchoredPosition;
        Destroy(cardObj, 0.5f);
        
        ArrangeCards();
        
        yield return new WaitForSeconds(0.5f);
        
        GameManager.Instance.CheckGameEnd();
        if (GameManager.Instance.CurrentState == GameManager.GameState.GameOver)
        {
            StartCoroutine(ShowStatusMessage("Game Over!", 3f));
        }
        else
        {
            StartCoroutine(ShowStatusMessage("Ending turn...", 0.5f));
            yield return new WaitForSeconds(0.5f);
            GameManager.Instance.NextTurn();
        }
        UpdateStatusPanel();
    }

    private void UpdateDiscardPileVisual(Card card)
    {
        // Create visual for top card of discard pile
        GameObject discardVisual = CreateCardUI(card);
        discardVisual.transform.SetParent(discardArea, false);
        RectTransform rt = discardVisual.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
    }

    // Sets up (or finds) the Canvas and creates a HandPanel at the bottom of the screen.
    void SetupCanvasAndHandPanel()
    {
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Add table decorator first so it's behind everything
        tableDecorator = TableDecorator.Create(canvas.transform);
        tableDecorator.Initialize();

        // Check if UIManager already exists
        UIManager uiManager = FindFirstObjectByType<UIManager>();
        if (uiManager == null)
        {
            GameObject uiManagerObj = new GameObject("UIManager");
            uiManagerObj.transform.SetParent(canvas.transform);
            uiManager = uiManagerObj.AddComponent<UIManager>();

            // Create UI elements for UIManager
            GameObject stateTextObj = new GameObject("GameStateText");
            stateTextObj.transform.SetParent(uiManagerObj.transform);
            Text stateText = stateTextObj.AddComponent<Text>();
            stateText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            stateText.alignment = TextAnchor.MiddleCenter;
            stateText.color = Color.black;  // Make text visible
            RectTransform stateRT = stateTextObj.GetComponent<RectTransform>();
            stateRT.anchorMin = new Vector2(0.5f, 1);
            stateRT.anchorMax = new Vector2(0.5f, 1);
            stateRT.pivot = new Vector2(0.5f, 1);
            stateRT.sizeDelta = new Vector2(400, 50);
            stateRT.anchoredPosition = new Vector2(0, -25);
            uiManager.gameStateText = stateText;

            // Create start button
            GameObject startButtonObj = new GameObject("StartButton");
            startButtonObj.transform.SetParent(uiManagerObj.transform);
            Image startBtnImage = startButtonObj.AddComponent<Image>();
            startBtnImage.color = Color.white;
            Button startBtn = startButtonObj.AddComponent<Button>();

            // Add visual feedback
            ColorBlock colors = startBtn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            startBtn.colors = colors;

            RectTransform startBtnRT = startButtonObj.GetComponent<RectTransform>();
            startBtnRT.anchorMin = new Vector2(0.5f, 0.5f);  // Center in screen
            startBtnRT.anchorMax = new Vector2(0.5f, 0.5f);
            startBtnRT.pivot = new Vector2(0.5f, 0.5f);
            startBtnRT.sizeDelta = new Vector2(200, 50);
            startBtnRT.anchoredPosition = new Vector2(0, 100);  // 100 units above center

            GameObject startBtnTextObj = new GameObject("Text");
            startBtnTextObj.transform.SetParent(startButtonObj.transform);
            Text startBtnText = startBtnTextObj.AddComponent<Text>();
            startBtnText.text = "START GAME";
            startBtnText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            startBtnText.alignment = TextAnchor.MiddleCenter;
            startBtnText.color = Color.black;

            RectTransform textRT = startBtnTextObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            uiManager.startGameButton = startBtn;

            // Create game over panel
            GameObject gameOverObj = new GameObject("GameOverPanel");
            gameOverObj.transform.SetParent(uiManagerObj.transform);
            gameOverObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.8f);
            RectTransform gameOverRT = gameOverObj.GetComponent<RectTransform>();
            gameOverRT.anchorMin = Vector2.zero;
            gameOverRT.anchorMax = Vector2.one;
            gameOverRT.offsetMin = Vector2.zero;
            gameOverRT.offsetMax = Vector2.zero;
            gameOverObj.SetActive(false);
            uiManager.gameOverPanel = gameOverObj;
        }

        GameObject handPanelObj = new GameObject("HandPanel");
        handPanelObj.transform.SetParent(canvas.transform);
        handPanel = handPanelObj.AddComponent<RectTransform>();
        // Anchor at bottom center.
        handPanel.anchorMin = new Vector2(0.5f, 0);
        handPanel.anchorMax = new Vector2(0.5f, 0);
        handPanel.pivot = new Vector2(0.5f, 0); // Bottom-center pivot.
        // Make the panel wide enough to hold cards and a bit extra for raised cards.
        handPanel.sizeDelta = new Vector2(800, cardHeight + raisedYOffset + 20);
        // Position flush at the bottom.
        handPanel.anchoredPosition = new Vector2(0, 0);

        // Add deck area
        GameObject deckObj = new GameObject("DeckArea");
        deckObj.transform.SetParent(canvas.transform);
        deckArea = deckObj.AddComponent<RectTransform>();
        Image deckImage = deckObj.AddComponent<Image>();
        deckImage.color = Color.green;
        Button deckButton = deckObj.AddComponent<Button>();
        deckButton.onClick.RemoveAllListeners();  // Clear any existing listeners
        deckButton.onClick.AddListener(OnDeckClicked);

        // Add visual feedback
        ColorBlock deckColors = deckButton.colors;
        deckColors.normalColor = Color.green;
        deckColors.highlightedColor = new Color(0, 0.8f, 0, 1);
        deckColors.pressedColor = new Color(0, 0.6f, 0, 1);
        deckButton.colors = deckColors;

        // Position deck in center
        deckArea.anchorMin = new Vector2(0.5f, 0.5f);
        deckArea.anchorMax = new Vector2(0.5f, 0.5f);
        deckArea.pivot = new Vector2(0.5f, 0.5f);
        deckArea.sizeDelta = new Vector2(cardWidth, cardHeight);
        deckArea.anchoredPosition = new Vector2(-cardWidth/2, 0);

        // Add deck text
        GameObject deckTextObj = new GameObject("DeckText");
        deckTextObj.transform.SetParent(deckObj.transform, false);
        Text deckText = deckTextObj.AddComponent<Text>();
        deckText.text = "DECK";
        deckText.alignment = TextAnchor.MiddleCenter;
        deckText.color = Color.white;
        deckText.font = Font.CreateDynamicFontFromOSFont("Arial", 20);

        RectTransform deckTextRT = deckTextObj.GetComponent<RectTransform>();
        deckTextRT.anchorMin = Vector2.zero;
        deckTextRT.anchorMax = Vector2.one;
        deckTextRT.offsetMin = Vector2.zero;
        deckTextRT.offsetMax = Vector2.zero;

        // Add discard area
        GameObject discardObj = new GameObject("DiscardArea");
        discardObj.transform.SetParent(canvas.transform);
        discardArea = discardObj.AddComponent<RectTransform>();
        Image discardImage = discardObj.AddComponent<Image>();
        discardImage.color = new Color(0.8f, 0.8f, 0.8f, 0.5f);

        // Position discard area next to deck
        discardArea.anchorMin = new Vector2(0.5f, 0.5f);
        discardArea.anchorMax = new Vector2(0.5f, 0.5f);
        discardArea.pivot = new Vector2(0.5f, 0.5f);
        discardArea.sizeDelta = new Vector2(cardWidth, cardHeight);
        discardArea.anchoredPosition = new Vector2(cardWidth/2, 0);

        cpuVisualizer = CPUHandVisualizer.Create(canvas.transform);
        cpuVisualizer.Initialize();

        CreatePlayerInfoTexts();
    }

    void CreatePlayerInfoTexts()
    {
        foreach (Player player in GameManager.Instance.Players)
        {
            GameObject textObj = new GameObject($"PlayerInfo_{player.Name}");
            textObj.transform.SetParent(canvas.transform);
            Text text = textObj.AddComponent<Text>();
            text.text = $"{player.Name}: {player.Hand.Count} cards";
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 20);
            text.alignment = TextAnchor.MiddleLeft;

            RectTransform rt = textObj.GetComponent<RectTransform>();
            if (player.IsHuman)
            {
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot = new Vector2(0, 0);
                rt.anchoredPosition = new Vector2(20, 20);
            }
            else
            {
                int playerIndex = GameManager.Instance.GetPlayerIndex(player);
                Vector2 handPos = cpuVisualizer.GetHandPosition(playerIndex);
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = handPos + new Vector2(0, -cardHeight/2 - 20);
                rt.rotation = cpuVisualizer.GetHandRotation(playerIndex);
            }
            rt.sizeDelta = new Vector2(200, 30);

            playerInfoTexts[player] = text;
        }
    }

    // Arranges the cards in a fanned layout.
    void ArrangeCards()
    {
        float startX = -((cardObjects.Count - 1) * spacing) / 2f;
        float midIndex = (cardObjects.Count - 1) / 2f;

        for (int i = 0; i < cardObjects.Count; i++)
        {
            RectTransform rt = cardObjects[i].GetComponent<RectTransform>();
            float posX = startX + i * spacing;
            rt.anchoredPosition = new Vector2(posX, 0);
            // Rotate relative to center.
            float angle = (midIndex != 0) ? maxRotationAngle * ((i - midIndex) / midIndex) : 0;
            rt.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

    GameObject CreateActionButton(string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(text + "Button");
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(50, 20);
        rt.anchoredPosition = position;

        Image btnImage = btnObj.AddComponent<Image>();
        btnImage.color = Color.white;

        Button btn = btnObj.AddComponent<Button>();
        btn.onClick.AddListener(action);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        Text btnText = textObj.AddComponent<Text>();
        btnText.text = text;
        btnText.alignment = TextAnchor.MiddleCenter;
        btnText.color = Color.black;
        btnText.font = Font.CreateDynamicFontFromOSFont("Arial", 12);

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        return btnObj;
    }

    void KeepCard(Card card)
    {
        if (drawnCard != null)
        {
            StartCoroutine(ShowStatusMessage("Keeping card"));
            
            // Remove the action buttons
            foreach (Transform child in drawnCard.transform)
            {
                if (child.name.Contains("Button"))
                {
                    Destroy(child.gameObject);
                }
            }

            AddCardToHand(card);
            drawnCard.transform.SetParent(handPanel, false);
            ArrangeCards();
            drawnCard = null;
            isCardDrawn = false;
            
            StartCoroutine(ShowStatusMessage("Ending turn...", 0.5f));
            GameManager.Instance.NextTurn();
        }
    }

    void DiscardDrawnCard(Card card)
    {
        if (drawnCard != null)
        {
            StartCoroutine(ShowStatusMessage($"Discarding {card.Rank}{card.Suit}"));
            
            // Move card to discard pile
            drawnCard.transform.SetParent(handPanel.parent);
            RectTransform rt = drawnCard.GetComponent<RectTransform>();
            rt.anchoredPosition = discardArea.anchoredPosition;
            Destroy(drawnCard, 0.5f);
            drawnCard = null;
            isCardDrawn = false;
            
            StartCoroutine(ShowStatusMessage("Ending turn...", 0.5f));
            GameManager.Instance.NextTurn();
        }
    }

    private void HandleCardPlayed(Card card, Player player)
    {
        // Get positions for animation
        Vector2 startPos = GetPlayerHandPosition(player);
        Vector2 endPos = discardArea.anchoredPosition;
        
        // Create animated card
        CardAnimation.CreateAnimatedCard(
            card, 
            handPanel.parent, 
            startPos,
            endPos
        );
    }

    private void HandleCardDrawn(Card card, Player player)
    {
        // Get positions for animation
        Vector2 startPos = deckArea.anchoredPosition;
        Vector2 endPos = GetPlayerHandPosition(player);
        
        // Create animated card
        CardAnimation.CreateAnimatedCard(
            card, 
            handPanel.parent, 
            startPos,
            endPos
        );
    }

    private Vector2 GetPlayerHandPosition(Player player)
    {
        // Calculate position based on player index
        int playerIndex = GameManager.Instance.GetPlayerIndex(player);
        float yOffset = Screen.height * 0.1f;
        return new Vector2(20, -yOffset - (playerIndex * 40));
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            GameManager.Instance.OnCardDiscarded -= HandleCardDiscarded;
            GameManager.Instance.OnSuitDeclared -= HandleSuitDeclared;
            GameManager.Instance.OnPlayerChanged -= HandlePlayerChanged;
            GameManager.Instance.OnCardPlayed -= HandleCardPlayed;
            GameManager.Instance.OnCardDrawn -= HandleCardDrawn;
        }
    }

    void SetupStatusText()
    {
        GameObject statusObj = new GameObject("GameStatusText");
        statusObj.transform.SetParent(canvas.transform);
        gameStatusText = statusObj.AddComponent<Text>();
        gameStatusText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
        gameStatusText.alignment = TextAnchor.MiddleCenter;
        gameStatusText.color = Color.black;

        RectTransform statusRT = statusObj.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0.5f, 1);
        statusRT.anchorMax = new Vector2(0.5f, 1);
        statusRT.pivot = new Vector2(0.5f, 1);
        statusRT.sizeDelta = new Vector2(400, 50);
        statusRT.anchoredPosition = new Vector2(0, -100); // Below game state text
    }

    private IEnumerator ShowStatusMessage(string message, float duration = 2f)
    {
        if (gameStatusText != null)
        {
            string fullMessage = message;
            
            // Add relevant game state info
            if (GameManager.Instance.IsSequentialPlayActive)
                fullMessage += "\nSequential Play Active";
            if (GameManager.Instance.CurrentDrawAmount > 0)
                fullMessage += $"\nDraw {GameManager.Instance.CurrentDrawAmount} Cards";
            if (GameManager.Instance.DeclaredSuit != null)
                fullMessage += $"\nDeclared Suit: {GameManager.Instance.DeclaredSuit}";

            gameStatusText.text = fullMessage;
            gameStatusText.color = new Color(0, 0, 0, 1);
            
            // Use duration parameter instead of statusDisplayTime
            yield return new WaitForSeconds(duration - 0.5f);
            float elapsed = 0;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                gameStatusText.color = new Color(0, 0, 0, 1 - (elapsed / 0.5f));
                yield return null;
            }
            gameStatusText.text = "";
        }
    }

    private IEnumerator HandleAutoKeepCard(Card card)
    {
        StartCoroutine(ShowStatusMessage("No valid play - keeping card"));
        
        // Create temporary visual for the drawn card
        GameObject tempCard = CreateCardUI(card);
        tempCard.transform.SetParent(handPanel.parent);
        RectTransform rt = tempCard.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = deckArea.anchoredPosition;

        // Animate card movement
        float duration = 0.5f;
        float elapsed = 0;
        Vector2 startPos = deckArea.anchoredPosition;
        Vector2 endPos = handPanel.anchoredPosition + new Vector2(0, cardHeight/2);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        // Add to hand and clean up
        Destroy(tempCard);
        AddCardToHand(card);
        ArrangeCards();
        
        // Wait before ending turn
        yield return new WaitForSeconds(turnDelay);
        StartCoroutine(ShowStatusMessage("Ending turn..."));
        yield return new WaitForSeconds(0.5f);
        
        isCardDrawn = false;
        GameManager.Instance.NextTurn();
    }

    private void SetupStatusPanel()
    {
        // Create status panel
        statusPanel = new GameObject("StatusPanel");
        statusPanel.transform.SetParent(canvas.transform);
        Image panelImage = statusPanel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.1f);

        RectTransform panelRT = statusPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(1, 1);
        panelRT.anchorMax = new Vector2(1, 1);
        panelRT.pivot = new Vector2(1, 1);
        panelRT.sizeDelta = new Vector2(250, 150);
        panelRT.anchoredPosition = new Vector2(-20, -20);

        // Game state text
        gameStateText = CreateStatusText("GameState: Waiting", 0);
        turnCountText = CreateStatusText("Turn: 1", 1);
        deckCountText = CreateStatusText("Cards in deck: 52", 2);
        specialEffectText = CreateStatusText("", 3);

        UpdateStatusPanel();
    }

    private Text CreateStatusText(string initialText, int position)
    {
        GameObject textObj = new GameObject($"StatusText_{position}");
        textObj.transform.SetParent(statusPanel.transform);
        Text text = textObj.AddComponent<Text>();
        text.text = initialText;
        text.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        text.alignment = TextAnchor.MiddleLeft;
        text.color = Color.black;

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 1);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.pivot = new Vector2(0, 1);
        textRT.sizeDelta = new Vector2(0, 30);
        textRT.anchoredPosition = new Vector2(10, -35 * position - 10);

        return text;
    }

    private void UpdateStatusPanel()
    {
        if (GameManager.Instance == null) return;

        // Update game state
        gameStateText.text = $"State: {GameManager.Instance.CurrentState}";
        
        // Update turn count (add TurnCount property to GameManager)
        turnCountText.text = $"Turn: {GameManager.Instance.TurnCount}";
        
        // Update deck count
        deckCountText.text = $"Cards in deck: {GameManager.Instance.DeckCount}";
        
        // Update special effects
        string effects = "";
        if (GameManager.Instance.IsSequentialPlayActive)
            effects += "Sequential Play Active\n";
        if (GameManager.Instance.CurrentDrawAmount > 0)
            effects += $"Draw {GameManager.Instance.CurrentDrawAmount} Cards\n";
        if (GameManager.Instance.DeclaredSuit != null)
            effects += $"Declared Suit: {GameManager.Instance.DeclaredSuit}";
        
        specialEffectText.text = effects;
    }
}
