using UnityEngine;
using CardGame;
using CardGame.Core;
using CardGame.Rules;
using CardGame.UI;
using CardGame.Utils;

/// <summary>
/// Handles the initial setup of the game environment.
/// This class is created by GameBootstrap during game initialization.
/// </summary>
public class GameSetup : MonoBehaviour
{
    private static GameSetup instance;
    public static GameSetup Instance => instance;

    private IGameManager gameManager;
    private IDeck deck;
    private ICardRules cardRules;
    private IUIManager uiManager;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGameSystems();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeGameSystems()
    {
        Debug.Log("GameSetup: Initializing game systems");

        // Create and initialize core systems in the correct order
        cardRules = CreateCardRules();
        deck = CreateDeck();
        gameManager = CreateGameManager();
        uiManager = CreateUIManager();

        // Initialize GameManager with dependencies
        if (gameManager is GameManager gm)
        {
            gm.Initialize(deck, cardRules, uiManager);
        }

        // Create CardGame object with CardSimulation if needed
        if (Object.FindAnyObjectByType<CardSimulation>() == null)
        {
            GameObject cardGame = new GameObject("CardGame");
            cardGame.AddComponent<CardSimulation>();
        }

        // Initialize error handling system
        InitializeErrorHandling();
    }

    private GameManager CreateGameManager()
    {
        var existingManager = Object.FindAnyObjectByType<GameManager>();
        if (existingManager != null)
        {
            return existingManager;
        }

        GameObject gameManagerObj = new GameObject("GameManager");
        DontDestroyOnLoad(gameManagerObj);
        return gameManagerObj.AddComponent<GameManager>();
    }

    private ICardRules CreateCardRules()
    {
        // First check for existing component
        var existingRules = Object.FindAnyObjectByType<CardRulesComponent>();
        if (existingRules != null)
        {
            return existingRules.Rules;
        }

        // Create new component
        GameObject rulesObj = new GameObject("CardRules");
        DontDestroyOnLoad(rulesObj);
        var rulesComponent = rulesObj.AddComponent<CardRulesComponent>();
        rulesComponent.Initialize();

        // Return the interface
        return CardRules.Instance;
    }

    private IUIManager CreateUIManager()
    {
        GameObject uiManagerObject = new GameObject("UIManager");
        UIManager uiManager = uiManagerObject.AddComponent<UIManager>();
        uiManager.Initialize(gameManager);
        return uiManager;
    }

    private IDeck CreateDeck()
    {
        var existingDeck = Object.FindAnyObjectByType<Deck>();
        if (existingDeck != null)
        {
            return existingDeck;
        }

        GameObject deckObj = new GameObject("Deck");
        DontDestroyOnLoad(deckObj);
        var deck = deckObj.AddComponent<Deck>();
        deck.Initialize();
        return deck;
    }

    private void InitializeErrorHandling()
    {
        // ErrorHandler will create its own GameObject if needed
        var errorHandler = ErrorHandler.Instance;
        if (errorHandler == null)
        {
            Debug.LogError("Failed to initialize ErrorHandler");
        }
    }

    public void ConfigureGame(int playerCount, bool isSimulation = false)
    {
        Debug.Log($"GameSetup: Configuring game with {playerCount} players (Simulation: {isSimulation})");

        if (gameManager == null)
        {
            Debug.LogError("GameSetup: GameManager not initialized");
            return;
        }

        if (isSimulation)
        {
            gameManager.InitializeSimulation(playerCount, 0.5f, true);
        }
        else
        {
            gameManager.StartNewGame();
        }
    }
}

// Add this MonoBehaviour wrapper for CardRules
namespace CardGame.Rules
{
    public class CardRulesComponent : MonoBehaviour
    {
        public ICardRules Rules => CardRules.Instance;

        public void Initialize()
        {
            // No need to create a new instance since we're using the singleton
            Debug.Log("CardRules component initialized");
        }
    }
} 