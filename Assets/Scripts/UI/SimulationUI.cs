using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using CardGame.Core;
using CardGame.Players;
using CardGame.Rules;
using CardGame.Utils;

namespace CardGame.UI
{
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

        [Header("Replay")]
        private GameObject replayButton;
        private float replayButtonWidth = 200f;
        private float replayButtonHeight = 60f;

        private CardDrawAnimation drawAnimation;
        private CardPlayAnimation playAnimation;

        [Header("Error Display")]
        private GameObject errorPanel;
        private Text errorText;
        private float errorDisplayDuration = 5f;

        private IGameManager gameManager;

        private void Awake()
        {
            gameManager = GameManager.Instance as IGameManager;
            if (gameManager == null)
            {
                Debug.LogError("Could not access IGameManager interface");
            }
        }

        private void Start()
        {
            CreateSimulationButton();
            CreateStatsPanel();
            CreateNotificationPanel();
            CreateDrawCountPanel();
            CreateReplayButton();
            CreateErrorPanel();
            
            if (gameManager != null)
            {
                SubscribeToEvents();
            }
            else
            {
                Debug.LogError("GameManager interface not available!");
            }

            SetupDrawAnimation();
            SetupPlayAnimation();
            
            // Subscribe to error events
            ErrorHandler.OnError += HandleError;
            ErrorHandler.OnWarning += HandleWarning;
        }

