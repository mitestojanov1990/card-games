using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    public enum GameState
    {
        WaitingToStart,
        PlayerTurn,
        GameOver
    }

    public GameState CurrentState { get; private set; }
    private Deck deck;
    private Card topDiscard;
    private List<Card> playerHand = new List<Card>();
    public System.Action<Card> OnCardDiscarded;
    public System.Action<GameState> OnGameStateChanged;
    public int PlayerHandSize { get; private set; } = 7;
    public bool RequireInitialDiscard { get; private set; } = true;

    private int currentDrawAmount = 0;
    private bool isSequentialPlayActive = false;
    private string declaredSuit = null;
    private bool hasCalledMacau = false;
    private bool canCallStopMacau = false;
    private const int STOP_MACAU_PENALTY = 3;
    private const int INITIAL_HAND_SIZE = 6; // Rule: 6 cards are dealt
    
    public System.Action<string> OnSuitDeclared;
    public System.Action<int> OnDrawCardsEffect;
    public System.Action OnSkipTurn;
    public System.Action<bool> OnSequentialPlayChanged;
    public System.Action<string> OnMacauCalled;
    public System.Action<string> OnStopMacauCalled;

    private SuitSelector suitSelector;

    private List<Player> players = new List<Player>();
    private int currentPlayerIndex = 0;
    public Player CurrentPlayer => players[currentPlayerIndex];
    public System.Action<Player> OnPlayerChanged;
    public int PlayerCount { get; private set; } = 3; // Default to 2 CPU + 1 human

    // Add new events
    public System.Action<Card, Player> OnCardPlayed;
    public System.Action<Card, Player> OnCardDrawn;

    // Add public accessor for players
    public IReadOnlyList<Player> Players => players;
    
    // Add method to get player index
    public int GetPlayerIndex(Player player)
    {
        return players.IndexOf(player);
    }

    private int turnCount = 1;

    // Add public properties
    public bool IsSequentialPlayActive => isSequentialPlayActive;
    public int TurnCount => turnCount;
    public int DeckCount => deck?.RemainingCards ?? 0;
    public int CurrentDrawAmount => currentDrawAmount;
    public string DeclaredSuit => declaredSuit;

    // Add new fields
    private bool isSimulation = false;
    private float simulationDelay = 0.5f;
    private bool detailedLogging = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Set initial state to WaitingToStart
        CurrentState = GameState.WaitingToStart;
        OnGameStateChanged?.Invoke(CurrentState);
        
        // Create suit selector
        GameObject suitSelectorObj = new GameObject("SuitSelector", typeof(RectTransform));
        suitSelectorObj.transform.SetParent(FindFirstObjectByType<Canvas>().transform, false);
        suitSelector = suitSelectorObj.AddComponent<SuitSelector>();
        suitSelector.gameObject.SetActive(false);

        // Add simulation UI
        GameObject simUIObj = new GameObject("SimulationUI");
        simUIObj.transform.SetParent(FindFirstObjectByType<Canvas>().transform, false);
        simUIObj.AddComponent<SimulationUI>();
    }

    public void InitializeSimulation(int playerCount, float delay, bool logging)
    {
        isSimulation = true;
        simulationDelay = delay;
        detailedLogging = logging;
        PlayerCount = playerCount;
        StartNewGame();
    }

    public void StartNewGame()
    {
        Debug.Log($"Starting new game (Simulation: {isSimulation})");
        deck = new Deck();
        
        // Create players
        players.Clear();
        if (isSimulation)
        {
            for (int i = 0; i < PlayerCount; i++)
            {
                players.Add(new Player($"CPU {i + 1}", false));
            }
        }
        else
        {
            players.Add(new Player("You", true));
            for (int i = 1; i < PlayerCount; i++)
            {
                players.Add(new Player($"CPU {i}", false));
            }
        }

        // Deal 6 cards to each player
        foreach (Player player in players)
        {
            for (int i = 0; i < INITIAL_HAND_SIZE; i++)
            {
                Card card = DrawCard();
                if (card != null)
                {
                    player.AddCard(card);
                }
            }
        }

        // Turn up first card
        topDiscard = DrawCard();
        OnCardDiscarded?.Invoke(topDiscard);

        // Initialize game state
        currentPlayerIndex = 0;
        CurrentState = GameState.PlayerTurn;
        isSequentialPlayActive = false;
        currentDrawAmount = 0;
        declaredSuit = null;
        hasCalledMacau = false;
        canCallStopMacau = false;

        OnGameStateChanged?.Invoke(CurrentState);
        OnPlayerChanged?.Invoke(CurrentPlayer);
    }

    public Card DrawCard()
    {
        if (deck == null)
        {
            Debug.LogError("Deck is null!");
            return null;
        }
        Card card = deck.DrawCard();
        Debug.Log($"Drew card: {(card != null ? card.Rank + card.Suit : "null")}");  // Add debug
        return card;
    }

    public void DiscardCard(Card card)
    {
        topDiscard = card;
        OnCardDiscarded?.Invoke(card);
    }

    public bool IsValidPlay(Card card)
    {
        if (topDiscard == null) return true;

        // Check if countering draw cards effect
        if (currentDrawAmount > 0)
        {
            return CardRules.IsDrawCardCounter(card, topDiscard);
        }

        return CardRules.CanPlayOnTop(card, topDiscard, isSequentialPlayActive, declaredSuit);
    }

    public void PlayCard(Card card, Player player)
    {
        // Handle special effects
        var effect = CardRules.GetCardEffect(card);
        switch (effect)
        {
            case CardRules.SpecialEffect.DrawTwo:
            case CardRules.SpecialEffect.DrawThree:
                currentDrawAmount += CardRules.GetDrawAmount(card);
                OnDrawCardsEffect?.Invoke(currentDrawAmount);
                break;

            case CardRules.SpecialEffect.PopCup:
                if (!CardRules.IsPopCupCounter(card, topDiscard))
                {
                    currentDrawAmount = 5;
                    OnDrawCardsEffect?.Invoke(currentDrawAmount);
                }
                else
                {
                    // Queen counters King - reverse the effect
                    Player previousPlayer = players[(currentPlayerIndex - 1 + players.Count) % players.Count];
                    ForcePlayerDraw(previousPlayer, 5);
                }
                break;

            case CardRules.SpecialEffect.SkipTurn:
                if (players.Count == 2)
                {
                    // In 2-player game, current player gets another turn
                    currentPlayerIndex = (currentPlayerIndex - 1 + players.Count) % players.Count;
                }
                break;

            case CardRules.SpecialEffect.ChangeSuit:
                if (player.IsHuman)
                {
                    ShowSuitSelector();
                }
                else
                {
                    // CPU chooses most common suit in hand
                    declaredSuit = player.GetMostCommonSuit();
                    OnSuitDeclared?.Invoke(declaredSuit);
                }
                break;

            case CardRules.SpecialEffect.Sequential:
                isSequentialPlayActive = true;
                OnSequentialPlayChanged?.Invoke(true);
                break;
        }

        DiscardCard(card);
        OnCardPlayed?.Invoke(card, player);

        // Check if player has won
        if (player.Hand.Count == 0)
        {
            Debug.Log($"{player.Name} wins the game!");
            CurrentState = GameState.GameOver;
            OnGameStateChanged?.Invoke(CurrentState);
            return;
        }

        // Handle Macau calls
        if (player.Hand.Count == 2 && !hasCalledMacau)
        {
            canCallStopMacau = true;
        }
    }

    public void CallMacau(Player player)
    {
        if (player.Hand.Count == 1 && !hasCalledMacau)
        {
            hasCalledMacau = true;
            OnMacauCalled?.Invoke(player.Name);
        }
    }

    public void CallStopMacau(Player caller, Player target)
    {
        if (canCallStopMacau && target.Hand.Count == 1 && !hasCalledMacau)
        {
            OnStopMacauCalled?.Invoke($"{caller.Name} caught {target.Name} not calling Macau!");
            ForcePlayerDraw(target, STOP_MACAU_PENALTY);
        }
    }

    private void ForcePlayerDraw(Player player, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Card card = DrawCard();
            if (card != null)
            {
                player.AddCard(card);
            }
        }
    }

    public void CheckGameEnd()
    {
        // Check if any player has won
        foreach (Player player in players)
        {
            if (player.Hand.Count == 0)
            {
                CurrentState = GameState.GameOver;
                Debug.Log($"{player.Name} wins!");
                OnGameStateChanged?.Invoke(CurrentState);
                return;
            }
        }

        // Check if deck is empty and no one can play
        if (deck.IsEmpty() && players.All(p => !p.Hand.Any(c => IsValidPlay(c))))
        {
            CurrentState = GameState.GameOver;
            // Find player with fewest cards
            var winner = players.OrderBy(p => p.Hand.Count).First();
            Debug.Log($"Game Over - Deck empty! {winner.Name} wins with fewest cards ({winner.Hand.Count})!");
            OnGameStateChanged?.Invoke(CurrentState);
        }
    }

    public void AddCardToHand(Card card)
    {
        if (CurrentPlayer.IsHuman)
        {
            CurrentPlayer.AddCard(card);
        }
    }

    public void RemoveCardFromHand(Card card)
    {
        if (CurrentPlayer.IsHuman)
        {
            CurrentPlayer.RemoveCard(card);
        }
    }

    public void DeclareSuit(string suit)
    {
        declaredSuit = suit;
        OnSuitDeclared?.Invoke(suit);
        Debug.Log($"Declared suit: {suit}");
    }

    public void NextTurn()
    {
        currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
        turnCount++;
        OnPlayerChanged?.Invoke(CurrentPlayer);

        if (!CurrentPlayer.IsHuman)
        {
            PlayCPUTurn();
        }
    }

    private void PlayCPUTurn()
    {
        StartCoroutine(PlayCPUTurnAnimation());
    }

    private IEnumerator PlayCPUTurnAnimation()
    {
        if (detailedLogging)
        {
            Debug.Log($"\n=== {CurrentPlayer.Name}'s turn ===");
            Debug.Log($"Hand: {string.Join(", ", CurrentPlayer.Hand.Select(c => c.Rank + c.Suit))}");
            Debug.Log($"Top card: {topDiscard.Rank}{topDiscard.Suit}");
            if (declaredSuit != null) Debug.Log($"Declared suit: {declaredSuit}");
            if (currentDrawAmount > 0) Debug.Log($"Draw amount: {currentDrawAmount}");
            if (isSequentialPlayActive) Debug.Log("Sequential play active");
        }

        yield return new WaitForSeconds(simulationDelay);
        
        // Check for Macau
        CurrentPlayer.CheckMacau();

        // Try to play a card
        Card cardToPlay = CurrentPlayer.GetBestPlay(topDiscard, declaredSuit, isSequentialPlayActive);
        
        if (cardToPlay != null)
        {
            if (detailedLogging)
            {
                Debug.Log($"{CurrentPlayer.Name} played {cardToPlay.Rank}{cardToPlay.Suit}");
            }

            CurrentPlayer.RemoveCard(cardToPlay);
            OnCardPlayed?.Invoke(cardToPlay, CurrentPlayer);
            
            yield return new WaitForSeconds(simulationDelay);
            
            PlayCard(cardToPlay, CurrentPlayer);

            // Don't continue turn if game is over
            if (CurrentState == GameState.GameOver)
            {
                yield break;
            }
        }
        else
        {
            Card drawnCard = DrawCard();
            if (drawnCard != null)
            {
                if (detailedLogging)
                {
                    Debug.Log($"{CurrentPlayer.Name} drew {drawnCard.Rank}{drawnCard.Suit}");
                }

                OnCardDrawn?.Invoke(drawnCard, CurrentPlayer);
                yield return new WaitForSeconds(simulationDelay);
                CurrentPlayer.AddCard(drawnCard);
            }
        }

        if (detailedLogging)
        {
            Debug.Log($"Hand size: {CurrentPlayer.Hand.Count}");
        }

        yield return new WaitForSeconds(simulationDelay);

        CheckGameEnd();
        if (CurrentState != GameState.GameOver)
        {
            NextTurn();
        }
        else if (detailedLogging)
        {
            Debug.Log($"\n=== Game Over ===\nWinner: {CurrentPlayer.Name}");
            foreach (var player in players)
            {
                Debug.Log($"{player.Name}'s final hand: {string.Join(", ", player.Hand.Select(c => c.Rank + c.Suit))}");
            }
        }
    }

    private void ShowSuitSelector()
    {
        Vector2 center = new Vector2(Screen.width/2, Screen.height/2);
        suitSelector.Show(center);
    }
} 