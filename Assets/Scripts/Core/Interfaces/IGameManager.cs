using System;
using CardGame.Core.Interfaces;
using CardGame.Players.Interfaces;

namespace CardGame.Core.Interfaces
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

    public enum GameState
    {
        WaitingToStart,
        PlayerTurn,
        GameOver
    }
} 