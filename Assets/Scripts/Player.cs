using System.Collections.Generic;
using UnityEngine;

public class Player
{
    public List<Card> Hand { get; private set; } = new List<Card>();
    public bool IsHuman { get; private set; }
    public string Name { get; private set; }
    public bool HasCalledMacau { get; set; }

    public Player(string name, bool isHuman)
    {
        Name = name;
        IsHuman = isHuman;
        HasCalledMacau = false;
    }

    public void AddCard(Card card)
    {
        Hand.Add(card);
    }

    public void RemoveCard(Card card)
    {
        Hand.Remove(card);
    }

    public Card GetBestPlay(Card topCard, string declaredSuit, bool isSequentialPlay)
    {
        if (!IsHuman)
        {
            // CPU logic for choosing a card
            foreach (Card card in Hand)
            {
                if (CardRules.CanPlayOnTop(card, topCard, isSequentialPlay, declaredSuit))
                {
                    // Prioritize special cards
                    var effect = CardRules.GetCardEffect(card);
                    if (effect != CardRules.SpecialEffect.None)
                    {
                        return card;
                    }
                }
            }

            // If no special cards, play first valid card
            foreach (Card card in Hand)
            {
                if (CardRules.CanPlayOnTop(card, topCard, isSequentialPlay, declaredSuit))
                {
                    return card;
                }
            }
        }

        return null; // No valid play found
    }

    public void CheckMacau()
    {
        if (!IsHuman && Hand.Count == 1 && !HasCalledMacau)
        {
            HasCalledMacau = true;
            Debug.Log($"{Name} called Macau!");
        }
    }
} 