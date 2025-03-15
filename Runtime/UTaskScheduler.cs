using System;
using System.Collections.Generic;
using UnityEngine;

namespace THEBADDEST.Tasks
{
    /// <summary>
    /// A scheduler for managing Unity tasks and coroutines efficiently.
    /// Handles both one-time actions and per-frame actions with proper cleanup.
    /// </summary>
    public class UTaskScheduler : MonoBehaviour
    {
        private static readonly Queue<Action> mainThreadActions = new Queue<Action>();
        private static readonly HashSet<Action> perFrameActions = new HashSet<Action>();
        private static readonly object lockObject = new object();
        private static bool isProcessing;
        private static int maxActionsPerFrame = 100; // Prevent too many actions in one frame
        private static bool isInitialized;
        private static bool isQuitting;
        private static int totalProcessedActions;
        private static int totalProcessedPerFrameActions;
        private static readonly List<Action> tempActionsList = new List<Action>();

        /// <summary>
        /// Initializes the UTaskScheduler if it hasn't been initialized yet.
        /// Creates a hidden GameObject with the scheduler component.
        /// </summary>
        private static void Initialize()
        {
            if (isInitialized) return;

            try
            {
                var go = new GameObject("UTaskScheduler") { hideFlags = HideFlags.HideInHierarchy };
                var scheduler = go.AddComponent<UTaskScheduler>();
                DontDestroyOnLoad(go);
                isInitialized = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to initialize UTaskScheduler: {e}");
                isInitialized = false;
                throw;
            }
        }

        private void OnApplicationQuit()
        {
            isInitialized = false;
            isQuitting = true;
            ClearPerFrameActions();
        }

        private void OnDisable()
        {
            if (isInitialized)
            {
                ClearPerFrameActions();
            }
        }

        /// <summary>
        /// Schedules a one-time action to be executed on the main thread.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
        public static void Schedule(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (isQuitting) return;

            if (!isInitialized)
            {
                Initialize();
            }

            lock (lockObject)
            {
                mainThreadActions.Enqueue(action);
            }
        }

        /// <summary>
        /// Schedules an action to be executed every frame.
        /// </summary>
        /// <param name="action">The action to execute every frame.</param>
        /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
        public static void SchedulePerFrame(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (isQuitting) return;

            if (!isInitialized)
            {
                Initialize();
            }

            lock (lockObject)
            {
                perFrameActions.Add(action);
            }
        }

        /// <summary>
        /// Removes a per-frame action from the scheduler.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        /// <returns>True if the action was removed, false otherwise.</returns>
        public static bool RemovePerFrame(Action action)
        {
            if (action == null || isQuitting) return false;

            lock (lockObject)
            {
                return perFrameActions.Remove(action);
            }
        }

        /// <summary>
        /// Clears all per-frame actions from the scheduler.
        /// </summary>
        public static void ClearPerFrameActions()
        {
            if (isQuitting) return;

            lock (lockObject)
            {
                perFrameActions.Clear();
            }
        }

        private void Update()
        {
            if (!isInitialized || isProcessing || isQuitting) return;
            isProcessing = true;

            try
            {
                ProcessMainThreadActions();
                ProcessPerFrameActions();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in UTaskScheduler Update: {e}");
            }
            finally
            {
                isProcessing = false;
            }
        }

        private void ProcessMainThreadActions()
        {
            int processedActions = 0;
            while (processedActions < maxActionsPerFrame)
            {
                Action action;
                lock (lockObject)
                {
                    if (mainThreadActions.Count == 0) break;
                    action = mainThreadActions.Dequeue();
                }

                try
                {
                    action();
                    processedActions++;
                    totalProcessedActions++;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }

            if (processedActions >= maxActionsPerFrame)
            {
                Debug.LogWarning($"Reached maximum actions per frame ({maxActionsPerFrame})");
            }
        }

        private void ProcessPerFrameActions()
        {
            // Clear and fill temporary list with current actions
            lock (lockObject)
            {
                tempActionsList.Clear();
                foreach (var action in perFrameActions)
                {
                    tempActionsList.Add(action);
                }
            }

            // Process actions from the temporary list
            var actionsToRemove = new List<Action>();
            foreach (var action in tempActionsList)
            {
                try
                {
                    if (action == null)
                    {
                        actionsToRemove.Add(action);
                        continue;
                    }

                    // Check if the method's target is a destroyed object
                    if (action.Target is UnityEngine.Object target && target == null)
                    {
                        actionsToRemove.Add(action);
                        continue;
                    }

                    action();
                    totalProcessedPerFrameActions++;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    actionsToRemove.Add(action);
                }
            }

            // Remove invalid actions
            if (actionsToRemove.Count > 0)
            {
                lock (lockObject)
                {
                    foreach (var action in actionsToRemove)
                    {
                        perFrameActions.Remove(action);
                    }
                }
            }
        }
    }
}