using UnityEngine;
using CardGame.Core;
using CardGame.Exceptions;
using CardGame.Rules.Interfaces;

namespace CardGame.Rules
{
    public class CardRules : ICardRules
    {
        private static readonly CardRules instance = new CardRules();
        public static CardRules Instance => instance;
        private CardRules() { }

        public bool CanPlayOnTop(ICard playedCard, ICard topCard, bool isSequentialPlay, string declaredSuit)
        {
            // ... existing code using ICard ...
        }

        public bool IsDrawCardCounter(ICard playedCard, ICard topCard)
        {
            // ... existing code using ICard ...
        }

        // ... implement other interface methods ...
    }
} 