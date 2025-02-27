using UnityEngine;

namespace CardGame.Core.Interfaces
{
    public interface ICard
    {
        string Rank { get; }
        string Suit { get; }
        Color Color { get; }
        int Value { get; }
    }
} 