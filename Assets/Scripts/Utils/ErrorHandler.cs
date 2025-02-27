using UnityEngine;
using System;
using System.Collections.Generic;
using CardGame.Exceptions;

namespace CardGame.Utils
{
    public class ErrorHandler : MonoBehaviour
    {
        private static ErrorHandler instance;
        public static ErrorHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    var obj = new GameObject("ErrorHandler");
                    instance = obj.AddComponent<ErrorHandler>();
                    DontDestroyOnLoad(obj);
                }
                return instance;
            }
        }

        public static event Action<string> OnError;
        public static event Action<string> OnWarning;

        private Queue<string> errorQueue = new Queue<string>();
        private const int MAX_QUEUED_ERRORS = 10;
        private bool isProcessingErrors = false;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public static void HandleException(Exception ex, string context = "")
        {
            if (Instance != null)
            {
                Instance.ProcessException(ex, context);
            }
        }

        private void ProcessException(Exception ex, string context)
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
                // Queue unexpected errors
                QueueError($"{message}\nStackTrace: {ex.StackTrace}");
            }
        }

        private void QueueError(string error)
        {
            if (errorQueue.Count >= MAX_QUEUED_ERRORS)
            {
                errorQueue.Dequeue(); // Remove oldest error
            }
            errorQueue.Enqueue(error);

            if (!isProcessingErrors)
            {
                ProcessErrorQueue();
            }
        }

        private void ProcessErrorQueue()
        {
            isProcessingErrors = true;
            while (errorQueue.Count > 0)
            {
                string error = errorQueue.Dequeue();
                Debug.LogError(error);
                OnError?.Invoke("An unexpected error occurred. Please try again.");
            }
            isProcessingErrors = false;
        }

        public static void LogWarning(string message)
        {
            if (Instance != null)
            {
                Debug.LogWarning(message);
                OnWarning?.Invoke(message);
            }
        }

        public static void LogError(string message)
        {
            if (Instance != null)
            {
                Instance.QueueError(message);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
} 