        private void SubscribeToEvents()
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;
            gameManager.OnDrawCardsEffect += HandleDrawCardsEffect;
            gameManager.OnSequentialPlayChanged += HandleSequentialPlayChanged;
            gameManager.OnSuitDeclared += HandleSuitDeclared;
            gameManager.OnSkipTurn += HandleSkipTurn;
            gameManager.OnCardPlayed += HandleCardPlayed;
            gameManager.OnCardDrawn += HandleCardDrawn;
        }

        private void UnsubscribeFromEvents()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= HandleGameStateChanged;
                gameManager.OnDrawCardsEffect -= HandleDrawCardsEffect;
                gameManager.OnSequentialPlayChanged -= HandleSequentialPlayChanged;
                gameManager.OnSuitDeclared -= HandleSuitDeclared;
                gameManager.OnSkipTurn -= HandleSkipTurn;
                gameManager.OnCardPlayed -= HandleCardPlayed;
                gameManager.OnCardDrawn -= HandleCardDrawn;
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
            if (gameManager != null)
            {
                gameManager.InitializeSimulation(3, 0.5f, true);
            }
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                var winner = gameManager.Players.FirstOrDefault(p => p.Hand.Count == 0);
                if (winner != null)
                {
                    ShowNotification($"Game Over!\n{winner.Name} wins!", 0f); // Don't auto-hide
                }
                else
                {
                    // Handle deck empty case
                    var winnerByCards = gameManager.Players.OrderBy(p => p.Hand.Count).First();
                    ShowNotification($"Game Over - Deck empty!\n{winnerByCards.Name} wins with fewest cards!", 0f);
                }
                
                // Show replay button
                replayButton.SetActive(true);
                
                UpdateStatsDisplay();
            }
            else
            {
                // Hide replay button during gameplay
                replayButton.SetActive(false);
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
            var gameOverText = gameManager.CurrentState == GameState.GameOver ? 
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

        private void CreateReplayButton()
        {
            replayButton = new GameObject("ReplayButton");
            replayButton.transform.SetParent(transform, false);

            Image buttonImage = replayButton.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.6f, 0.2f); // Green color

            Button button = replayButton.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.2f, 0.6f, 0.2f);
            colors.highlightedColor = new Color(0.3f, 0.7f, 0.3f);
            colors.pressedColor = new Color(0.1f, 0.5f, 0.1f);
            button.colors = colors;

            RectTransform buttonRT = replayButton.GetComponent<RectTransform>();
            buttonRT.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRT.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRT.pivot = new Vector2(0.5f, 0.5f);
            buttonRT.sizeDelta = new Vector2(replayButtonWidth, replayButtonHeight);
            buttonRT.anchoredPosition = new Vector2(0, -100); // Position below center

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(replayButton.transform, false);
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = "Play Again";
            buttonText.font = Font.CreateDynamicFontFromOSFont("Arial", fontSize);
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;

            button.onClick.AddListener(RestartGame);
            
            // Hide initially
            replayButton.SetActive(false);
        }

        private void RestartGame()
        {
            if (gameManager != null)
            {
                // Hide UI elements
                replayButton.SetActive(false);
                if (notificationPanel != null) notificationPanel.SetActive(false);
                if (drawCountPanel != null) drawCountPanel.SetActive(false);
                
                // Start new game with same settings
                if (gameManager.CurrentState == GameState.GameOver)
                {
                    gameManager.StartNewGame();
                }
            }
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
                // Hide both panels when effect is complete
                drawCountPanel.SetActive(false);
                HideNotification();
            }
        }

        private void HandleCardPlayed(ICard card, IPlayer player)
        {
            // Get card position based on player
            Vector2 startPos;
            if (player.IsHuman)
            {
                startPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.2f);
            }
            else
            {
                // Position based on player index
                var players = gameManager.Players;
                int playerIndex = players.ToList().IndexOf(player);
                float angle = (playerIndex * 360f / players.Count) * Mathf.Deg2Rad;
                startPos = new Vector2(
                    Screen.width * 0.5f + Mathf.Cos(angle) * 300f,
                    Screen.height * 0.5f + Mathf.Sin(angle) * 200f
                );
            }

            var concreteCard = card as Card;
            if (concreteCard != null)
            {
                bool isSpecial = CardRules.Instance.GetCardEffect(concreteCard) != SpecialEffect.None;
                playAnimation.QueueCardPlay(concreteCard, startPos, isSpecial);

                // Clear draw notifications if a counter card was played
                if (gameManager.CurrentDrawAmount == 0)
                {
                    drawCountPanel.SetActive(false);
                    HideNotification();
                }
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
            
            if (duration > 0)
            {
                CancelInvoke(nameof(HideNotification));
                Invoke(nameof(HideNotification), duration);
            }
        }

        private void HideNotification()
        {
            if (notificationPanel != null)
            {
                notificationPanel.SetActive(false);
            }
        }

        private void SetupDrawAnimation()
        {
            GameObject animObj = new GameObject("CardDrawAnimation");
            animObj.transform.SetParent(transform, false);
            drawAnimation = animObj.AddComponent<CardDrawAnimation>();
            
            // Set deck and hand positions (adjust these based on your layout)
            Vector2 deckPos = new Vector2(Screen.width * 0.2f, Screen.height * 0.5f);
            Vector2 handPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.2f);
            drawAnimation.Initialize(deckPos, handPos);
        }

        private void SetupPlayAnimation()
        {
            GameObject animObj = new GameObject("CardPlayAnimation");
            animObj.transform.SetParent(transform, false);
            playAnimation = animObj.AddComponent<CardPlayAnimation>();
            
            // Set discard pile position (adjust based on your layout)
            Vector2 discardPos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            playAnimation.Initialize(discardPos);
        }

        private void HandleCardDrawn(ICard card, IPlayer player)
        {
            if (player.IsHuman)
            {
                var concreteCard = card as Card;
                if (concreteCard != null)
                {
                    drawAnimation.QueueCardDraw(concreteCard);
                }
            }
        }

        private void CreateErrorPanel()
        {
            errorPanel = new GameObject("ErrorPanel");
            errorPanel.transform.SetParent(transform, false);

            Image panelImage = errorPanel.AddComponent<Image>();
            panelImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);

            RectTransform panelRT = errorPanel.GetComponent<RectTransform>();
            panelRT.anchorMin = new Vector2(0.5f, 0);
            panelRT.anchorMax = new Vector2(0.5f, 0);
            panelRT.pivot = new Vector2(0.5f, 0);
            panelRT.sizeDelta = new Vector2(400, 80);
            panelRT.anchoredPosition = new Vector2(0, 100);

            GameObject textObj = new GameObject("ErrorText");
            textObj.transform.SetParent(errorPanel.transform, false);
            errorText = textObj.AddComponent<Text>();
            errorText.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            errorText.alignment = TextAnchor.MiddleCenter;
            errorText.color = Color.white;

            RectTransform textRT = textObj.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.sizeDelta = Vector2.zero;
            textRT.offsetMin = new Vector2(10, 10);
            textRT.offsetMax = new Vector2(-10, -10);

            errorPanel.SetActive(false);
        }

        private void HandleError(string message)
        {
            ShowError(message, Color.red);
        }

        private void HandleWarning(string message)
        {
            ShowError(message, new Color(1f, 0.6f, 0));
        }

        private void ShowError(string message, Color color)
        {
            errorText.text = message;
            Image panelImage = errorPanel.GetComponent<Image>();
            panelImage.color = new Color(color.r, color.g, color.b, 0.9f);
            errorPanel.SetActive(true);

            CancelInvoke(nameof(HideError));
            Invoke(nameof(HideError), errorDisplayDuration);
        }

        private void HideError()
        {
            if (errorPanel != null)
            {
                errorPanel.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            ErrorHandler.OnError -= HandleError;
            ErrorHandler.OnWarning -= HandleWarning;
        }
    }
} 