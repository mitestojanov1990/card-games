using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using CardGame.Players;
using CardGame.Rules;
using CardGame.UI;
using CardGame.Utils;
using CardGame.Exceptions;
using System;
using static CardGame.Core.GameManager;

namespace CardGame.Core
{
    public interface IGameManager
    {
        GameState CurrentState { get; }
        IPlayer CurrentPlayer { get; }
        int PlayerCount { get; }
        bool IsSequentialPlayActive { get; }
        int TurnCount { get; }
        int DeckCount { get; }
        int CurrentDrawAmount { get; }
        string DeclaredSuit { get; }

        void InitializeSimulation(int playerCount, float delay, bool logging);
        void StartNewGame();
        void PlayCard(ICard card, IPlayer player);
        ICard DrawCard();
        void NextTurn();
        void CallMacau(IPlayer player);
        void CallStopMacau(IPlayer caller, IPlayer target);
        void DeclareSuit(string suit);

        event Action<GameState> OnGameStateChanged;
        event Action<IPlayer> OnPlayerChanged;
        event Action<ICard, IPlayer> OnCardPlayed;
        event Action<ICard, IPlayer> OnCardDrawn;
        event Action<string> OnSuitDeclared;
        event Action<int> OnDrawCardsEffect;
        event Action OnSkipTurn;
        event Action<bool> OnSequentialPlayChanged;
        event Action<string> OnMacauCalled;
        event Action<string> OnStopMacauCalled;
    }

    public class GameManager : MonoBehaviour, IGameManager
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
        private bool isDrawingMultipleCards = false;
        private int remainingCardsToDraw = 0;

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
            ValidateSimulationParams(playerCount, delay);

