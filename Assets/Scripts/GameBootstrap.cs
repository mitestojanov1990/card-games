using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void OnGameStart()
    {
        // Check if GameSetup already exists
        if (FindFirstObjectByType<GameSetup>() == null)
        {
            // Create the initial setup
            GameObject setupObj = new GameObject("GameSetup");
            setupObj.AddComponent<GameSetup>();
            
            // Make sure it persists between scenes
            DontDestroyOnLoad(setupObj);
        }
    }
} 