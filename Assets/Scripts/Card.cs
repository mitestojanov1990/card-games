using UnityEngine;

[System.Serializable]
public class Card
{
    public string Rank { get; private set; }
    public string Suit { get; private set; }
    public Color Color { get; private set; }
    public int Value { get; private set; }  // Numerical value for game logic

    public Card(string rank, string suit)
    {
        Rank = rank;
        Suit = suit;
        Color = (suit == "♥" || suit == "♦") ? Color.red : Color.black;
        Value = CalculateValue(rank);
    }

    private int CalculateValue(string rank)
    {
        switch (rank)
        {
            case "Jack": return 11;
            case "Queen": return 12;
            case "King": return 13;
            case "Ace": return 14;
            default: return int.Parse(rank);
        }
    }
} 