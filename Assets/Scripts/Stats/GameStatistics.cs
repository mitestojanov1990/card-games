using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CardGame.Core;
using CardGame.Core.Interfaces;
using CardGame.Players;
using CardGame.Players.Interfaces;
using CardGame.Rules;
using CardGame.Rules.Interfaces;
using CardGame.Utils;
using CardGame.DI;

namespace CardGame.Stats
{
    public class GameStatistics : MonoBehaviour
    {
        // Add batch simulation settings
        public int batchSize = 100;
        public int currentBatch = 0;
        private bool isBatchRunning = false;

        // Add more detailed statistics
        public class GameStats
        {
            public float duration;
            public int totalTurns;
            public string winner;
            public int finalDeckSize;
            public Dictionary<string, int> finalHandSizes = new Dictionary<string, int>();
            public int specialEffectsTriggered;
            public int drawCardEvents;
            public int sequentialPlays;
            public bool wasMacauCalled;
            public bool wasStopMacauCalled;
        }

        public class PlayerStats
        {
            public string playerName;
            public int cardsPlayed;
            public int cardsDrawn;
            public int specialCardsPlayed;
            public int macauCalls;
            public int stopMacauCalls;
            public int drawCardsPassed;
            public int drawCardsReceived;
            public Dictionary<string, int> suitPlayCount = new Dictionary<string, int>();
            public Dictionary<string, int> rankPlayCount = new Dictionary<string, int>();
            public float averageHandSize;
            public int turnCount;
            public int wins;
            public float averageDecisionTime;
            public List<float> decisionTimes = new List<float>();
            public int sequentialPlayChains;
            public int longestSequentialChain;
            public int successfulDrawCardCounters;
            public int failedDrawCardCounters;
            public int successfulPopCupCounters;
            public int missedMacauCalls;
            public int correctStopMacauCalls;
            public int wrongStopMacauCalls;
            public float winRate;
            public List<int> turnsTillFirstPlay = new List<int>();
            public Dictionary<SpecialEffect, int> specialEffectUsage = new Dictionary<SpecialEffect, int>();

            public PlayerStats(string name)
            {
                playerName = name;
                foreach (var suit in new[] { "♥", "♦", "♣", "♠" })
                    suitPlayCount[suit] = 0;
                foreach (SpecialEffect effect in System.Enum.GetValues(typeof(SpecialEffect)))
                    specialEffectUsage[effect] = 0;
            }
        }

        private Dictionary<string, PlayerStats> playerStats = new Dictionary<string, PlayerStats>();
        private float gameStartTime;
        private int totalTurns;
        private int gamesPlayed;
        private List<string> gameLog = new List<string>();
        private float lastActionTime;
        private List<GameStats> batchStats = new List<GameStats>();
        private GameStats currentGameStats;
        private int currentSequentialChain = 0;

        private readonly ICardRules cardRules;

        private void Awake()
        {
            cardRules = CardRules.Instance;
        }

        public void Initialize()
        {
            gameStartTime = Time.time;
            lastActionTime = gameStartTime;
            totalTurns = 0;
            
            // Subscribe to GameManager events
            GameContainer.Instance.GameManager.OnCardPlayed += HandleCardPlayed;
            GameContainer.Instance.GameManager.OnCardDrawn += HandleCardDrawn;
            GameContainer.Instance.GameManager.OnMacauCalled += HandleMacauCalled;
            GameContainer.Instance.GameManager.OnStopMacauCalled += HandleStopMacauCalled;
            GameContainer.Instance.GameManager.OnGameStateChanged += HandleGameStateChanged;
            GameContainer.Instance.GameManager.OnPlayerChanged += HandlePlayerChanged;
        }

        public void StartBatchSimulation()
        {
            isBatchRunning = true;
            currentBatch = 0;
            batchStats.Clear();
            StartNextGame();
        }

        private void StartNextGame()
        {
            if (currentBatch < batchSize)
            {
                currentBatch++;
                currentGameStats = new GameStats();
                gameStartTime = Time.time;
                lastActionTime = gameStartTime;
                totalTurns = 0;
                currentSequentialChain = 0;
                GameContainer.Instance.GameManager.StartNewGame();
            }
            else
            {
                isBatchRunning = false;
                PrintBatchSummary();
            }
        }

