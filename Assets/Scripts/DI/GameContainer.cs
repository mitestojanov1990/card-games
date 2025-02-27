using UnityEngine;
using CardGame.Core;
using CardGame.Rules;
using CardGame.UI;

namespace CardGame.DI
{
    public class GameContainer : MonoBehaviour
    {
        private static GameContainer instance;
        public static GameContainer Instance => instance;

        private IDeck deck;
        private ICardRules cardRules;
        private IUIManager uiManager;
        private IGameManager gameManager;

        public IDeck Deck => deck;
        public ICardRules CardRules => cardRules;
        public IUIManager UIManager => uiManager;
        public IGameManager GameManager => gameManager;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Get references from GameSetup
                var gameSetup = Object.FindAnyObjectByType<GameSetup>();
                if (gameSetup == null)
                {
                    Debug.LogError("GameSetup not found! Make sure GameSetup is initialized first.");
                    return;
                }

                // Get components from the scene
                gameManager = Object.FindAnyObjectByType<GameManager>();
                deck = Object.FindAnyObjectByType<Deck>();
                uiManager = Object.FindAnyObjectByType<UIManager>();
                cardRules = CardGame.Rules.CardRules.Instance;

                if (gameManager == null || deck == null || uiManager == null || cardRules == null)
                {
                    Debug.LogError("Required components not found! Make sure GameSetup has initialized everything.");
                    return;
                }

                Debug.Log("GameContainer initialized with existing components");
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Start the simulation if we have all components
            if (gameManager != null && deck != null && uiManager != null && cardRules != null)
            {
                try
                {
                    gameManager.InitializeSimulation(3, 0.5f, true);
                    Debug.Log("Simulation started through GameContainer");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Failed to start simulation: {ex.Message}");
                }
            }
        }
    }
}