using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using CardGame.Core;
using CardGame.Players;

namespace CardGame.UI
{
    public class CPUHandVisualizer : MonoBehaviour
    {
        private Dictionary<Player, List<GameObject>> cpuCards = new Dictionary<Player, List<GameObject>>();
        private float cardWidth = 100f;
        private float cardHeight = 150f;
        private float cardSpacing = 30f;
        private float handSpacing = 120f;
        private bool isInitialized = false;
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const float RETRY_DELAY = 0.5f;
        private bool isVerified = false;
        private InitializationState initState = InitializationState.NotStarted;
        private float stateValidationInterval = 2f; // Check every 2 seconds
        private bool enableRuntimeValidation = true;
        private RectTransform deckArea;
        private RectTransform discardArea;
        private const float REFERENCE_CHECK_INTERVAL = 1f;
        private bool referencesValid = false;

        // Add events
        public System.Action<string> OnReferenceError;
        public System.Action<string> OnReferenceRestored;
        public System.Action<ReferenceState> OnReferenceStateChanged;

        // Make ReferenceState public
        [System.Serializable]
        public class ReferenceState
        {
            public bool canvasValid;
            public bool deckAreaValid;
            public bool discardAreaValid;
            public bool transformValid;
            public int validCardCount;
            public int totalCardCount;
            public string lastError;
            public System.DateTime lastCheck;

            public ReferenceState()
            {
                lastCheck = System.DateTime.Now;
            }

            public bool IsFullyValid()
            {
                return canvasValid && deckAreaValid && discardAreaValid && 
                       transformValid && validCardCount == totalCardCount;
            }
        }

        private ReferenceState currentState = new ReferenceState();
        
        // Add persistence
        private const string REFERENCE_STATE_KEY = "CPUHandVisualizerState";
        private const int STATE_SAVE_INTERVAL = 30; // Save every 30 seconds
        private float lastSaveTime;

        private enum InitializationState
        {
            NotStarted,
            InProgress,
            Completed,
            Failed
        }

        public static CPUHandVisualizer Create(Transform parent)
        {
            GameObject obj = new GameObject("CPUHandVisualizer");
            obj.transform.SetParent(parent, false);
            return obj.AddComponent<CPUHandVisualizer>();
        }

        public void Initialize()
        {
            try
            {
                if (initState == InitializationState.Completed && isVerified)
                {
                    Debug.Log("CPUHandVisualizer already initialized and verified");
                    return;
                }

                initState = InitializationState.InProgress;
                Debug.Log("Starting CPUHandVisualizer initialization...");

                // Get references to deck and discard areas
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    deckArea = canvas.transform.Find("DeckArea")?.GetComponent<RectTransform>();
                    discardArea = canvas.transform.Find("DiscardArea")?.GetComponent<RectTransform>();

                    if (deckArea == null || discardArea == null)
                    {
                        Debug.LogError("Could not find deck or discard areas");
                        initState = InitializationState.Failed;
                        throw new System.Exception("Missing required UI elements");
                    }
                }
                else
                {
                    Debug.LogError("Could not find Canvas");
                    initState = InitializationState.Failed;
                    throw new System.Exception("Missing Canvas");
                }

                if (!VerifyDependencies())
                {
                    initState = InitializationState.Failed;
                    throw new System.Exception("Failed dependency verification");
                }

                cpuCards.Clear();
                isInitialized = true;
                
                if (!InitializePlayers())
                {
                    initState = InitializationState.Failed;
                    throw new System.Exception("Failed to initialize players");
                }

                SubscribeToEvents();
                
                if (!VerifyState())
                {
                    initState = InitializationState.Failed;
                    throw new System.Exception("Failed state verification");
                }

                initState = InitializationState.Completed;
                isVerified = true;
                Debug.Log("CPUHandVisualizer initialization completed successfully");
            }
            catch (System.Exception e)
            {
                isInitialized = false;
                isVerified = false;
                initState = InitializationState.Failed;
                Debug.LogError($"Failed to initialize CPUHandVisualizer: {e.Message}");
                StartCoroutine(RetryInitialization());
            }
        }

