using UnityEngine;

namespace CardGame.UI.Interfaces
{
    public interface IUIManager
    {
        void ShowStartScreen();
        void UpdateGameUI();
        void ShowGameOverScreen();
        void SetUIScale(float scale);
        void ToggleDebugInfo();
    }
} 