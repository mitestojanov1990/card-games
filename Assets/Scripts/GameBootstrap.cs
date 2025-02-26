using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameBootstrap : MonoBehaviour
{
    public bool simulationMode = false;
    public int simulationPlayers = 3;
    public float cpuPlayDelay = 0.5f;
    public bool detailedLogging = true;
    public bool batchSimulation = false;
    public int batchSize = 100;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnGameStart()
    {
        // Check if GameSetup already exists
        if (FindFirstObjectByType<GameSetup>() == null)
        {
            // Create the initial setup
            GameObject setupObj = new GameObject("GameSetup");
            setupObj.AddComponent<GameSetup>();
            
            // Make sure it persists between scenes
            DontDestroyOnLoad(setupObj);
        }
    }

    private void Start()
    {
        if (simulationMode)
        {
            StartSimulation();
        }
        else
        {
            StartNormalGame();
        }
    }

    private void StartNormalGame()
    {
        // ... existing normal game setup ...
    }

    private void StartSimulation()
    {
        Debug.Log($"Starting {(batchSimulation ? "batch " : "")}simulation mode");
        
        // Create GameManager if needed
        if (FindFirstObjectByType<GameManager>() == null)
        {
            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<GameManager>().InitializeSimulation(simulationPlayers, cpuPlayDelay, detailedLogging);
        }

        // Add statistics tracker
        GameObject statsObj = new GameObject("GameStatistics");
        var statistics = statsObj.AddComponent<GameStatistics>();
        statistics.Initialize();
        
        if (batchSimulation)
        {
            statistics.batchSize = batchSize;
            statistics.StartBatchSimulation();
        }

        // Create minimal UI for monitoring
        CreateSimulationUI();
    }

    private void CreateSimulationUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("SimulationCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Add simulation status panel
        GameObject statusPanel = new GameObject("SimulationStatus");
        statusPanel.transform.SetParent(canvas.transform, false);
        var statusRT = statusPanel.AddComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0, 1);
        statusRT.anchorMax = new Vector2(1, 1);
        statusRT.sizeDelta = new Vector2(0, 100);
        statusRT.anchoredPosition = new Vector2(0, -50);

        var statusText = statusPanel.AddComponent<Text>();
        statusText.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
        statusText.color = Color.white;
        statusText.alignment = TextAnchor.UpperLeft;

        // Subscribe to game events for logging
        GameManager.Instance.OnCardPlayed += (card, player) => 
        {
            if (detailedLogging)
            {
                statusText.text = $"{player.Name} played {card.Rank}{card.Suit}\n" + statusText.text;
                if (statusText.text.Length > 1000) // Trim log if too long
                    statusText.text = statusText.text.Substring(0, 1000);
            }
        };

        GameManager.Instance.OnGameStateChanged += (state) =>
        {
            if (state == GameManager.GameState.GameOver)
            {
                statusText.text = "Simulation complete!\n" + statusText.text;
            }
        };
    }
} 