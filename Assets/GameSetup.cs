using UnityEngine;

public class GameSetup : MonoBehaviour
{
    void Awake()
    {
        // Create CardGame object with CardSimulation
        if (FindFirstObjectByType<CardSimulation>() == null)
        {
            GameObject cardGame = new GameObject("CardGame");
            cardGame.AddComponent<CardSimulation>();
        }
    }
} 