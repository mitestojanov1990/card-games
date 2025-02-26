using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Player
{
    public string Name { get; private set; }
    public bool IsHuman { get; private set; }
    private List<Card> hand = new List<Card>();
    public IReadOnlyList<Card> Hand => hand;

    // CPU player strategy fields
    private bool hasDeclaredMacau = false;
    private const float MACAU_CALL_CHANCE = 0.8f; // 80% chance CPU remembers to call Macau
    private const float STOP_MACAU_CHANCE = 0.7f; // 70% chance CPU catches missing Macau

    public Player(string name, bool isHuman)
    {
        Name = name;
        IsHuman = isHuman;
        hand = new List<Card>();
    }

    public void AddCard(Card card)
    {
        if (card != null)
        {
            hand.Add(card);
            // Reset Macau state when getting new cards
            if (hand.Count > 1)
            {
                hasDeclaredMacau = false;
            }
        }
    }

    public void RemoveCard(Card card)
    {
        hand.Remove(card);
    }

    public void CheckMacau()
    {
        if (!IsHuman && hand.Count == 1 && !hasDeclaredMacau)
        {
            // CPU players have a chance to forget calling Macau
            if (Random.value < MACAU_CALL_CHANCE)
            {
                hasDeclaredMacau = true;
                GameManager.Instance.CallMacau(this);
            }
        }
    }

    public void CheckStopMacau(Player target)
    {
        if (!IsHuman && target.Hand.Count == 1 && !target.hasDeclaredMacau)
        {
            // CPU players have a chance to catch missing Macau
            if (Random.value < STOP_MACAU_CHANCE)
            {
                GameManager.Instance.CallStopMacau(this, target);
            }
        }
    }

    public Card GetBestPlay(Card topCard, string declaredSuit, bool isSequentialPlay)
    {
        if (IsHuman) return null; // Human players choose their own cards

        List<Card> validPlays = new List<Card>();
        foreach (var card in hand)
        {
            if (CardRules.CanPlayOnTop(card, topCard, isSequentialPlay, declaredSuit))
            {
                validPlays.Add(card);
            }
        }

        if (validPlays.Count == 0) return null;

        // Prioritize plays based on strategy
        Card bestPlay = null;
        int bestScore = -1;

        foreach (var card in validPlays)
        {
            int score = EvaluatePlay(card, topCard, hand.Count);
            if (score > bestScore)
            {
                bestScore = score;
                bestPlay = card;
            }
        }

        return bestPlay;
    }

    private int EvaluatePlay(Card card, Card topCard, int handSize)
    {
        int score = 0;

        // Prioritize special cards when having many cards
        if (handSize > 3)
        {
            switch (CardRules.GetCardEffect(card))
            {
                case CardRules.SpecialEffect.DrawTwo:
                case CardRules.SpecialEffect.DrawThree:
                    score += 3;
                    break;
                case CardRules.SpecialEffect.PopCup:
                    score += 4;
                    break;
                case CardRules.SpecialEffect.SkipTurn:
                    score += 2;
                    break;
                case CardRules.SpecialEffect.Sequential:
                    if (HasSequentialPlay(card))
                        score += 5;
                    break;
            }
        }
        // Prioritize normal cards when close to winning
        else
        {
            if (CardRules.GetCardEffect(card) == CardRules.SpecialEffect.None)
                score += 2;
        }

        // Prefer keeping special cards for later if possible
        if (handSize > 1 && HasBetterSpecialCard(card))
            score -= 1;

        return score;
    }

    private bool HasSequentialPlay(Card initialCard)
    {
        // Check if we have cards that could be played sequentially
        return hand.Any(c => c != initialCard && 
                           (c.Suit == initialCard.Suit || c.Rank == initialCard.Rank));
    }

    private bool HasBetterSpecialCard(Card card)
    {
        var currentEffect = CardRules.GetCardEffect(card);
        if (currentEffect == CardRules.SpecialEffect.None) return false;

        return hand.Any(c => c != card && 
                           CardRules.GetCardEffect(c) != CardRules.SpecialEffect.None &&
                           IsEffectBetter(CardRules.GetCardEffect(c), currentEffect));
    }

    private bool IsEffectBetter(CardRules.SpecialEffect effect1, CardRules.SpecialEffect effect2)
    {
        // Define effect hierarchy
        Dictionary<CardRules.SpecialEffect, int> effectValue = new Dictionary<CardRules.SpecialEffect, int>
        {
            { CardRules.SpecialEffect.None, 0 },
            { CardRules.SpecialEffect.Sequential, 1 },
            { CardRules.SpecialEffect.ChangeSuit, 2 },
            { CardRules.SpecialEffect.SkipTurn, 3 },
            { CardRules.SpecialEffect.DrawTwo, 4 },
            { CardRules.SpecialEffect.DrawThree, 5 },
            { CardRules.SpecialEffect.PopCup, 6 }
        };

        return effectValue[effect1] > effectValue[effect2];
    }

    public string GetMostCommonSuit()
    {
        return hand.GroupBy(c => c.Suit)
                  .OrderByDescending(g => g.Count())
                  .First()
                  .Key;
    }
} 