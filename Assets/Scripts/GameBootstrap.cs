using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CardGame;
using CardGame.Core;
using CardGame.Stats;
using CardGame.Utils;

public class GameBootstrap : MonoBehaviour
{
    private GameSetup gameSetup;
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnGameStart()
    {
        // Check if GameSetup already exists
        if (Object.FindAnyObjectByType<GameSetup>() == null)
        {
            // Create the initial setup
            GameObject setupObj = new GameObject("GameSetup");
            var setup = setupObj.AddComponent<GameSetup>();
            DontDestroyOnLoad(setupObj);
        }
    }

    private void Start()
    {
        gameSetup = Object.FindAnyObjectByType<GameSetup>();
        if (gameSetup == null)
        {
            Debug.LogError("GameSetup not found!");
            return;
        }

        // Configure the game through GameSetup
        gameSetup.ConfigureGame(3, true); // 3 players, simulation mode
    }
} 