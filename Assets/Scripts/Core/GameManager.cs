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
using UnityEngine.UI;

namespace CardGame.Core
{
    public interface IGameManager
    {
        GameState CurrentState { get; }
        IPlayer CurrentPlayer { get; }
        IReadOnlyList<IPlayer> Players { get; }
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
        public GameState CurrentState { get; private set; }
        private Deck deck;
        private Card topDiscard;
        private List<Card> playerHand = new List<Card>();
        public System.Action<Card> OnCardDiscarded;
        public int PlayerHandSize { get; private set; } = 7;
        public bool RequireInitialDiscard { get; private set; } = true;
        public int PlayerCount { get; private set; }

        private int currentDrawAmount = 0;
        private bool isSequentialPlayActive = false;
        private string declaredSuit = null;
        private bool hasCalledMacau = false;
        private bool canCallStopMacau = false;
        private const int STOP_MACAU_PENALTY = 3;
        private const int INITIAL_HAND_SIZE = 6; // Rule: 6 cards are dealt

        private SuitSelector suitSelector;

        private List<Player> players = new List<Player>();
        private int currentPlayerIndex = 0;
        public IPlayer CurrentPlayer => players[currentPlayerIndex];

        // Add new events
        public System.Action<Card, Player> OnCardPlayed;
        public System.Action<Card, Player> OnCardDrawn;

        // Add public accessor for players
        public IReadOnlyList<IPlayer> Players => players.Cast<IPlayer>().ToList();

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

        // Add CardRules instance field
        private ICardRules cardRules;

        // Remove public event declarations that duplicate interface events
        private event Action<GameState> OnGameStateChangedInternal;
        private event Action<IPlayer> OnPlayerChangedInternal;
        private event Action<ICard, IPlayer> OnCardPlayedInternal;
        private event Action<ICard, IPlayer> OnCardDrawnInternal;
        private event Action<string> OnSuitDeclaredInternal;
        private event Action<int> OnDrawCardsEffectInternal;
        private event Action OnSkipTurnInternal;
        private event Action<bool> OnSequentialPlayChangedInternal;
        private event Action<string> OnMacauCalledInternal;
        private event Action<string> OnStopMacauCalledInternal;

        // Implement interface events
        event Action<GameState> IGameManager.OnGameStateChanged
        {
            add => OnGameStateChangedInternal += value;
            remove => OnGameStateChangedInternal -= value;
        }

        event Action<IPlayer> IGameManager.OnPlayerChanged
        {
            add => OnPlayerChangedInternal += value;
            remove => OnPlayerChangedInternal -= value;
        }

        event Action<ICard, IPlayer> IGameManager.OnCardPlayed
        {
            add => OnCardPlayedInternal += value;
            remove => OnCardPlayedInternal -= value;
        }

        event Action<ICard, IPlayer> IGameManager.OnCardDrawn
        {
            add => OnCardDrawnInternal += value;
            remove => OnCardDrawnInternal -= value;
        }

        event Action<string> IGameManager.OnSuitDeclared
        {
            add => OnSuitDeclaredInternal += value;
            remove => OnSuitDeclaredInternal -= value;
        }

        event Action<int> IGameManager.OnDrawCardsEffect
        {
            add => OnDrawCardsEffectInternal += value;
            remove => OnDrawCardsEffectInternal -= value;
        }

        event Action IGameManager.OnSkipTurn
        {
            add => OnSkipTurnInternal += value;
            remove => OnSkipTurnInternal -= value;
        }

        event Action<bool> IGameManager.OnSequentialPlayChanged
        {
            add => OnSequentialPlayChangedInternal += value;
            remove => OnSequentialPlayChangedInternal -= value;
        }

        event Action<string> IGameManager.OnMacauCalled
        {
            add => OnMacauCalledInternal += value;
            remove => OnMacauCalledInternal -= value;
        }

        event Action<string> IGameManager.OnStopMacauCalled
        {
            add => OnStopMacauCalledInternal += value;
            remove => OnStopMacauCalledInternal -= value;
        }

        // Update event invocations throughout the code
        private void RaiseGameStateChanged(GameState state)
        {
            OnGameStateChangedInternal?.Invoke(state);
        }

        private void RaisePlayerChanged(IPlayer player)
        {
            OnPlayerChangedInternal?.Invoke(player);
        }

        private void RaiseCardPlayed(ICard card, IPlayer player)
        {
            OnCardPlayedInternal?.Invoke(card, player);
        }

        private void RaiseCardDrawn(ICard card, IPlayer player)
        {
            OnCardDrawnInternal?.Invoke(card, player);
        }

        private void RaiseSuitDeclared(string suit)
        {
            OnSuitDeclaredInternal?.Invoke(suit);
        }

