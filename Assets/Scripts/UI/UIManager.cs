using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using CardGame.Core;
using CardGame.Players;
using CardGame.Rules;
using CardGame.UI.Interfaces;

namespace CardGame.UI
{
    public class UIManager : MonoBehaviour, IUIManager
    {
        private static UIManager instance;
        public static UIManager Instance => instance;

        [Header("UI References")]
        private Canvas mainCanvas;
        private CPUHandVisualizer cpuHandVisualizer;
        private SimulationUI simulationUI;

        [Header("UI Settings")]
        private float uiScale = 1f;
        private bool showDebugInfo = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeUI();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeUI()
        {
            // Setup main canvas
            mainCanvas = FindFirstObjectByType<Canvas>();
            if (mainCanvas == null)
            {
                GameObject canvasObj = new GameObject("MainCanvas");
                mainCanvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // Setup CPU hand visualizer
            GameObject cpuHandObj = new GameObject("CPUHandVisualizer");
            cpuHandObj.transform.SetParent(mainCanvas.transform, false);
            cpuHandVisualizer = cpuHandObj.AddComponent<CPUHandVisualizer>();

            // Setup simulation UI
            GameObject simUIObj = new GameObject("SimulationUI");
            simUIObj.transform.SetParent(mainCanvas.transform, false);
            simulationUI = simUIObj.AddComponent<SimulationUI>();

            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
            else
            {
                Debug.LogError("GameManager not found during UI initialization");
            }
        }

        private void HandleGameStateChanged(GameManager.GameState newState)
        {
            switch (newState)
            {
                case GameManager.GameState.WaitingToStart:
                    ShowStartScreen();
                    break;
                case GameManager.GameState.PlayerTurn:
                    UpdateGameUI();
                    break;
                case GameManager.GameState.GameOver:
                    ShowGameOverScreen();
                    break;
            }
        }

        private void ShowStartScreen()
        {
            // Implementation for start screen
        }

        private void UpdateGameUI()
        {
            // Implementation for game UI updates
        }

        private void ShowGameOverScreen()
        {
            // Implementation for game over screen
        }

        public void SetUIScale(float scale)
        {
            uiScale = Mathf.Clamp(scale, 0.5f, 2f);
            mainCanvas.GetComponent<CanvasScaler>().scaleFactor = uiScale;
        }

        public void ToggleDebugInfo()
        {
            showDebugInfo = !showDebugInfo;
            // Implementation for showing/hiding debug info
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }
        }
    }
} 