        private void HandleCardPlayed(ICard card, IPlayer player)
        {
            if (!playerStats.ContainsKey(player.Name))
                playerStats[player.Name] = new PlayerStats(player.Name);

            var stats = playerStats[player.Name];
            stats.cardsPlayed++;
            stats.suitPlayCount[card.Suit]++;
            
            if (!stats.rankPlayCount.ContainsKey(card.Rank))
                stats.rankPlayCount[card.Rank] = 0;
            stats.rankPlayCount[card.Rank]++;

            var effect = cardRules.GetCardEffect(card);
            if (effect != SpecialEffect.None)
                stats.specialCardsPlayed++;

            float decisionTime = Time.time - lastActionTime;
            stats.decisionTimes.Add(decisionTime);
            
            // Safe average calculation for decision time
            if (stats.decisionTimes.Count > 0)
            {
                stats.averageDecisionTime = (float)stats.decisionTimes.Average();
            }
            
            lastActionTime = Time.time;

            LogAction($"{player.Name} played {card.Rank}{card.Suit} (Decision time: {decisionTime:F2}s)");

            stats.specialEffectUsage[effect]++;
            currentGameStats.specialEffectsTriggered++;

            // Track sequential plays
            if (effect == SpecialEffect.Sequential)
            {
                currentSequentialChain++;
                stats.sequentialPlayChains++;
                stats.longestSequentialChain = Mathf.Max(stats.longestSequentialChain, currentSequentialChain);
            }
            else
            {
                currentSequentialChain = 0;
            }

            // Track first play timing
            if (stats.cardsPlayed == 1)
            {
                stats.turnsTillFirstPlay.Add(totalTurns);
            }
        }

        private void HandleCardDrawn(ICard card, IPlayer player)
        {
            if (!playerStats.ContainsKey(player.Name))
                playerStats[player.Name] = new PlayerStats(player.Name);

            playerStats[player.Name].cardsDrawn++;
            LogAction($"{player.Name} drew a card");
        }

        private void HandleMacauCalled(string playerName)
        {
            if (!playerStats.ContainsKey(playerName))
                playerStats[playerName] = new PlayerStats(playerName);

            playerStats[playerName].macauCalls++;
            LogAction($"{playerName} called Macau");
        }

        private void HandleStopMacauCalled(string message)
        {
            LogAction($"Stop Macau: {message}");
        }

        private void HandleGameStateChanged(GameState state)
        {
            if (state == GameState.GameOver)
            {
                // Record current game stats
                currentGameStats.duration = Time.time - gameStartTime;
                currentGameStats.totalTurns = totalTurns;
                currentGameStats.winner = GameContainer.Instance.GameManager.CurrentPlayer.Name;
                currentGameStats.finalDeckSize = GameContainer.Instance.GameManager.DeckCount;
                
                foreach (var player in GameContainer.Instance.GameManager.Players)
                {
                    currentGameStats.finalHandSizes[player.Name] = player.Hand.Count;
                    if (player.Name == currentGameStats.winner)
                    {
                        playerStats[player.Name].wins++;
                    }
                }

                batchStats.Add(currentGameStats);
                
                if (isBatchRunning)
                {
                    StartNextGame();
                }
                else
                {
                    PrintGameSummary();
                }
            }
        }

        private void HandlePlayerChanged(IPlayer player)
        {
            totalTurns++;
            if (!playerStats.ContainsKey(player.Name))
                playerStats[player.Name] = new PlayerStats(player.Name);

            var stats = playerStats[player.Name];
            stats.turnCount++;
            
            // Safe average calculation for hand size
            if (stats.turnCount > 1)
            {
                stats.averageHandSize = ((stats.averageHandSize * (stats.turnCount - 1)) + player.Hand.Count) / stats.turnCount;
            }
            else
            {
                stats.averageHandSize = player.Hand.Count;
            }
        }

        private void LogAction(string message)
        {
            gameLog.Add($"[{Time.time - gameStartTime:F2}s] {message}");
        }

        public void PrintGameSummary()
        {
            Debug.Log("\n=== Game Statistics ===");
            Debug.Log($"Total turns: {totalTurns}");
            Debug.Log($"Game duration: {(Time.time - gameStartTime):F2} seconds");
            Debug.Log("\nPlayer Statistics:");

            foreach (var stat in playerStats.Values)
            {
                Debug.Log($"\n{stat.playerName}:");
                Debug.Log($"Cards played: {stat.cardsPlayed}");
                Debug.Log($"Cards drawn: {stat.cardsDrawn}");
                Debug.Log($"Special cards played: {stat.specialCardsPlayed}");
                Debug.Log($"Macau calls: {stat.macauCalls}");
                Debug.Log($"Average hand size: {stat.averageHandSize:F2}");
                Debug.Log($"Average decision time: {stat.averageDecisionTime:F2}s");
                
                Debug.Log("Suit distribution:");
                foreach (var suit in stat.suitPlayCount)
                    Debug.Log($"  {suit.Key}: {suit.Value}");
                
                Debug.Log("Rank distribution:");
                foreach (var rank in stat.rankPlayCount.OrderByDescending(x => x.Value))
                    Debug.Log($"  {rank.Key}: {rank.Value}");
            }

            Debug.Log("\nGame Log:");
            foreach (var log in gameLog.Take(20))
                Debug.Log(log);
        }

