using CardGame.Core.Interfaces;

namespace CardGame.Rules.Interfaces
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
} 