        private bool VerifyDependencies()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager.Instance is null");
                return false;
            }

            if (deckArea == null || discardArea == null)
            {
                Debug.LogError("Missing deck or discard area references");
                return false;
            }

            var players = GameManager.Instance.Players;
            if (players == null || players.Count == 0)
            {
                Debug.LogError("No players available");
                return false;
            }

            if (!players.Any(p => !p.IsHuman))
            {
                Debug.LogError("No CPU players found");
                return false;
            }

            return true;
        }

        private bool InitializePlayers()
        {
            bool anySuccess = false;
            foreach (var player in GameManager.Instance.Players)
            {
                if (player == null)
                {
                    Debug.LogError("Null player found in Players list");
                    continue;
                }

                if (!player.IsHuman)
                {
                    try
                    {
                        Debug.Log($"Initializing CPU player {player.Name}");
                        cpuCards[player] = new List<GameObject>();
                        UpdatePlayerHand(player);
                        anySuccess = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to initialize player {player.Name}: {e.Message}");
                    }
                }
            }
            return anySuccess;
        }

        private bool VerifyState()
        {
            if (!isInitialized)
            {
                Debug.LogError("Initialization flag not set");
                return false;
            }

            if (cpuCards == null)
            {
                Debug.LogError("CPU cards dictionary is null");
                return false;
            }

            foreach (var player in GameManager.Instance.Players)
            {
                if (!player.IsHuman)
                {
                    if (!cpuCards.ContainsKey(player))
                    {
                        Debug.LogError($"Missing cards for CPU player {player.Name}");
                        return false;
                    }

                    if (cpuCards[player] == null)
                    {
                        Debug.LogError($"Null card list for CPU player {player.Name}");
                        return false;
                    }

                    if (cpuCards[player].Count != player.Hand.Count)
                    {
                        Debug.LogError($"Card count mismatch for player {player.Name}: Visual={cpuCards[player].Count}, Actual={player.Hand.Count}");
                        return false;
                    }
                }
            }

            return true;
        }

        public bool IsFullyInitialized()
        {
            return initState == InitializationState.Completed && 
                   isInitialized && 
                   isVerified && 
                   VerifyState();
        }

        private void LogInitializationStatus()
        {
            Debug.Log($"CPUHandVisualizer Status:\n" +
                     $"State: {initState}\n" +
                     $"Initialized: {isInitialized}\n" +
                     $"Verified: {isVerified}\n" +
                     $"Player Count: {cpuCards.Count}\n" +
                     $"All Checks Pass: {IsFullyInitialized()}");
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                UnsubscribeFromEvents(); // Ensure we don't double-subscribe
                GameManager.Instance.OnCardPlayed += HandleCardPlayed;
                GameManager.Instance.OnCardDrawn += HandleCardDrawn;
            }
            else
            {
                Debug.LogError("Cannot subscribe to events - GameManager.Instance is null");
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCardPlayed -= HandleCardPlayed;
                GameManager.Instance.OnCardDrawn -= HandleCardDrawn;
            }
        }

        private void UpdatePlayerHand(Player player, bool skipValidation = false)
        {
            // Skip validation checks during recovery
            if (!skipValidation && !ValidatePlayer(player, "UpdatePlayerHand")) return;

            try
            {
                if (!cpuCards.ContainsKey(player))
                {
                    cpuCards[player] = new List<GameObject>();
                }

                // Clean up existing cards
                CleanupExistingCards(player);

                // Create new cards to match actual hand count
                int handCount = player.Hand.Count;
                for (int i = 0; i < handCount; i++)
                {
                    GameObject cardObj = CreateFaceDownCard();
                    cardObj.transform.SetParent(transform, false);
                    
                    Vector2 position = GetHandPosition(GetPlayerIndex(player)) + new Vector2(i * cardSpacing, 0);
                    RectTransform rt = cardObj.GetComponent<RectTransform>();
                    rt.anchoredPosition = position;
                    rt.localRotation = GetHandRotation(GetPlayerIndex(player));
                    
                    cpuCards[player].Add(cardObj);
                }

                Debug.Log($"Updated {player.Name}'s hand. Visual cards: {cpuCards[player].Count}, Actual cards: {handCount}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating hand for player {player.Name}: {e.Message}");
            }
        }

        private IEnumerator RetryUpdateHand(Player player, int attemptCount = 0)
        {
            if (attemptCount >= MAX_RETRY_ATTEMPTS)
            {
                Debug.LogError($"Failed to update hand for player {player.Name} after maximum retry attempts");
                yield break;
            }

            yield return new WaitForSeconds(RETRY_DELAY);
            UpdatePlayerHand(player);
        }

        private void CleanupExistingCards(Player player)
        {
            foreach (var card in cpuCards[player].Where(c => c != null))
            {
                try
                {
                    Destroy(card);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error destroying card: {e.Message}");
                }
            }
            cpuCards[player].Clear();
        }

        private void CreateNewCards(Player player)
        {
            int playerIndex = GetPlayerIndex(player);
            Vector2 startPos = GetHandPosition(playerIndex);
            Quaternion rotation = GetHandRotation(playerIndex);

            for (int i = 0; i < player.Hand.Count; i++)
            {
                try
                {
                    GameObject cardObj = CreateFaceDownCard();
                    if (cardObj == null)
                    {
                        Debug.LogError("Failed to create face down card");
                        continue;
                    }

                    cardObj.transform.SetParent(transform, false);
                    SetupCardTransform(cardObj, startPos + new Vector2(i * cardSpacing, 0), rotation);
                    cpuCards[player].Add(cardObj);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error creating card {i} for player {player.Name}: {e.Message}");
                }
            }
        }

        private void SetupCardTransform(GameObject cardObj, Vector2 position, Quaternion rotation)
        {
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            if (rt == null)
            {
                Debug.LogError("Card object missing RectTransform");
                return;
            }

            rt.anchoredPosition = position;
            rt.localRotation = rotation;
        }

        private bool ValidatePlayer(Player player, string context)
        {
            if (!referencesValid)
            {
                Debug.LogError($"{context}: References invalid");
                return false;
            }

            if (player == null)
            {
                Debug.LogError($"{context}: Player is null");
                return false;
            }

            if (context == "UpdatePlayerHand" && initState == InitializationState.InProgress)
            {
                return true;
            }

            if (!IsFullyInitialized())
            {
                Debug.LogError($"{context}: CPUHandVisualizer not fully initialized (State: {initState})");
                return false;
            }

            ValidatePlayerState(player);
            return cpuCards.ContainsKey(player);
        }

        private void OnDestroy()
        {
            try
            {
                UnsubscribeFromEvents();
                CleanupAllCards();
                isInitialized = false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during CPUHandVisualizer cleanup: {e.Message}");
            }
        }

        private void CleanupAllCards()
        {
            foreach (var playerCards in cpuCards)
            {
                foreach (var card in playerCards.Value.Where(c => c != null))
                {
                    try
                    {
                        Destroy(card);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"Error destroying card during cleanup: {e.Message}");
                    }
                }
            }
            cpuCards.Clear();
        }

        private GameObject CreateFaceDownCard()
        {
            GameObject cardObj = new GameObject("FaceDownCard");
            
            // Add background
            Image cardImage = cardObj.AddComponent<Image>();
            cardImage.color = Color.white;
            
            // Add card back design
            GameObject backDesignObj = new GameObject("BackDesign");
            backDesignObj.transform.SetParent(cardObj.transform, false);
            Image backDesign = backDesignObj.AddComponent<Image>();
            backDesign.sprite = CardBackDesign.CreateCardBackSprite();
            
            // Setup transforms
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(cardWidth, cardHeight);
            
            RectTransform backRT = backDesignObj.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0.1f, 0.1f);
            backRT.anchorMax = new Vector2(0.9f, 0.9f);
            backRT.offsetMin = Vector2.zero;
            backRT.offsetMax = Vector2.zero;

            // Add white border
            GameObject borderObj = new GameObject("Border");
            borderObj.transform.SetParent(cardObj.transform, false);
            Image border = borderObj.AddComponent<Image>();
            border.color = Color.white;
            
            RectTransform borderRT = borderObj.GetComponent<RectTransform>();
            borderRT.anchorMin = Vector2.zero;
            borderRT.anchorMax = Vector2.one;
            borderRT.offsetMin = new Vector2(2, 2);
            borderRT.offsetMax = new Vector2(-2, -2);
            borderRT.SetAsFirstSibling();

            return cardObj;
        }

        public Vector2 GetHandPosition(int playerIndex)
        {
            float angle = (playerIndex * 90f) / GameManager.Instance.PlayerCount;
            float radius = handSpacing * 2f;
            
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * radius + handSpacing;
            
            return new Vector2(x, y);
        }

        public Quaternion GetHandRotation(int playerIndex)
        {
            float angle = (playerIndex * 90f) / GameManager.Instance.PlayerCount;
            return Quaternion.Euler(0, 0, -angle);
        }

        private void HandleCardPlayed(Card card, Player player)
        {
            if (!referencesValid)
            {
                Debug.LogError("Cannot handle card played - references invalid");
                return;
            }

            if (player == null || !player.IsHuman)
            {
                StartCoroutine(HandleCardPlayedAnimation(card, player));
            }
        }

        private IEnumerator HandleCardPlayedAnimation(Card card, Player player)
        {
            if (cpuCards.ContainsKey(player) && cpuCards[player].Count > 0)
            {
                // Remove and animate the last card
                GameObject cardToRemove = cpuCards[player][cpuCards[player].Count - 1];
                cpuCards[player].RemoveAt(cpuCards[player].Count - 1);

                // Animate to discard pile
                float duration = 0.3f;
                float elapsed = 0f;
                RectTransform rt = cardToRemove.GetComponent<RectTransform>();
                Vector2 startPos = rt.anchoredPosition;
                Vector2 endPos = discardArea.anchoredPosition;

                while (elapsed < duration)
                {
                    float t = elapsed / duration;
                    rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                    rt.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                Destroy(cardToRemove);

                // Wait a frame to ensure the card is removed from the player's hand
                yield return null;

                // Verify the count matches
                if (cpuCards[player].Count != player.Hand.Count)
                {
                    Debug.LogWarning($"Card count mismatch after play. Visual: {cpuCards[player].Count}, Actual: {player.Hand.Count}. Fixing...");
                    UpdatePlayerHand(player);
                }
            }
        }

        private void HandleCardDrawn(Card card, Player player)
        {
            if (!referencesValid)
            {
                Debug.LogError("Cannot handle card drawn - references invalid");
                return;
            }

            if (player == null)
            {
                Debug.LogError("HandleCardDrawn: Player is null");
                return;
            }

            if (!player.IsHuman)
            {
                // Wait for the card to be added to the player's hand
                StartCoroutine(HandleCardDrawnAnimation(card, player));
            }
        }

        private IEnumerator HandleCardDrawnAnimation(Card card, Player player)
        {
            // Wait a frame to ensure the card is added to the player's hand
            yield return null;

            if (!cpuCards.ContainsKey(player))
            {
                cpuCards[player] = new List<GameObject>();
            }

            // Create and animate the new card
            GameObject cardObj = CreateFaceDownCard();
            cardObj.transform.SetParent(transform, false);
            
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            Vector2 startPos = deckArea.anchoredPosition;
            Vector2 endPos = GetHandPosition(GetPlayerIndex(player)) + new Vector2(cpuCards[player].Count * cardSpacing, 0);
            rt.anchoredPosition = startPos;
            rt.localRotation = GetHandRotation(GetPlayerIndex(player));

            // Add the card to our list
            cpuCards[player].Add(cardObj);

            // Animate the card movement
            float duration = 0.3f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = elapsed / duration;
                rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                elapsed += Time.deltaTime;
                yield return null;
            }
            rt.anchoredPosition = endPos;

            // Skip validation during animation
            yield return new WaitForSeconds(0.1f);
        }

        private int GetPlayerIndex(Player player)
        {
            return GameManager.Instance.GetPlayerIndex(player);
        }

        private IEnumerator RetryInitialization(int attemptCount = 0)
        {
            if (attemptCount >= MAX_RETRY_ATTEMPTS)
            {
                Debug.LogError("Failed to initialize CPUHandVisualizer after maximum retry attempts");
                yield break;
            }

            Debug.Log($"Retrying initialization (attempt {attemptCount + 1}/{MAX_RETRY_ATTEMPTS})...");
            yield return new WaitForSeconds(RETRY_DELAY);
            Initialize();
        }

        private void Start()
        {
            LoadReferenceState();
            if (enableRuntimeValidation)
            {
                StartCoroutine(RuntimeStateValidation());
                StartCoroutine(ReferenceValidationRoutine());
            }
        }

        private IEnumerator RuntimeStateValidation()
        {
            WaitForSeconds wait = new WaitForSeconds(stateValidationInterval);
            
            while (enabled)
            {
                yield return wait;
                
                if (!IsFullyInitialized())
                {
                    Debug.LogWarning("Runtime validation: CPUHandVisualizer not fully initialized");
                    continue;
                }

                ValidateRuntimeState();
            }
        }

        private void ValidateRuntimeState()
        {
            try
            {
                // Check GameManager connection
                if (GameManager.Instance == null)
                {
                    LogStateError("GameManager.Instance is null during runtime");
                    AttemptStateRecovery();
                    return;
                }

                // Validate all CPU players
                foreach (var player in GameManager.Instance.Players)
                {
                    if (!player.IsHuman)
                    {
                        ValidatePlayerState(player);
                    }
                }

                // Check for orphaned card objects
                ValidateCardObjects();

                // Verify card counts match
                ValidateCardCounts();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Runtime validation error: {e.Message}");
            }
        }

        private void ValidatePlayerState(Player player)
        {
            if (!cpuCards.ContainsKey(player))
            {
                LogStateError($"Player {player.Name} missing from cpuCards dictionary");
                RecoverPlayerState(player);
                return;
            }

            var cardList = cpuCards[player];
            if (cardList == null)
            {
                LogStateError($"Null card list for player {player.Name}");
                RecoverPlayerState(player);
                return;
            }

            // Check for null card objects
            var nullCards = cardList.Where(c => c == null).Count();
            if (nullCards > 0)
            {
                LogStateError($"Found {nullCards} null card objects for player {player.Name}");
                RecoverPlayerState(player);
            }
        }

        private void ValidateCardObjects()
        {
            foreach (var playerCards in cpuCards)
            {
                var invalidCards = playerCards.Value.Where(card => 
                    card != null && (!card.activeInHierarchy || card.transform.parent != transform));
                
                foreach (var card in invalidCards)
                {
                    LogStateError($"Found card with invalid state for player {playerCards.Key.Name}");
                    Destroy(card);
                }

                playerCards.Value.RemoveAll(card => card == null || !card.activeInHierarchy);
            }
        }

        private void ValidateCardCounts()
        {
            foreach (var kvp in cpuCards)
            {
                Player player = kvp.Key;
                int visualCount = kvp.Value.Count;
                int actualCount = player.Hand.Count;

                if (visualCount != actualCount)
                {
                    LogStateError($"Card count mismatch for {player.Name}: Visual={visualCount}, Actual={actualCount}");
                    RecoverPlayerState(player);
                }
            }
        }

        private void LogStateError(string message)
        {
            Debug.LogError($"[Runtime State Error] {message}");
            LogInitializationStatus(); // Log current state for debugging
        }

        private void AttemptStateRecovery()
        {
            Debug.Log("Attempting state recovery...");
            
            // Reset state
            isInitialized = false;
            isVerified = false;
            initState = InitializationState.NotStarted;
            
            // Clean up
            CleanupAllCards();
            
            // Reinitialize
            Initialize();
        }

        private void RecoverPlayerState(Player player)
        {
            Debug.Log($"Recovering state for player {player.Name}...");
            
            // Clean up existing cards for this player
            if (cpuCards.ContainsKey(player))
            {
                CleanupExistingCards(player);
            }
            
            // Reinitialize player's cards with validation disabled
            cpuCards[player] = new List<GameObject>();
            UpdatePlayerHand(player, true);
        }

        private IEnumerator ReferenceValidationRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(REFERENCE_CHECK_INTERVAL);
            
            while (enabled)
            {
                yield return wait;
                ValidateReferences();
            }
        }

        private void ValidateReferences()
        {
            try
            {
                ReferenceState newState = new ReferenceState();
                bool previousValid = referencesValid;
                referencesValid = true;

                // Check Canvas
                Canvas canvas = FindFirstObjectByType<Canvas>();
                newState.canvasValid = canvas != null;
                if (!newState.canvasValid)
                {
                    LogReferenceError("Canvas not found");
                    referencesValid = false;
                }

                // Check Deck Area
                if (deckArea == null)
                {
                    deckArea = canvas?.transform.Find("DeckArea")?.GetComponent<RectTransform>();
                }
                newState.deckAreaValid = deckArea != null && deckArea.gameObject.activeInHierarchy;
                if (!newState.deckAreaValid)
                {
                    LogReferenceError("DeckArea reference invalid");
                    referencesValid = false;
                }

                // Check Discard Area
                if (discardArea == null)
                {
                    discardArea = canvas?.transform.Find("DiscardArea")?.GetComponent<RectTransform>();
                }
                newState.discardAreaValid = discardArea != null && discardArea.gameObject.activeInHierarchy;
                if (!newState.discardAreaValid)
                {
                    LogReferenceError("DiscardArea reference invalid");
                    referencesValid = false;
                }

                // Check Transform hierarchy
                newState.transformValid = transform.parent != null;
                if (!newState.transformValid)
                {
                    LogReferenceError("CPUHandVisualizer parent missing");
                    referencesValid = false;
                }

                // Check card objects
                int validCards = 0;
                int totalCards = 0;
                foreach (var playerCards in cpuCards)
                {
                    foreach (var card in playerCards.Value.ToList())
                    {
                        totalCards++;
                        if (card == null) continue;

                        bool cardValid = true;
                        if (card.transform.parent != transform)
                        {
                            LogReferenceError($"Card {card.name} has incorrect parent");
                            card.transform.SetParent(transform, false);
                            cardValid = false;
                        }

                        if (!card.activeInHierarchy)
                        {
                            LogReferenceError($"Card {card.name} inactive");
                            card.SetActive(true);
                            cardValid = false;
                        }

                        if (card.GetComponent<RectTransform>() == null)
                        {
                            LogReferenceError($"Card {card.name} missing RectTransform");
                            playerCards.Value.Remove(card);
                            cardValid = false;
                        }

                        if (cardValid) validCards++;
                    }
                }
                newState.validCardCount = validCards;
                newState.totalCardCount = totalCards;

                // Update state and trigger events
                if (!currentState.IsFullyValid() && newState.IsFullyValid())
                {
                    OnReferenceRestored?.Invoke("All references restored");
                }
                else if (currentState.IsFullyValid() && !newState.IsFullyValid())
                {
                    OnReferenceError?.Invoke("References became invalid");
                }

                currentState = newState;
                OnReferenceStateChanged?.Invoke(currentState);

                // Save state periodically
                if (Time.time - lastSaveTime > STATE_SAVE_INTERVAL)
                {
                    SaveReferenceState();
                }

                // Handle recovery if needed
                if (!previousValid && referencesValid)
                {
                    Debug.Log("References restored successfully");
                    AttemptStateRecovery();
                }
                else if (previousValid && !referencesValid)
                {
                    Debug.LogWarning("References became invalid, attempting recovery...");
                    AttemptReferenceRecovery();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during reference validation: {e.Message}");
                referencesValid = false;
            }
        }

        private void AttemptReferenceRecovery()
        {
            Debug.Log("Attempting to recover references...");
            
            try
            {
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    // Reparent to canvas if needed
                    if (transform.parent != canvas.transform)
                    {
                        transform.SetParent(canvas.transform, false);
                    }

                    // Attempt to find references
                    deckArea = canvas.transform.Find("DeckArea")?.GetComponent<RectTransform>();
                    discardArea = canvas.transform.Find("DiscardArea")?.GetComponent<RectTransform>();

                    if (deckArea != null && discardArea != null)
                    {
                        Debug.Log("References recovered successfully");
                        
                        // Update all card positions
                        foreach (var player in GameManager.Instance.Players)
                        {
                            if (!player.IsHuman)
                            {
                                UpdatePlayerHand(player);
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError("Failed to recover all references");
                    }
                }
                else
                {
                    Debug.LogError("Cannot recover references - Canvas not found");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during reference recovery: {e.Message}");
            }
        }

        private void LogReferenceError(string message)
        {
            Debug.LogError($"[Reference Error] {message}");
            referencesValid = false;
            currentState.lastError = message;
            OnReferenceError?.Invoke(message);
        }

        private void SaveReferenceState()
        {
            try
            {
                string stateJson = JsonUtility.ToJson(currentState);
                PlayerPrefs.SetString(REFERENCE_STATE_KEY, stateJson);
                PlayerPrefs.Save();
                lastSaveTime = Time.time;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save reference state: {e.Message}");
            }
        }

        private void LoadReferenceState()
        {
            try
            {
                if (PlayerPrefs.HasKey(REFERENCE_STATE_KEY))
                {
                    string stateJson = PlayerPrefs.GetString(REFERENCE_STATE_KEY);
                    ReferenceState savedState = JsonUtility.FromJson<ReferenceState>(stateJson);
                    
                    // Check if saved state is recent enough (within last hour)
                    if ((System.DateTime.Now - savedState.lastCheck).TotalHours < 1)
                    {
                        currentState = savedState;
                        OnReferenceStateChanged?.Invoke(currentState);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load reference state: {e.Message}");
            }
        }
    }
} 