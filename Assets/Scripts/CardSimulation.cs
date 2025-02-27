using UnityEngine;
using UnityEngine.UI;
using CardGame.Core;
using CardGame.UI;
using CardGame.DI;

namespace CardGame
{
    public class CardSimulation : MonoBehaviour
    {
        private IGameManager gameManager;
        private IUIManager uiManager;
        private SimulationUI simulationUI;
        private bool isRunning = false;
        private float simulationDelay = 0.5f;
        private bool detailedLogging = true;

        private void Start()
        {
            // Create container if it doesn't exist
            if (GameContainer.Instance == null)
            {
                var containerObj = new GameObject("GameContainer");
                containerObj.AddComponent<GameContainer>();
            }

            // Get dependencies
            gameManager = GameContainer.Instance.GameManager;
            uiManager = GameContainer.Instance.UIManager;

            // Setup UI
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            GameObject uiObj = new GameObject("SimulationUI");
            uiObj.transform.SetParent(canvas.transform, false);
            simulationUI = uiObj.AddComponent<SimulationUI>();
        }

        public void StartSimulation(int playerCount = 3)
        {
            if (!isRunning)
            {
                isRunning = true;
                if (gameManager != null)
                {
                    gameManager.InitializeSimulation(playerCount, simulationDelay, detailedLogging);
                }
            }
        }

        public void StopSimulation()
        {
            isRunning = false;
        }

        public void SetSimulationSpeed(float delay)
        {
            simulationDelay = Mathf.Clamp(delay, 0.1f, 2f);
        }

        public void ToggleDetailedLogging()
        {
            detailedLogging = !detailedLogging;
        }

        private void OnDestroy()
        {
            StopSimulation();
        }
    }
} 