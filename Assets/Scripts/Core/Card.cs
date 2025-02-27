using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using CardGame.Exceptions;
using CardGame.Core.Interfaces;

namespace CardGame.Core
{
    [System.Serializable]
    public class Card : ICard
    {
        public string Rank { get; }
        public string Suit { get; }
        public Color Color { get; }
        public int Value { get; private set; }  // Numerical value for game logic

        private static readonly string[] ValidRanks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
        private static readonly string[] ValidSuits = { "♥", "♦", "♣", "♠" };

        private static readonly Dictionary<string, int> RankValues = new Dictionary<string, int>
        {
            {"A", 14}, {"2", 2}, {"3", 3}, {"4", 4}, {"5", 5},
            {"6", 6}, {"7", 7}, {"8", 8}, {"9", 9}, {"10", 10},
            {"J", 11}, {"Q", 12}, {"K", 13}
        };

        public Card(string rank, string suit)
        {
            ValidateCard(rank, suit);
            
            Rank = rank;
            Suit = suit;
            Color = (suit == "♥" || suit == "♦") ? Color.red : Color.black;
            Value = CalculateValue(rank);
        }

        private void ValidateCard(string rank, string suit)
        {
            if (string.IsNullOrEmpty(rank))
                throw new GameValidationException("Card rank cannot be null or empty");
            
            if (string.IsNullOrEmpty(suit))
                throw new GameValidationException("Card suit cannot be null or empty");

            if (!ValidRanks.Contains(rank))
                throw new GameValidationException($"Invalid card rank: {rank}");
            
            if (!ValidSuits.Contains(suit))
                throw new GameValidationException($"Invalid card suit: {suit}");

            if (!RankValues.ContainsKey(rank))
                throw new GameValidationException($"No value defined for rank: {rank}");
            
            if (suit.Length != 1)
                throw new GameValidationException($"Suit must be a single character: {suit}");
        }

        private int CalculateValue(string rank)
        {
            switch (rank)
            {
                case "Jack": return 11;
                case "Queen": return 12;
                case "King": return 13;
                case "Ace": return 14;
                default: return int.Parse(rank);
            }
        }
    }
} 