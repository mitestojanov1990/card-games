using System;

namespace CardGame.Exceptions
{
    public class GameValidationException : Exception
    {
        public GameValidationException(string message) : base(message) { }
    }
} 