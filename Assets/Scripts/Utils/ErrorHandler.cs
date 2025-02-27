using UnityEngine;
using System;
using CardGame.Exceptions;

namespace CardGame.Utils
{
    public static class ErrorHandler
    {
        public static event Action<string> OnError;
        public static event Action<string> OnWarning;

        public static void HandleException(Exception ex, string context = "")
        {
            string message = string.IsNullOrEmpty(context) 
                ? ex.Message 
                : $"{context}: {ex.Message}";

            if (ex is GameValidationException)
            {
                // Validation errors are expected, show them to the user
                Debug.LogWarning(message);
                OnWarning?.Invoke(message);
            }
            else
            {
                // Unexpected errors, log full details
                Debug.LogError($"{message}\nStackTrace: {ex.StackTrace}");
                OnError?.Invoke("An unexpected error occurred. Please try again.");
            }
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning(message);
            OnWarning?.Invoke(message);
        }

        public static void LogError(string message)
        {
            Debug.LogError(message);
            OnError?.Invoke(message);
        }
    }
} 