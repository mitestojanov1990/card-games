using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using CardGame.Core;
using CardGame.Rules;
using CardGame.Exceptions;
using CardGame.DI;
using CardGame.Utils;

namespace CardGame.Players
{
    public interface IPlayer
    {
        string Name { get; }
        bool IsHuman { get; }
        IReadOnlyList<ICard> Hand { get; }
        int Score { get; set; }
        void AddCard(ICard card);
        void RemoveCard(ICard card);
        void CheckMacau();
        void CheckStopMacau(IPlayer target);
        ICard GetBestPlay(ICard topCard, string declaredSuit, bool isSequentialPlay);
        string GetMostCommonSuit();
    }
    public class Player : IPlayer
    {
        private readonly ICardRules cardRules;
        private List<ICard> hand = new List<ICard>();
        public IReadOnlyList<ICard> Hand => hand;
        public string Name { get; }
        public bool IsHuman { get; }
        public int Score { get; set; }

        // CPU player strategy fields
        private bool hasDeclaredMacau = false;
        private const float MACAU_CALL_CHANCE = 0.8f; // 80% chance CPU remembers to call Macau
        private const float STOP_MACAU_CHANCE = 0.7f; // 70% chance CPU catches missing Macau

        public Player(string name, bool isHuman, ICardRules cardRules)
        {
            ValidatePlayer(name);
            Name = name;
            IsHuman = isHuman;
            this.cardRules = cardRules;
            hand = new List<ICard>();
            Score = 0;
        }

        private void ValidatePlayer(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new GameValidationException("Player name cannot be null or empty");
            if (name.Length < 2)
                throw new GameValidationException("Player name too short");
            if (name.Length > 20)
                throw new GameValidationException("Player name too long");
            if (name.Any(c => !char.IsLetterOrDigit(c) && !char.IsWhiteSpace(c)))
                throw new GameValidationException("Player name contains invalid characters");
        }

        private void ValidateHandSize()
        {
            if (hand.Count > 20)
                throw new GameValidationException("Hand size exceeds maximum limit");
        }

        public void AddCard(ICard card)
        {
            ValidateAddCard(card);
            ValidateHandSize();
            hand.Add(card);
            // Reset Macau state when getting new cards
            if (hand.Count > 1)
            {
                hasDeclaredMacau = false;
            }
        }

        private void ValidateAddCard(ICard card)
        {
            if (card == null)
                throw new GameValidationException("Cannot add null card to hand");
        }

        public void RemoveCard(ICard card)
        {
            ValidateRemoveCard(card);
            hand.Remove(card);
        }

        private void ValidateRemoveCard(ICard card)
        {
            if (card == null)
                throw new GameValidationException("Cannot remove null card from hand");
            if (!hand.Contains(card))
                throw new GameValidationException("Card not in player's hand");
        }

        public void CheckMacau()
        {
            if (!IsHuman && hand.Count == 1 && !hasDeclaredMacau)
            {
                // CPU players have a chance to forget calling Macau
                if (Random.value < MACAU_CALL_CHANCE)
                {
                    hasDeclaredMacau = true;
                    GameContainer.Instance.GameManager.CallMacau(this);
                }
            }
        }

        public void CheckStopMacau(IPlayer target)
        {
            if (!IsHuman && target.Hand.Count == 1)
            {
                // CPU players have a chance to catch missing Macau
                if (Random.value < STOP_MACAU_CHANCE)
                {
                    GameManager.Instance.CallStopMacau(this, (Player)target);
                }
            }
        }

        public ICard GetBestPlay(ICard topCard, string declaredSuit, bool isSequentialPlay)
        {
            ValidatePlayConditions(topCard);
            if (IsHuman) return null; // Human players choose their own cards

            List<ICard> validPlays = new List<ICard>();
            foreach (var card in hand)
            {
                if (cardRules.CanPlayOnTop(card, topCard, isSequentialPlay, declaredSuit))
                {
                    validPlays.Add(card);
                }
            }

            if (validPlays.Count == 0) return null;

            // Prioritize plays based on strategy
            ICard bestPlay = null;
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

        private void ValidatePlayConditions(ICard topCard)
        {
            if (hand.Count == 0)
                throw new GameValidationException("Cannot play from empty hand");
            if (topCard == null && !IsFirstPlay())
                throw new GameValidationException("Top card cannot be null except for first play");
        }

        private bool IsFirstPlay()
        {
            return GameManager.Instance.TurnCount == 1;
        }

        private int EvaluatePlay(ICard card, ICard topCard, int handSize)
        {
            int score = 0;

            // Prioritize special cards when having many cards
            if (handSize > 3)
            {
                switch (cardRules.GetCardEffect(card))
                {
                    case SpecialEffect.DrawTwo:
                    case SpecialEffect.DrawThree:
                        score += 3;
                        break;
                    case SpecialEffect.PopCup:
                        score += 4;
                        break;
                    case SpecialEffect.SkipTurn:
                        score += 2;
                        break;
                    case SpecialEffect.Sequential:
                        if (HasSequentialPlay(card))
                            score += 5;
                        break;
                }
            }
            // Prioritize normal cards when close to winning
            else
            {
                if (cardRules.GetCardEffect(card) == SpecialEffect.None)
                    score += 2;
            }

            // Prefer keeping special cards for later if possible
            if (handSize > 1 && HasBetterSpecialCard(card))
                score -= 1;

            return score;
        }

        private bool HasSequentialPlay(ICard initialCard)
        {
            // Check if we have cards that could be played sequentially
            return hand.Any(c => c != initialCard &&
                               (c.Suit == initialCard.Suit || c.Rank == initialCard.Rank));
        }

        private bool HasBetterSpecialCard(ICard card)
        {
            var currentEffect = cardRules.GetCardEffect(card);
            if (currentEffect == SpecialEffect.None) return false;

            return hand.Any(c => c != card &&
                               cardRules.GetCardEffect(c) != SpecialEffect.None &&
                               IsEffectBetter(cardRules.GetCardEffect(c), currentEffect));
        }

        private bool IsEffectBetter(SpecialEffect effect1, SpecialEffect effect2)
        {
            // Define effect hierarchy
            Dictionary<SpecialEffect, int> effectValue = new Dictionary<SpecialEffect, int>
            {
                { SpecialEffect.None, 0 },
                { SpecialEffect.Sequential, 1 },
                { SpecialEffect.ChangeSuit, 2 },
                { SpecialEffect.SkipTurn, 3 },
                { SpecialEffect.DrawTwo, 4 },
                { SpecialEffect.DrawThree, 5 },
                { SpecialEffect.PopCup, 6 }
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
}