        private void RaiseDrawCardsEffect(int amount)
        {
            OnDrawCardsEffectInternal?.Invoke(amount);
        }

        private void RaiseSkipTurn()
        {
            OnSkipTurnInternal?.Invoke();
        }

        private void RaiseSequentialPlayChanged(bool active)
        {
            OnSequentialPlayChangedInternal?.Invoke(active);
        }

        private void RaiseMacauCalled(string playerName)
        {
            OnMacauCalledInternal?.Invoke(playerName);
        }

        private void RaiseStopMacauCalled(string message)
        {
            OnStopMacauCalledInternal?.Invoke(message);
        }

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
            Debug.Log("GameManager: Start called");
            // Log the initial state
            Debug.Log($"Initial Game State: {CurrentState}");

            // Set initial state to WaitingToStart
            CurrentState = GameState.WaitingToStart;
            RaiseGameStateChanged(CurrentState);

            // Initialize UI components
            InitializeSuitSelector();
            InitializeSimulationUI();
        }

        private void InitializeSuitSelector()
        {
            try
            {
                // Find or create canvas
                Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
                if (canvas == null)
                {
                    GameObject canvasObj = new GameObject("GameCanvas");
                    canvas = canvasObj.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvasObj.AddComponent<CanvasScaler>();
                    canvasObj.AddComponent<GraphicRaycaster>();
                    DontDestroyOnLoad(canvasObj);
                }

                // Create suit selector if it doesn't exist
                if (suitSelector == null)
                {
                    GameObject selectorObj = new GameObject("SuitSelector");
                    selectorObj.transform.SetParent(canvas.transform, false);
                    
                    // Add required components
                    RectTransform rt = selectorObj.AddComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    
                    suitSelector = selectorObj.AddComponent<SuitSelector>();
                    
                    // Initialize after all components are added
                    suitSelector.Initialize();
                    suitSelector.gameObject.SetActive(false);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Failed to initialize SuitSelector");
            }
        }

        private void InitializeSimulationUI()
        {
            try
            {
                Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    // Add simulation UI
                    GameObject simUIObj = new GameObject("SimulationUI");
                    simUIObj.transform.SetParent(canvas.transform, false);
                    simUIObj.AddComponent<SimulationUI>();
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "Failed to initialize SimulationUI");
            }
        }

        public void InitializeSimulation(int playerCount, float delay, bool logging)
        {
            try
            {
                Debug.Log($"Initializing simulation with {playerCount} players");
                
                // Store simulation settings
                this.PlayerCount = playerCount;
                this.detailedLogging = logging;
                this.simulationDelay = delay;
                this.isSimulation = true;

                // Clear existing players
                players.Clear();
                currentPlayerIndex = 0;

                // Create players
                InitializePlayers(playerCount);

                // Initialize game state
                CurrentState = GameState.WaitingToStart;
                isSequentialPlayActive = false;
                currentDrawAmount = 0;
                declaredSuit = null;
                turnCount = 1;

                // Reset deck without reinitializing
                if (deck != null)
                {
                    // Clear all cards and reinitialize only if empty
                    if (deck.RemainingCards == 0)
                    {
                        deck.Initialize();
                    }
                    else
                    {
                        deck.Shuffle(); // Just shuffle if already initialized
                    }
                }

                // Deal initial cards
                DealInitialHands();

                // Start the game
                StartNewGame();

                Debug.Log("Simulation initialized successfully");
            }
            catch (System.Exception ex)
            {
                ErrorHandler.HandleException(ex, "Failed to initialize simulation");
                throw;
            }
        }

        private void InitializePlayers(int playerCount)
        {
            Debug.Log($"Initializing {playerCount} players");
            for (int i = 0; i < playerCount; i++)
            {
                string playerName = i == 0 ? "Player" : $"CPU {i}";
                bool isHuman = i == 0;
                var player = new Player(playerName, isHuman, cardRules);
                players.Add(player);
            }
        }

        private void DealInitialHands()
        {
            foreach (var player in players)
            {
                for (int i = 0; i < INITIAL_HAND_SIZE; i++)
                {
                    var card = DrawCard();
                    if (card != null)
                    {
                        player.AddCard(card);
                    }
                }
            }

            // Draw first card for discard pile
            var firstDiscard = DrawCard() as Card;
            if (firstDiscard != null)
            {
                topDiscard = firstDiscard;
            }
        }

