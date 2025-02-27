using UnityEngine;
using CardGame.Core;
using CardGame.Core.Interfaces;
using CardGame.Rules;
using CardGame.Rules.Interfaces;
using CardGame.UI;
using CardGame.UI.Interfaces;

namespace CardGame.DI
{
    public class GameContainer : MonoBehaviour
    {
        private static GameContainer instance;
        public static GameContainer Instance => instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeContainer();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private IDeck deck;
        private ICardRules cardRules;
        private IUIManager uiManager;
        private IGameManager gameManager;

        public IDeck Deck => deck;
        public ICardRules CardRules => cardRules;
        public IUIManager UIManager => uiManager;
        public IGameManager GameManager => gameManager;

        private void InitializeContainer()
        {
            // Initialize core systems
            cardRules = CardGame.Rules.CardRules.Instance;
            deck = new Deck();

            // Initialize Unity components
            var uiManagerObj = new GameObject("UIManager");
            uiManagerObj.transform.SetParent(transform);
            uiManager = uiManagerObj.AddComponent<UIManager>() as IUIManager;

            var gameManagerObj = new GameObject("GameManager");
            gameManagerObj.transform.SetParent(transform);
            gameManager = gameManagerObj.AddComponent<GameManager>();

            // Inject dependencies
            InjectDependencies();
        }

        private void InjectDependencies()
        {
            if (gameManager is GameManager gm)
            {
                gm.Initialize(deck, cardRules, uiManager);
            }
        }
    }
}