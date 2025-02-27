using System.Collections.Generic;
using CardGame.Core;
using CardGame.Core.Interfaces;

namespace CardGame.Players.Interfaces
{
    public interface IPlayer
    {
        string Name { get; }
        bool IsHuman { get; }
        IReadOnlyList<ICard> Hand { get; }
        void AddCard(ICard card);
        void RemoveCard(ICard card);
        void CheckMacau();
        void CheckStopMacau(IPlayer target);
        ICard GetBestPlay(ICard topCard, string declaredSuit, bool isSequentialPlay);
        string GetMostCommonSuit();
    }
} 