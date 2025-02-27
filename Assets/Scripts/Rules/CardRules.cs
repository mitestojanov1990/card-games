using UnityEngine;
using CardGame.Core;
using CardGame.Exceptions;
using CardGame.Utils;
using System.Linq;

namespace CardGame.Rules
{
    public interface ICardRules
    {
        bool CanPlayOnTop(ICard playedCard, ICard topCard, bool isSequentialPlay, string declaredSuit);
        bool IsDrawCardCounter(ICard playedCard, ICard topCard);
        bool IsPopCupCounter(ICard card, ICard previousCard);
        SpecialEffect GetCardEffect(ICard card);
        int GetDrawAmount(ICard card);
        bool RequiresSuitDeclaration(ICard card);
        bool AllowsExtraTurn(ICard card, int playerCount);
    }
    public class CardRules : ICardRules
    {
        private static readonly CardRules instance = new CardRules();
        public static CardRules Instance => instance;
        private CardRules() { }

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

        public bool IsPopCupCounter(ICard card, ICard previousCard)
        {
            return previousCard.Rank == "King" && previousCard.Suit == "♥" &&
                   card.Rank == "Queen" && card.Suit == "♥";
        }

        public SpecialEffect GetCardEffect(ICard card)
        {
            switch (card.Rank)
            {
                case "2":
                    return SpecialEffect.DrawTwo;
                case "3":
                    return SpecialEffect.DrawThree;
                case "9":
                    return SpecialEffect.Sequential;
                case "Jack":
                    return SpecialEffect.ChangeSuit;
                case "King":
                    return card.Suit == "♥" ? SpecialEffect.PopCup : SpecialEffect.None;
                case "Ace":
                    return SpecialEffect.SkipTurn;
                default:
                    return SpecialEffect.None;
            }
        }

        public int GetDrawAmount(ICard card)
        {
            if (card == null)
                throw new GameValidationException("Cannot get draw amount for null card");

            switch (card.Rank)
            {
                case "2":
                    return 2;
                case "3":
                    return 3;
                case "King":
                    return card.Suit == "♥" ? 5 : 0;
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