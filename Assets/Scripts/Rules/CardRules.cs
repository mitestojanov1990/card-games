using UnityEngine;
using CardGame.Core;
using CardGame.Exceptions;
using CardGame.Utils;
using System.Linq;
using System.Collections.Generic;

namespace CardGame.Rules
{
    public interface ICardRules
    {
        bool CanPlayOnTop(ICard playedCard, ICard topCard, bool isSequentialPlay, string declaredSuit);
        bool IsDrawCardCounter(ICard playedCard, ICard topCard);
        bool IsPopCupCounter(ICard card, ICard topCard);
        SpecialEffect GetCardEffect(ICard card);
        int GetDrawAmount(ICard card);
        bool RequiresSuitDeclaration(ICard card);
        bool AllowsExtraTurn(ICard card, int playerCount);
        bool IsValidPlay(ICard card, ICard topCard, string declaredSuit, bool isSequentialPlay);
    }

    public class CardRules : ICardRules
    {
        private static CardRules instance;
        public static CardRules Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CardRules();
                }
                return instance;
            }
        }

        private readonly Dictionary<string, SpecialEffect> specialCards = new Dictionary<string, SpecialEffect>
        {
            {"2", SpecialEffect.DrawTwo},
            {"3", SpecialEffect.DrawThree},
            {"K", SpecialEffect.PopCup},
            {"Q", SpecialEffect.PopCup},  // Queen counters King
            {"J", SpecialEffect.SkipTurn},
            {"A", SpecialEffect.ChangeSuit},
            {"7", SpecialEffect.Sequential}
        };

        public bool CanPlayOnTop(ICard playedCard, ICard topCard, bool isSequentialPlay, string declaredSuit)
        {
            ValidatePlayRules(playedCard, topCard, declaredSuit);

            if (topCard == null) return true;

            // Handle declared suit from Jack
            if (declaredSuit != null)
            {
                return playedCard.Suit == declaredSuit;
            }

            // Handle sequential play (after playing 9)
            if (isSequentialPlay)
            {
                return playedCard.Suit == topCard.Suit || playedCard.Rank == topCard.Rank;
            }

            // Handle special counter cases
            if (IsDrawCardCounter(playedCard, topCard))
            {
                return true;
            }

            // Handle Pop Cup counter
            if (IsPopCupCounter(playedCard, topCard))
            {
                return true;
            }

            // Normal play - match suit or rank
            return playedCard.Suit == topCard.Suit || playedCard.Rank == topCard.Rank;
        }

        public bool IsDrawCardCounter(ICard playedCard, ICard topCard)
        {
            // Can counter a 2 or 3 with another 2 or 3 of the same suit
            if ((topCard.Rank == "2" || topCard.Rank == "3") &&
                (playedCard.Rank == "2" || playedCard.Rank == "3") &&
                playedCard.Suit == topCard.Suit)
            {
                return true;
            }
            return false;
        }

        public bool IsPopCupCounter(ICard card, ICard topCard)
        {
            if (card == null || topCard == null) return false;

            // Queen counters King's pop cup effect
            return topCard.Rank == "K" && card.Rank == "Q";
        }

        public SpecialEffect GetCardEffect(ICard card)
        {
            if (card == null) return SpecialEffect.None;
            return specialCards.TryGetValue(card.Rank, out SpecialEffect effect) ? effect : SpecialEffect.None;
        }

        public int GetDrawAmount(ICard card)
        {
            if (card == null) return 0;
            
            var effect = GetCardEffect(card);
            switch (effect)
            {
                case SpecialEffect.DrawTwo:
                    return 2;
                case SpecialEffect.DrawThree:
                    return 3;
                default:
                    return 0;
            }
        }

        public bool RequiresSuitDeclaration(ICard card)
        {
            return card.Rank == "Jack";
        }

        public bool AllowsExtraTurn(ICard card, int playerCount)
        {
            return card.Rank == "Ace" && playerCount == 2;
        }

        public bool IsValidPlay(ICard card, ICard topCard, string declaredSuit, bool isSequentialPlay)
        {
            if (card == null || topCard == null) return false;

            // Check if it's a sequential play (must match rank)
            if (isSequentialPlay)
            {
                return card.Rank == topCard.Rank;
            }

            // Check if suit matches declared suit
            if (!string.IsNullOrEmpty(declaredSuit))
            {
                return card.Suit == declaredSuit || card.Rank == topCard.Rank;
            }

            // Normal play - match suit or rank
            return card.Suit == topCard.Suit || card.Rank == topCard.Rank;
        }

        private void ValidatePlayRules(ICard playedCard, ICard topCard, string declaredSuit)
        {
            if (playedCard == null)
                throw new GameValidationException("Cannot validate null card");

            if (declaredSuit != null && !ValidSuits.Contains(declaredSuit))
                throw new GameValidationException($"Invalid declared suit: {declaredSuit}");

            if (topCard != null)
            {
                if (!ValidRanks.Contains(topCard.Rank))
                    throw new GameValidationException($"Invalid top card rank: {topCard.Rank}");
                if (!ValidSuits.Contains(topCard.Suit))
                    throw new GameValidationException($"Invalid top card suit: {topCard.Suit}");
            }
        }

        private static readonly string[] ValidRanks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
        private static readonly string[] ValidSuits = { "♥", "♦", "♣", "♠" };
    }
}