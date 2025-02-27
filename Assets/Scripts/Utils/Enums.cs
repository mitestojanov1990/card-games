namespace CardGame.Utils
{
    public enum GameState
    {
        WaitingToStart,
        PlayerTurn,
        GameOver
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