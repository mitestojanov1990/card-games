using UnityEngine;
using System.Collections.Generic;
using CardGame.Exceptions;
using CardGame.Core;
using CardGame.Core.Interfaces;

namespace CardGame.Core
{
    public class Deck : IDeck
    {
        private List<ICard> cards = new List<ICard>();
        private static string[] ranks = { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };
        private static string[] suits = { "♥", "♦", "♣", "♠" };

        public int RemainingCards => cards?.Count ?? 0;

        public bool IsEmpty() => cards.Count == 0;

        public Deck()
        {
            ValidateInitialization();
            InitializeDeck();
        }

        private void ValidateInitialization()
        {
            if (cards != null)
                throw new GameValidationException("Deck already initialized");
        }

        private void InitializeDeck()
        {
            foreach (string suit in suits)
            {
                foreach (string rank in ranks)
                {
                    cards.Add(new Card(rank, suit));
                }
            }
        }

        public void Shuffle()
        {
            ValidateShuffle();
            
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                ICard temp = cards[i];
                cards[i] = cards[j];
                cards[j] = temp;
            }
        }

        private void ValidateShuffle()
        {
            if (cards == null)
                throw new GameValidationException("Cannot shuffle uninitialized deck");
            if (cards.Count == 0)
                throw new GameValidationException("Cannot shuffle empty deck");
        }

        ICard IDeck.DrawCard()
        {
            ValidateDrawCard();
            return DrawCard();
        }

        public Card DrawCard()
        {
            ValidateDrawCard();
            Card card = cards[cards.Count - 1] as Card;
            cards.RemoveAt(cards.Count - 1);
            return card;
        }

        private void ValidateDrawCard()
        {
            if (cards == null)
                throw new GameValidationException("Deck not initialized");
            if (cards.Count == 0)
                throw new GameValidationException("Cannot draw from empty deck");
        }
    }
} 