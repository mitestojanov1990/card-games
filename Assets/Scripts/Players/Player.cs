using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CardGame.Core;
using CardGame.Rules;
using CardGame.Exceptions;
using CardGame.Players.Interfaces;
using CardGame.Core.Interfaces;
using CardGame.Rules.Interfaces;

namespace CardGame.Players
{
    public class Player : IPlayer
    {
        private readonly ICardRules cardRules;
        private List<ICard> hand = new List<ICard>();
        public IReadOnlyList<ICard> Hand => hand;

        public Player(string name, bool isHuman, ICardRules cardRules)
        {
            Name = name;
            IsHuman = isHuman;
            this.cardRules = cardRules;
        }

        public void AddCard(ICard card)
        {
            ValidateAddCard(card);
            ValidateHandSize();
            hand.Add(card);
            if (hand.Count > 1)
            {
                hasDeclaredMacau = false;
            }
        }

        public void RemoveCard(ICard card)
        {
            ValidateRemoveCard(card);
            hand.Remove(card);
        }

        public ICard GetBestPlay(ICard topCard, string declaredSuit, bool isSequentialPlay)
        {
            foreach (var card in hand)
            {
                if (cardRules.CanPlayOnTop(card, topCard, isSequentialPlay, declaredSuit))
                {
                    return card;
                }
            }
            return null;
        }
    }
} 