            isSimulation = true;
            simulationDelay = delay;
            detailedLogging = logging;
            PlayerCount = playerCount;
            StartNewGame();
        }

        private void ValidateSimulationParams(int playerCount, float delay)
        {
            if (playerCount < 2)
                throw new GameValidationException("Must have at least 2 players");
            if (playerCount > 6)
                throw new GameValidationException("Maximum 6 players allowed");
            if (delay < 0)
                throw new GameValidationException("Delay cannot be negative");
        }

        public void StartNewGame()
        {
            GameLogger.Instance.LogGameEvent("GameStarted", new Dictionary<string, object>
            {
                { "PlayerCount", PlayerCount },
                { "IsSimulation", isSimulation }
            });

            ValidateGameState(GameState.PlayerTurn);
            ValidateNewGame();

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

        private void ValidateGameState(GameState newState)
        {
            switch (CurrentState)
            {
                case GameState.WaitingToStart:
                    if (newState != GameState.PlayerTurn)
                        throw new GameValidationException("Game must start with player turn");
                    break;
                case GameState.PlayerTurn:
                    if (newState != GameState.GameOver && newState != GameState.PlayerTurn)
                        throw new GameValidationException("Invalid state transition from PlayerTurn");
                    break;
                case GameState.GameOver:
                    if (newState != GameState.WaitingToStart)
                        throw new GameValidationException("Can only restart game from GameOver state");
                    break;
            }
        }

        private void ValidateNewGame()
        {
            if (players == null)
                throw new GameValidationException("Players list not initialized");
            if (PlayerCount <= 0)
                throw new GameValidationException("Invalid player count");
            if (INITIAL_HAND_SIZE <= 0)
                throw new GameValidationException("Invalid initial hand size");
        }

        private void ValidateNextTurn()
        {
            if (CurrentState != GameState.PlayerTurn)
                throw new GameValidationException("Cannot change turns outside of PlayerTurn state");
            if (players.Count == 0)
                throw new GameValidationException("No players in game");
            if (currentPlayerIndex < 0 || currentPlayerIndex >= players.Count)
                throw new GameValidationException("Invalid player index");
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

        public void HandleCardDraw()
        {
            try
            {
                ValidateCardDraw();

                if (currentDrawAmount > 0)
                {
                    StartDrawingMultipleCards();
                }
                else
                {
                    DrawSingleCard();
                }
            }
            catch (GameValidationException ex)
            {
                ErrorHandler.HandleException(ex, "Invalid card draw");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Error drawing card");
                ResetGameState();
            }
        }

        private void ValidateCardDraw()
        {
            if (CurrentState != GameState.PlayerTurn)
                throw new GameValidationException("Cannot draw cards outside of player turn");
            if (deck.IsEmpty())
                throw new GameValidationException("Cannot draw from empty deck");
        }

        private void DrawSingleCard()
        {
            Card drawnCard = DrawCard();
            if (drawnCard != null)
            {
                OnCardDrawn?.Invoke(drawnCard, CurrentPlayer);
                CurrentPlayer.AddCard(drawnCard);

                // Check if drawn card can be played
                if (IsValidPlay(drawnCard))
                {
                    // Player can still play this card
                    return;
                }
                NextTurn();
            }
        }

        private void StartDrawingMultipleCards()
        {
            isDrawingMultipleCards = true;
            remainingCardsToDraw = currentDrawAmount;
            currentDrawAmount = 0; // Reset so the effect is cleared
            OnDrawCardsEffect?.Invoke(0); // Clear UI notification
            DrawMultipleCards();
        }

        private void DrawMultipleCards()
        {
            while (remainingCardsToDraw > 0)
            {
                Card drawnCard = DrawCard();
                if (drawnCard != null)
                {
                    OnCardDrawn?.Invoke(drawnCard, CurrentPlayer);
                    CurrentPlayer.AddCard(drawnCard);
                    remainingCardsToDraw--;
                }
                else
                {
                    break; // No more cards in deck
                }
            }

            // After drawing all required cards, check if player can play
            Card playableCard = CurrentPlayer.Hand.FirstOrDefault(IsValidPlay);
            if (playableCard == null)
            {
                // Draw one more card to check if it can be played
                Card extraCard = DrawCard();
                if (extraCard != null)
                {
                    OnCardDrawn?.Invoke(extraCard, CurrentPlayer);
                    CurrentPlayer.AddCard(extraCard);

                    if (!IsValidPlay(extraCard))
                    {
                        // If can't play the extra card, end turn
                        isDrawingMultipleCards = false;
                        NextTurn();
                    }
                }
                else
                {
                    // No more cards to draw, end turn
                    isDrawingMultipleCards = false;
                    NextTurn();
                }
            }
            // If player has a playable card, they can play it (turn continues)
        }

        public void PlayCard(Card card, Player player)
        {
            try
            {
                ValidatePlayCard(card, player);

                var parameters = new Dictionary<string, object>
                {
                    { "Player", player.Name },
                    { "Card", $"{card.Rank}{card.Suit}" },
                    { "TurnNumber", turnCount }
                };
                GameLogger.Instance.LogGameEvent("CardPlayed", parameters);

                // Handle special effects
                var effect = CardRules.GetCardEffect(card);
                HandleSpecialEffect(effect, card, player);

                DiscardCard(card);
                OnCardPlayed?.Invoke(card, player);

                CheckWinCondition(player);
            }
            catch (GameValidationException ex)
            {
                ErrorHandler.HandleException(ex, "Invalid card play");
                GameLogger.Instance.Log(ex.Message, GameLogger.LogType.Warning, "Card Validation");
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Error playing card");
                GameLogger.Instance.Log(ex.Message, GameLogger.LogType.Error, "Card Play Error");
                ResetGameState();
            }
        }

        private void HandleSpecialEffect(SpecialEffect effect, Card card, Player player)
        {
            try
            {
                GameLogger.Instance.LogGameEvent("SpecialEffect", new Dictionary<string, object>
                {
                    { "Effect", effect },
                    { "Player", player.Name },
                    { "Card", $"{card.Rank}{card.Suit}" }
                });

                switch (effect)
                {
                    case SpecialEffect.DrawTwo:
                    case SpecialEffect.DrawThree:
                        currentDrawAmount += CardRules.GetDrawAmount(card);
                        OnDrawCardsEffect?.Invoke(currentDrawAmount);
                        break;

                    case SpecialEffect.PopCup:
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

                    case SpecialEffect.SkipTurn:
                        if (players.Count == 2)
                        {
                            // In 2-player game, current player gets another turn
                            currentPlayerIndex = (currentPlayerIndex - 1 + players.Count) % players.Count;
                        }
                        break;

                    case SpecialEffect.ChangeSuit:
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

                    case SpecialEffect.Sequential:
                        isSequentialPlayActive = true;
                        OnSequentialPlayChanged?.Invoke(true);
                        break;
                }
            }
            catch (Exception ex)
            {
                GameLogger.Instance.Log(ex.Message, GameLogger.LogType.Error, "Special Effect Error");
                throw;
            }
        }

        private void ValidatePlayCard(Card card, Player player)
        {
            if (card == null)
                throw new GameValidationException("Cannot play null card");
            if (player == null)
                throw new GameValidationException("Player cannot be null");
            if (CurrentState != GameState.PlayerTurn)
                throw new GameValidationException("Cannot play card outside of player turn");
            if (player != CurrentPlayer)
                throw new GameValidationException("Not this player's turn");
            if (!player.Hand.Contains(card))
                throw new GameValidationException("Player does not have this card");
            if (!IsValidPlay(card))
                throw new GameValidationException("Invalid card play");
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
                    Debug.Log($"{player.Name} wins the game!");
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
            ValidateSuitDeclaration(suit);

            declaredSuit = suit;
            OnSuitDeclared?.Invoke(suit);
        }

        private void ValidateSuitDeclaration(string suit)
        {
            if (string.IsNullOrEmpty(suit))
                throw new GameValidationException("Declared suit cannot be null or empty");
            if (!Card.ValidSuits.Contains(suit))
                throw new GameValidationException($"Invalid suit declaration: {suit}");
        }

        public void NextTurn()
        {
            ValidateNextTurn();
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
            Vector2 center = new Vector2(Screen.width / 2, Screen.height / 2);
            suitSelector.Show(center);
        }

        private void ResetGameState()
        {
            // Reset any ongoing actions
            isDrawingMultipleCards = false;
            remainingCardsToDraw = 0;
            currentDrawAmount = 0;

            // Clear UI notifications
            OnDrawCardsEffect?.Invoke(0);

            // Log the reset
            ErrorHandler.LogWarning("Game state has been reset due to an error");
        }

        private void CheckWinCondition(Player player)
        {
            if (player.Hand.Count == 0)
            {
                GameLogger.Instance.LogGameEvent("GameWon", new Dictionary<string, object>
                {
                    { "Winner", player.Name },
                    { "TurnCount", turnCount },
                    { "RemainingCards", deck.RemainingCards }
                });

                CurrentState = GameState.GameOver;
                OnGameStateChanged?.Invoke(CurrentState);
            }
        }

        public void Initialize(IDeck deckInstance, ICardRules rulesInstance, IUIManager uiManagerInstance)
        {
            // Store references to injected dependencies
            this.deck = deckInstance as Deck;

            // Additional initialization logic as needed
            Debug.Log("GameManager initialized with dependencies");

            // You might want to set up event handlers or other initialization here

            // Set initial state
            CurrentState = GameState.WaitingToStart;
            OnGameStateChanged?.Invoke(CurrentState);
        }
    }
}