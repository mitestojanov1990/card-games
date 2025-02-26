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
    
    public System.Action<string> OnSuitDeclared;
    public System.Action<int> OnDrawCardsEffect;
    public System.Action OnSkipTurn;
    public System.Action<bool> OnSequentialPlayChanged;

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
        Debug.Log("GameManager: Starting new game");
        deck = new Deck();
        
        // Create players
        players.Clear();
        players.Add(new Player("You", true)); // Human player
        for (int i = 1; i < PlayerCount; i++)
        {
            players.Add(new Player($"CPU {i}", false));
        }

        // Deal initial cards
        foreach (Player player in players)
        {
            for (int i = 0; i < PlayerHandSize; i++)
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

        currentPlayerIndex = 0;
        CurrentState = GameState.PlayerTurn;
        OnGameStateChanged?.Invoke(CurrentState);
        OnPlayerChanged?.Invoke(CurrentPlayer);
        
        Debug.Log("Game started, cards dealt");
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
            return CardRules.CanCounterDrawCards(card, topDiscard);
        }

        // Check Pop Cup counter
        if (CardRules.IsPopCupCounter(card, topDiscard))
        {
            return true;
        }

        return CardRules.CanPlayOnTop(card, topDiscard, isSequentialPlayActive);
    }

    public void PlayCard(Card card)
    {
        // Handle special effects
        var effect = CardRules.GetCardEffect(card);
        switch (effect)
        {
            case CardRules.SpecialEffect.DrawCards:
                currentDrawAmount += CardRules.GetDrawAmount(card);
                OnDrawCardsEffect?.Invoke(currentDrawAmount);
                break;

            case CardRules.SpecialEffect.SkipTurn:
                OnSkipTurn?.Invoke();
                break;

            case CardRules.SpecialEffect.ChangeSuit:
                Vector2 center = new Vector2(Screen.width/2, Screen.height/2);
                suitSelector.Show(center);
                break;

            case CardRules.SpecialEffect.Sequential:
                isSequentialPlayActive = true;
                OnSequentialPlayChanged?.Invoke(true);
                break;
        }

        DiscardCard(card);
    }

    public void CallMacau()
    {
        if (playerHand.Count == 1 && !hasCalledMacau)
        {
            hasCalledMacau = true;
            Debug.Log("Player called Macau!");
        }
    }

    public void CallStopMacau()
    {
        if (!hasCalledMacau && playerHand.Count == 1)
        {
            // Force player to draw 3 cards
            for (int i = 0; i < 3; i++)
            {
                Card card = DrawCard();
                if (card != null)
                {
                    playerHand.Add(card);
                }
            }
            Debug.Log("Stop Macau! Player must draw 3 cards");
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
            
            PlayCard(cardToPlay);
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
} 