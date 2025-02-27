using UnityEngine;
using UnityEngine.UI;
using CardGame.Core;

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
            // Implement logic to update the game UI
            // For example, updating scores, player names, etc.
            if (scoreText != null)
            {
                // Assuming you have a way to get the current score
                scoreText.text = $"Score: {GameManager.Instance.CurrentPlayer.Score}"; // Example
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
            // Assuming you want to scale the entire UI
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.scaleFactor = scale;
            }
        }

        public void ToggleDebugInfo()
        {
            // Assuming you have a debug panel or similar
            // This is a placeholder implementation
            Debug.Log("Toggling debug info");
        }

        private void Start()
        {
            ShowStartScreen();

            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += UpdateGameState;
            }
        }

        private void UpdateGameState(GameState newState)  // Changed from GameManager.GameState
        {
            if (gameStateText == null) return;

            switch (newState)
            {
                case GameState.WaitingToStart:  // Updated enum references
                    gameStateText.text = "Press Start to Begin";
                    if (startGameButton != null)
                    {
                        startGameButton.gameObject.SetActive(true);
                    }
                    break;
                case GameState.PlayerTurn:  // Updated enum references
                    gameStateText.text = "Your Turn";
                    if (startGameButton != null)
                    {
                        startGameButton.gameObject.SetActive(false);
                    }
                    break;
                case GameState.GameOver:  // Updated enum references
                    ShowGameOverScreen();
                    break;
            }

            // Call UpdateGameUI to refresh the UI based on the current game state
            UpdateGameUI();
        }

        private void StartNewGame()
        {
            Debug.Log("UIManager: Start button clicked");
            if (GameManager.Instance != null)
            {
                Debug.Log("UIManager: Starting new game through GameManager");
                GameManager.Instance.StartNewGame();
                if (gameOverPanel != null)
                {
                    gameOverPanel.SetActive(false);
                }
            }
            else
            {
                Debug.LogError("UIManager: GameManager.Instance is null!");
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= UpdateGameState;
            }
        }
    }
}