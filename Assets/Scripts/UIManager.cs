using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text gameStateText;
    public Text scoreText;
    public Button startGameButton;
    public GameObject gameOverPanel;

    private void Start()
    {
        // Set initial state
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

        // Subscribe to GameManager events
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameStateChanged += UpdateGameState;
        }
    }

    private void UpdateGameState(GameManager.GameState newState)
    {
        if (gameStateText == null) return;

        switch (newState)
        {
            case GameManager.GameState.WaitingToStart:
                gameStateText.text = "Press Start to Begin";
                if (startGameButton != null)
                {
                    startGameButton.gameObject.SetActive(true);
                }
                break;
            case GameManager.GameState.PlayerTurn:
                gameStateText.text = "Your Turn";
                if (startGameButton != null)
                {
                    startGameButton.gameObject.SetActive(false);
                }
                break;
            case GameManager.GameState.GameOver:
                gameStateText.text = "Game Over!";
                if (gameOverPanel != null)
                {
                    gameOverPanel.SetActive(true);
                }
                break;
        }
    }

    private void StartNewGame()
    {
        Debug.Log("UIManager: Start button clicked");  // Add debug
        if (GameManager.Instance != null)
        {
            Debug.Log("UIManager: Starting new game through GameManager");  // Add debug
            GameManager.Instance.StartNewGame();
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(false);
            }
        }
        else
        {
            Debug.LogError("UIManager: GameManager.Instance is null!");  // Add error debug
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