        public void StartNewGame()
        {
            Debug.Log("GameManager: Starting new game");
            Debug.Log($"Current State before starting new game: {CurrentState}");

            try
            {
                // Reset game state if needed
                if (CurrentState == GameState.GameOver)
                {
                    deck.Initialize();
                    foreach (var player in players)
                    {
                        var playerHand = player.Hand.ToList();
                        foreach (var card in playerHand)
                        {
                            player.RemoveCard(card);
                        }
                    }
                    DealInitialHands();
                }

                // Set game state to player turn
                CurrentState = GameState.PlayerTurn;
                RaiseGameStateChanged(CurrentState);
                RaisePlayerChanged(CurrentPlayer);

                // Start CPU turn if first player is CPU
                if (!CurrentPlayer.IsHuman)
                {
                    PlayCPUTurn();
                }
            }
            catch (System.Exception ex)
            {
                ErrorHandler.HandleException(ex, "Failed to start new game");
                throw;
            }
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

        public ICard DrawCard()
        {
            try
            {
                if (deck == null)
                    throw new GameValidationException("Deck not initialized");

                ICard card = deck.DrawCard();
                RaiseCardDrawn(card, CurrentPlayer);
                return card;
            }
            catch (GameValidationException ex)
            {
                ErrorHandler.HandleException(ex, "DrawCard");
                return null;
            }
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
                return cardRules.IsDrawCardCounter(card, topDiscard);
            }

            return cardRules.CanPlayOnTop(card, topDiscard, isSequentialPlayActive, declaredSuit);
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
            var drawnCard = DrawCard() as Card;
            if (drawnCard != null)
            {
                RaiseCardDrawn(drawnCard, CurrentPlayer);
                CurrentPlayer.AddCard(drawnCard);

                if (IsValidPlay(drawnCard))
                {
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
            RaiseDrawCardsEffect(0); // Clear UI notification
            DrawMultipleCards();
        }

        private void DrawMultipleCards()
        {
            while (remainingCardsToDraw > 0)
            {
                var drawnCard = DrawCard() as Card;
                if (drawnCard != null)
                {
                    RaiseCardDrawn(drawnCard, CurrentPlayer);
                    CurrentPlayer.AddCard(drawnCard);
                    remainingCardsToDraw--;
                }
                else
                {
                    break;
                }
            }

            // After drawing all required cards, check if player can play
            var currentPlayerHand = (CurrentPlayer as Player)?.Hand;
            var playableCard = currentPlayerHand?.FirstOrDefault(c => IsValidPlay(c as Card)) as Card;
            
            if (playableCard == null)
            {
                // Draw one more card to check if it can be played
                var extraCard = DrawCard() as Card;
                if (extraCard != null)
                {
                    RaiseCardDrawn(extraCard, CurrentPlayer);
                    CurrentPlayer.AddCard(extraCard);

                    if (!IsValidPlay(extraCard))
                    {
                        isDrawingMultipleCards = false;
                        NextTurn();
                    }
                }
                else
                {
                    isDrawingMultipleCards = false;
                    NextTurn();
                }
            }
        }

        public void PlayCard(ICard card, IPlayer player)
        {
            try
            {
                var concreteCard = card as Card;
                var concretePlayer = player as Player;
                if (concreteCard == null || concretePlayer == null)
                {
                    throw new GameValidationException("Invalid card or player type");
                }

                ValidatePlayCard(concreteCard, concretePlayer);

                var parameters = new Dictionary<string, object>
                {
                    { "Player", player.Name },
                    { "Card", $"{card.Rank}{card.Suit}" },
                    { "TurnNumber", turnCount }
                };
                GameLogger.Instance.LogGameEvent("CardPlayed", parameters);

                // Handle special effects
                var effect = cardRules.GetCardEffect(card);
                HandleSpecialEffect(effect, concreteCard, concretePlayer);

                DiscardCard(concreteCard);
                RaiseCardPlayed(card, player);

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
                        currentDrawAmount += cardRules.GetDrawAmount(card);
                        RaiseDrawCardsEffect(currentDrawAmount);
                        break;

                    case SpecialEffect.PopCup:
                        if (!cardRules.IsPopCupCounter(card, topDiscard))
                        {
                            currentDrawAmount = 5;
                            RaiseDrawCardsEffect(currentDrawAmount);
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
                        RaiseSkipTurn();
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
                            RaiseSuitDeclared(declaredSuit);
                        }
                        break;

                    case SpecialEffect.Sequential:
                        isSequentialPlayActive = true;
                        RaiseSequentialPlayChanged(true);
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

        public void CallMacau(IPlayer player)
        {
            if (player.Hand.Count == 1 && !hasCalledMacau)
            {
                hasCalledMacau = true;
                RaiseMacauCalled(player.Name);
            }
        }

        public void CallStopMacau(IPlayer caller, IPlayer target)
        {
            var concreteTarget = target as Player;
            if (canCallStopMacau && concreteTarget?.Hand.Count == 1 && !hasCalledMacau)
            {
                RaiseStopMacauCalled($"{caller.Name} caught {target.Name} not calling Macau!");
                ForcePlayerDraw(concreteTarget, STOP_MACAU_PENALTY);
            }
        }

        private void ForcePlayerDraw(IPlayer player, int amount)
        {
            for (int i = 0; i < amount; i++)
            {
                var card = DrawCard() as Card;
                if (card != null)
                {
                    player.AddCard(card);
                }
            }
        }

        public void CheckGameEnd()
        {
            // Check if any player has won
            foreach (var player in players)
            {
                if (player.Hand.Count == 0)
                {
                    CurrentState = GameState.GameOver;
                    Debug.Log($"{player.Name} wins the game!");
                    RaiseGameStateChanged(CurrentState);
                    return;
                }
            }

            // Check if deck is empty and no one can play
            if (deck.IsEmpty() && players.All(p => !p.Hand.Any(c => IsValidPlay(c as Card))))
            {
                CurrentState = GameState.GameOver;
                // Find player with fewest cards
                var winner = players.OrderBy(p => p.Hand.Count).First();
                Debug.Log($"Game Over - Deck empty! {winner.Name} wins with fewest cards ({winner.Hand.Count})!");
                RaiseGameStateChanged(CurrentState);
            }
        }

        public void AddCardToHand(ICard card)
        {
            if (CurrentPlayer.IsHuman)
            {
                CurrentPlayer.AddCard(card);
            }
        }

        public void RemoveCardFromHand(ICard card)
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
            RaiseSuitDeclared(suit);
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
            Debug.Log("GameManager: NextTurn called");
            // Log the current player and state
            Debug.Log($"Current Player: {CurrentPlayer.Name}, State: {CurrentState}");
            ValidateNextTurn();
            currentPlayerIndex = (currentPlayerIndex + 1) % players.Count;
            turnCount++;
            RaisePlayerChanged(CurrentPlayer);

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

            CurrentPlayer.CheckMacau();

            var cardToPlay = CurrentPlayer.GetBestPlay(topDiscard, declaredSuit, isSequentialPlayActive) as Card;

            if (cardToPlay != null)
            {
                if (detailedLogging)
                {
                    Debug.Log($"{CurrentPlayer.Name} played {cardToPlay.Rank}{cardToPlay.Suit}");
                }

                CurrentPlayer.RemoveCard(cardToPlay);
                RaiseCardPlayed(cardToPlay, CurrentPlayer);

                yield return new WaitForSeconds(simulationDelay);

                PlayCard(cardToPlay, CurrentPlayer);

                if (CurrentState == GameState.GameOver)
                {
                    yield break;
                }
            }
            else
            {
                var drawnCard = DrawCard() as Card;
                if (drawnCard != null)
                {
                    if (detailedLogging)
                    {
                        Debug.Log($"{CurrentPlayer.Name} drew {drawnCard.Rank}{drawnCard.Suit}");
                    }

                    RaiseCardDrawn(drawnCard, CurrentPlayer);
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
            RaiseDrawCardsEffect(0);

            // Log the reset
            ErrorHandler.LogWarning("Game state has been reset due to an error");
        }

        private void CheckWinCondition(IPlayer player)
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
                RaiseGameStateChanged(CurrentState);
            }
        }

        public void Initialize(IDeck deckInstance, ICardRules rulesInstance, IUIManager uiManagerInstance)
        {
            try
            {
                if (deckInstance == null)
                    throw new ArgumentNullException(nameof(deckInstance));
                if (rulesInstance == null)
                    throw new ArgumentNullException(nameof(rulesInstance));

                // Store references to injected dependencies
                this.cardRules = rulesInstance;

                // Check if deckInstance is already a Deck component
                var existingDeck = deckInstance as Deck;
                if (existingDeck != null)
                {
                    this.deck = existingDeck;
                }
                else
                {
                    // Find existing deck or create new one
                    this.deck = UnityEngine.Object.FindAnyObjectByType<Deck>();
                    if (this.deck == null)
                    {
                        // Create a new GameObject for the deck
                        var deckObj = new GameObject("Deck");
                        deckObj.transform.SetParent(transform);
                        this.deck = deckObj.AddComponent<Deck>();
                    }
                }

                // Only initialize if the deck hasn't been initialized yet
                if (this.deck.RemainingCards == 0)
                {
                    this.deck.Initialize();
                }

                Debug.Log("GameManager initialized with dependencies");

                // Set initial state
                CurrentState = GameState.WaitingToStart;
                RaiseGameStateChanged(CurrentState);
            }
            catch (Exception ex)
            {
                ErrorHandler.HandleException(ex, "GameManager initialization failed");
                throw;
            }
        }

        public void UpdateGameState(GameState newState)
        {
            Debug.Log($"GameManager: State changed to {newState}");
            // Existing logic...
        }
    }
}