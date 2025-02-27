using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CardGame.Players;
using CardGame.Rules;
using CardGame.UI;
using CardGame.Utils;
using CardGame.Stats;
using CardGame.Exceptions;
using CardGame.Core.Interfaces;
using CardGame.Rules.Interfaces;
using CardGame.UI.Interfaces;

namespace CardGame.Core
{
    public class GameManager : MonoBehaviour, IGameManager
    {
        private IDeck deck;
        private ICardRules cardRules;
        private IUIManager uiManager;
        private List<IPlayer> players = new List<IPlayer>();
        private ICard topDiscard;
        public IPlayer CurrentPlayer => players[currentPlayerIndex];
        
        public void Initialize(IDeck deck, ICardRules cardRules, IUIManager uiManager)
        {
            this.deck = deck;
            this.cardRules = cardRules;
            this.uiManager = uiManager;
        }

        // ... implement other interface properties and methods ...
    }
} 