using System.Collections.Generic;
using UnityEngine;

public class Deck
{
    private List<Card> cards = new List<Card>();
    private static string[] ranks = { "Ace", "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King" };
    private static string[] suits = { "♥", "♦", "♣", "♠" };

    public int RemainingCards => cards.Count;

    public bool IsEmpty() => cards.Count == 0;

    public Deck()
    {
        InitializeDeck();
        Shuffle();
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
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = cards[i];
            cards[i] = cards[j];
            cards[j] = temp;
        }
    }

    public Card DrawCard()
    {
        if (cards.Count == 0) return null;
        
        Card card = cards[0];
        cards.RemoveAt(0);
        return card;
    }
} 