        private void PrintBatchSummary()
        {
            if (batchSize <= 0 || batchStats.Count == 0)
            {
                Debug.LogWarning("No games were played in this batch.");
                return;
            }

            Debug.Log($"\n=== Batch Simulation Summary ({batchStats.Count} games) ===");
            
            try
            {
                // Game statistics
                float avgDuration = (float)batchStats.Average(g => g.duration);
                float avgTurns = (float)batchStats.Average(g => g.totalTurns);
                Debug.Log($"Average game duration: {avgDuration:F2}s");
                Debug.Log($"Average turns per game: {avgTurns:F2}");
                Debug.Log($"Average special effects per game: {(float)batchStats.Average(g => g.specialEffectsTriggered):F2}");

                // Player statistics
                foreach (var stat in playerStats.Values)
                {
                    stat.winRate = batchSize > 0 ? (float)stat.wins / batchSize : 0;
                    Debug.Log($"\n{stat.playerName} Statistics:");
                    Debug.Log($"Win rate: {stat.winRate:P2}");
                    Debug.Log($"Average cards played per game: {(batchSize > 0 ? (float)stat.cardsPlayed / batchSize : 0):F2}");
                    Debug.Log($"Average cards drawn per game: {(batchSize > 0 ? (float)stat.cardsDrawn / batchSize : 0):F2}");
                    Debug.Log($"Average decision time: {(stat.decisionTimes.Count > 0 ? stat.averageDecisionTime : 0):F2}s");
                    Debug.Log($"Sequential play chains: {stat.sequentialPlayChains}");
                    Debug.Log($"Longest sequential chain: {stat.longestSequentialChain}");
                    
                    // Safe average calculation for turns till first play
                    float avgTurnsToFirst = 0;
                    if (stat.turnsTillFirstPlay.Count > 0)
                    {
                        avgTurnsToFirst = (float)stat.turnsTillFirstPlay.Average();
                    }
                    Debug.Log($"Average turns until first play: {avgTurnsToFirst:F2}");
                    
                    Debug.Log("Special effect usage:");
                    foreach (var effect in stat.specialEffectUsage.OrderByDescending(x => x.Value))
                    {
                        float perGame = batchSize > 0 ? (float)effect.Value / batchSize : 0;
                        Debug.Log($"  {effect.Key}: {effect.Value} times ({perGame:F2} per game)");
                    }
                }

                // Game flow analysis
                if (batchStats.Any())
                {
                    var shortestGame = batchStats.Min(g => g.totalTurns);
                    var longestGame = batchStats.Max(g => g.totalTurns);
                    Debug.Log($"\nGame length range: {shortestGame}-{longestGame} turns");
                    
                    // Winner distribution
                    var winnerStats = batchStats.GroupBy(g => g.winner)
                                              .OrderByDescending(g => g.Count());
                    Debug.Log("\nWinner distribution:");
                    foreach (var winner in winnerStats)
                    {
                        float winPercentage = batchSize > 0 ? (float)winner.Count() / batchSize : 0;
                        Debug.Log($"  {winner.Key}: {winner.Count()} wins ({winPercentage:P2})");
                    }
                }
                else
                {
                    Debug.LogWarning("No game statistics available for analysis.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error generating batch summary: {e.Message}");
                Debug.LogError($"Stack trace: {e.StackTrace}");
            }
        }

        private void OnDestroy()
        {
            if (GameContainer.Instance.GameManager != null)
            {
                GameContainer.Instance.GameManager.OnCardPlayed -= HandleCardPlayed;
                GameContainer.Instance.GameManager.OnCardDrawn -= HandleCardDrawn;
                GameContainer.Instance.GameManager.OnMacauCalled -= HandleMacauCalled;
                GameContainer.Instance.GameManager.OnStopMacauCalled -= HandleStopMacauCalled;
                GameContainer.Instance.GameManager.OnGameStateChanged -= HandleGameStateChanged;
                GameContainer.Instance.GameManager.OnPlayerChanged -= HandlePlayerChanged;
            }
        }
    }
} 