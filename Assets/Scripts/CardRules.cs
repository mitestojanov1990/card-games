using UnityEngine;

public static class CardRules
{
    public enum SpecialEffect
    {
        None,
        DrawCards,
        SkipTurn,
        ChangeSuit,
        Sequential
    }

    public static SpecialEffect GetCardEffect(Card card)
    {
        switch (card.Rank)
        {
            case "2":
            case "3":
            case "Pop Cup": // Special ♥ card
                return SpecialEffect.DrawCards;
            case "Ace":
                return SpecialEffect.SkipTurn;
            case "Jack":
                return SpecialEffect.ChangeSuit;
            case "9":
                return SpecialEffect.Sequential;
            default:
                return SpecialEffect.None;
        }
    }

    public static int GetDrawAmount(Card card, Card previousCard = null)
    {
        switch (card.Rank)
        {
            case "2":
                return 2;
            case "3":
                return 3;
            case "Pop Cup":
                return 5;
            default:
                return 0;
        }
    }

    public static bool CanPlayOnTop(Card playedCard, Card topCard, bool isSequentialPlay = false, string declaredSuit = null)
    {
        if (topCard == null) return true;

        // Handle Jack special case
        if (topCard.Rank == "Jack" && declaredSuit != null)
        {
            return playedCard.Suit == declaredSuit;
        }

        // Handle sequential play after 9
        if (isSequentialPlay)
        {
            return playedCard.Suit == topCard.Suit;
        }

        // Normal play - match suit or rank
        return playedCard.Suit == topCard.Suit || playedCard.Rank == topCard.Rank;
    }

    public static bool CanCounterDrawCards(Card counterCard, Card activeCard)
    {
        if (activeCard.Rank != "2" && activeCard.Rank != "3") return false;

        return (counterCard.Rank == "2" || counterCard.Rank == "3") && 
               counterCard.Suit == activeCard.Suit;
    }

    public static bool IsPopCupCounter(Card card, Card previousCard)
    {
        return previousCard.Rank == "King" && previousCard.Suit == "♥" &&
               card.Rank == "Queen" && card.Suit == "♥";
    }
} 