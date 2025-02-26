using UnityEngine;
using System.Collections.Generic;
using System.Collections;

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
    }

    public void StartNewGame()
    {
        Debug.Log("Starting new game");
        deck = new Deck();
        
        // Create players (2-10 players as per rules)
        players.Clear();
        players.Add(new Player("You", true));
        for (int i = 1; i < PlayerCount; i++)
        {
            players.Add(new Player($"CPU {i}", false));
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

        // Handle Macau calls
        if (player.Hand.Count == 2 && !hasCalledMacau)
        {
            canCallStopMacau = true;
        }

        DiscardCard(card);
        OnCardPlayed?.Invoke(card, player);
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

        if (deck.IsEmpty())
        {
            CurrentState = GameState.GameOver;
            Debug.Log("Game Over - Deck is empty!");
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
        Debug.Log($"{CurrentPlayer.Name}'s turn");
        
        // Add delay for readability
        yield return new WaitForSeconds(0.5f);
        
        // Check for Macau
        CurrentPlayer.CheckMacau();

        // Try to play a card
        Card cardToPlay = CurrentPlayer.GetBestPlay(topDiscard, declaredSuit, isSequentialPlayActive);
        
        if (cardToPlay != null)
        {
            CurrentPlayer.RemoveCard(cardToPlay);
            OnCardPlayed?.Invoke(cardToPlay, CurrentPlayer);
            
            // Wait for animation
            yield return new WaitForSeconds(0.6f);
            
            PlayCard(cardToPlay, CurrentPlayer);
            Debug.Log($"{CurrentPlayer.Name} played {cardToPlay.Rank}{cardToPlay.Suit}");
        }
        else
        {
            // Draw a card if can't play
            Card drawnCard = DrawCard();
            if (drawnCard != null)
            {
                OnCardDrawn?.Invoke(drawnCard, CurrentPlayer);
                
                // Wait for animation
                yield return new WaitForSeconds(0.6f);
                
                CurrentPlayer.AddCard(drawnCard);
                Debug.Log($"{CurrentPlayer.Name} drew a card");
            }
        }

        yield return new WaitForSeconds(0.5f);

        CheckGameEnd();
        if (CurrentState != GameState.GameOver)
        {
            NextTurn();
        }
    }

    private void ShowSuitSelector()
    {
        Vector2 center = new Vector2(Screen.width/2, Screen.height/2);
        suitSelector.Show(center);
    }
} 