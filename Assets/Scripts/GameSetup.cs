using UnityEngine;
using CardGame;

/// <summary>
/// Handles the initial setup of the game environment.
/// This class is created by GameBootstrap during game initialization.
/// </summary>
public class GameSetup : MonoBehaviour
{
    private void Awake()
    {
        // Initialize game systems and services
        Debug.Log("GameSetup: Initializing game systems");
        
        // Create any required managers or services that should persist
        // between scenes if they don't already exist
        
        // Example: Initialize settings, audio system, input system, etc.
    }
    
    public void ConfigureGame(int playerCount, bool isSimulation = false)
    {
        // Configure game parameters
        Debug.Log($"GameSetup: Configuring game with {playerCount} players (Simulation: {isSimulation})");
        
        // You can add additional configuration options here
    }
} 