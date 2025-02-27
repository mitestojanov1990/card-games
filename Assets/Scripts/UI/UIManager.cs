using UnityEngine;
using UnityEngine.UI;
using CardGame.Core;
using CardGame.Utils;
using CardGame.Players;

namespace CardGame.UI
{
    public interface IUIManager
    {
        void ShowStartScreen();
        void UpdateGameUI();
        void ShowGameOverScreen();
        void SetUIScale(float scale);
        void ToggleDebugInfo();
    }
    public class UIManager : MonoBehaviour, IUIManager
    {
        public Text gameStateText;
        public Text scoreText;
        public Button startGameButton;
        public GameObject gameOverPanel;

        private IGameManager gameManager;

        private void Awake()
        {
            Debug.Log("UIManager: Awake called");
            // Do not access GameManager here, it may not be initialized yet
        }

        public void Initialize(IGameManager gameManager)
        {
            this.gameManager = gameManager; // Set the GameManager here
            SubscribeToEvents(); // Subscribe to events after initialization
        }

        public void ShowStartScreen()
        {
            if (gameStateText != null)
            {
                gameStateText.text = "Press Start to Begin";
            }

            if (startGameButton != null)
            {
                startGameButton.gameObject.SetActive(true);
                startGameButton.onClick.RemoveAllListeners();
                startGameButton.onClick.AddListener(StartNewGame);
            }

            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }

        public void UpdateGameUI()
        {
            if (scoreText != null && gameManager != null)
            {
                scoreText.text = $"Score: {gameManager.CurrentPlayer.Score}";
            }
        }

        public void ShowGameOverScreen()
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                gameStateText.text = "Game Over!";
            }
        }

        public void SetUIScale(float scale)
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.scaleFactor = scale;
            }
        }

        public void ToggleDebugInfo()
        {
            Debug.Log("Toggling debug info");
        }

        private void Start()
        {
            Debug.Log("UIManager: Start called");
            ShowStartScreen();

            if (gameManager != null)
            {
                SubscribeToEvents();
            }
            else
            {
                Debug.LogError("GameManager interface not available!");
            }
        }

        private void SubscribeToEvents()
        {
            gameManager.OnGameStateChanged += UpdateGameState;
            gameManager.OnPlayerChanged += HandlePlayerChanged;
            // Subscribe to other events as needed
        }

        private void UnsubscribeFromEvents()
        {
            if (gameManager != null)
            {
                gameManager.OnGameStateChanged -= UpdateGameState;
                gameManager.OnPlayerChanged -= HandlePlayerChanged;
                // Unsubscribe from other events
            }
        }

        private void HandlePlayerChanged(IPlayer player)
        {
            // Update UI elements related to the current player
            if (player != null)
            {
                UpdateGameUI();
            }
        }

        private void UpdateGameState(GameState newState)
        {
            Debug.Log($"UIManager: Game state updated to {newState}");
            if (gameStateText == null) return;

            switch (newState)
            {
                case GameState.WaitingToStart:
                    gameStateText.text = "Press Start to Begin";
                    if (startGameButton != null)
                    {
                        startGameButton.gameObject.SetActive(true);
                    }
                    break;
                case GameState.PlayerTurn:
                    gameStateText.text = gameManager.CurrentPlayer.IsHuman ? "Your Turn" : $"{gameManager.CurrentPlayer.Name}'s Turn";
                    if (startGameButton != null)
                    {
                        startGameButton.gameObject.SetActive(false);
                    }
                    break;
                case GameState.GameOver:
                    ShowGameOverScreen();
                    break;
            }

            UpdateGameUI();
        }

        private void StartNewGame()
        {
            Debug.Log("UIManager: Start button clicked");
            if (gameManager != null)
            {
                Debug.Log("UIManager: Starting new game through GameManager");
                gameManager.StartNewGame();
                if (gameOverPanel != null)
                {
                    gameOverPanel.SetActive(false);
                }
            }
            else
            {
                Debug.LogError("UIManager: GameManager interface not available!");
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
    }
}