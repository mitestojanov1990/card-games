using System.Collections.Generic;

namespace CardGame.Core.Interfaces
{
    public interface IDeck
    {
        int RemainingCards { get; }
        bool IsEmpty();
        void Shuffle();
        ICard DrawCard();
        void ValidateDrawCard();
    }
} 