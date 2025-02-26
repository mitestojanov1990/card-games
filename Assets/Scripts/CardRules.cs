using UnityEngine;

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

    public static bool CanPlayOnTop(Card playedCard, Card topCard, bool isSequentialPlay = false, string declaredSuit = null)
    {
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