using UnityEngine;
using CardGame.Exceptions;
using CardGame.Core;

namespace CardGame.Rules
{
    public static class CardRules
    {
        public enum SpecialEffect
        {
            None,
            DrawTwo,
            DrawThree,
            PopCup,
            SkipTurn,
            ChangeSuit,
            Sequential
        }

        public static bool CanPlayOnTop(Card card, Card topCard, bool isSequential, string declaredSuit)
        {
            ValidatePlayRules(card, topCard, declaredSuit);

            if (topCard == null) return true;

            // Handle declared suit from Jack
            if (declaredSuit != null)
            {
                return card.Suit == declaredSuit;
            }

            // Handle sequential play (after playing 9)
            if (isSequential)
            {
                return card.Suit == topCard.Suit || card.Rank == topCard.Rank;
            }

            // Handle special counter cases
            if (IsDrawCardCounter(card, topCard))
            {
                return true;
            }

            // Handle Pop Cup counter
            if (IsPopCupCounter(card, topCard))
            {
                return true;
            }

            // Normal play - match suit or rank
            return card.Suit == topCard.Suit || card.Rank == topCard.Rank;
        }

        private static void ValidatePlayRules(Card card, Card topCard, string declaredSuit)
        {
            if (card == null)
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

        public static bool IsDrawCardCounter(Card playedCard, Card topCard)
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

        public static bool IsPopCupCounter(Card card, Card previousCard)
        {
            return previousCard.Rank == "King" && previousCard.Suit == "♥" &&
                   card.Rank == "Queen" && card.Suit == "♥";
        }

        public static SpecialEffect GetCardEffect(Card card)
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

        public static int GetDrawAmount(Card card)
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

        public static bool RequiresSuitDeclaration(Card card)
        {
            return card.Rank == "Jack";
        }

        public static bool AllowsExtraTurn(Card card, int playerCount)
        {
            return card.Rank == "Ace" && playerCount == 2;
        }
    }
} 