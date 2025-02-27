using UnityEngine;
using System.Collections.Generic;
using CardGame.Exceptions;

namespace CardGame.Core
{
    public interface IDeck
    {
        int RemainingCards { get; }
        bool IsEmpty();
        void Shuffle();
        ICard DrawCard();
        void Initialize();
    }

    public class Deck : MonoBehaviour, IDeck
    {
        private List<ICard> cards;
        private static readonly string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
        private static readonly string[] suits = { "♥", "♦", "♣", "♠" };

        public int RemainingCards => cards?.Count ?? 0;

        public bool IsEmpty() => cards == null || cards.Count == 0;

        public void Initialize()
        {
            ValidateInitialization();
            InitializeDeck();
            Shuffle();
        }

        private void ValidateInitialization()
        {
            if (cards != null)
                throw new GameValidationException("Deck already initialized");
            
            cards = new List<ICard>();
        }

        private void InitializeDeck()
        {
            foreach (string suit in suits)
            {
                foreach (string rank in ranks)
                {
                    ICard card = new Card(rank, suit);
                    cards.Add(card);
                }
            }
        }

        public void Shuffle()
        {
            ValidateShuffle();

            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]); // Tuple swap for clarity
            }
        }

        private void ValidateShuffle()
        {
            if (cards == null)
                throw new GameValidationException("Cannot shuffle uninitialized deck");
            if (cards.Count == 0)
                throw new GameValidationException("Cannot shuffle empty deck");
        }

        public ICard DrawCard()
        {
            ValidateDrawCard();
            ICard card = cards[cards.Count - 1];
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

        private void OnDestroy()
        {
            cards?.Clear();
            cards = null;
        }
    }
}