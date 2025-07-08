using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace THEBADDEST.Tasks
{
    /// <summary>
    /// A scheduler for managing Unity tasks and coroutines efficiently.
    /// Handles both one-time actions and per-frame actions with proper cleanup.
    /// </summary>
    public class UTaskScheduler : MonoBehaviour
    {
        private const int RING_BUFFER_SIZE = 1024;
        private static readonly Action[] actionRingBuffer = new Action[RING_BUFFER_SIZE];
        private static int head = 0;
        private static int tail = 0;
        private static readonly object lockObject = new object();
        private static readonly HashSet<Action> perFrameActions = new HashSet<Action>();
        private static readonly HashSet<Action> perFixedFrameActions = new HashSet<Action>();
        private static bool isProcessing;
        private static bool isInitialized;
        private static bool isQuitting;
        private static int maxActionsPerFrame = 100; // Prevent too many actions in one frame
        private static int totalProcessedActions;
        private static int totalProcessedPerFrameActions;
        private static int totalProcessedPerFixedFrameActions;
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
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorApplication.update += EditorUpdate;
                    isInitialized = true;
                    return;
                }
#endif
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
            ClearPerFixedFrameActions();
        }

        private void OnDisable()
        {
            if (isInitialized)
            {
                ClearPerFrameActions();
                ClearPerFixedFrameActions();
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
                int nextTail = (tail + 1) % RING_BUFFER_SIZE;
                if (nextTail == head)
                {
                    Debug.LogError("Action queue is full!");
                    return;
                }

                actionRingBuffer[tail] = action;
                tail = nextTail;
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

        /// <summary>
        /// Schedules an action to be executed every fixed update.
        /// </summary>
        /// <param name="action">The action to execute every fixed update.</param>
        /// <exception cref="ArgumentNullException">Thrown when action is null.</exception>
        public static void SchedulePerFixedFrame(Action action)
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
                perFixedFrameActions.Add(action);
            }
        }

        /// <summary>
        /// Removes a per-fixed-frame action from the scheduler.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        /// <returns>True if the action was removed, false otherwise.</returns>
        public static bool RemovePerFixedFrame(Action action)
        {
            if (action == null || isQuitting) return false;

            lock (lockObject)
            {
                return perFixedFrameActions.Remove(action);
            }
        }

        /// <summary>
        /// Clears all per-fixed-frame actions from the scheduler.
        /// </summary>
        public static void ClearPerFixedFrameActions()
        {
            if (isQuitting) return;

            lock (lockObject)
            {
                perFixedFrameActions.Clear();
            }
        }

#if UNITY_EDITOR
        private static void EditorUpdate()
        {
            if (Application.isPlaying || !isInitialized || isProcessing || isQuitting) return;
            isProcessing = true;
            try
            {
                ProcessMainThreadActionsStatic();
                ProcessPerFrameActionsStatic();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in UTaskScheduler EditorUpdate: {e}");
            }
            finally
            {
                isProcessing = false;
            }
        }
#endif

        private void Update()
        {
            if (!isInitialized || isProcessing || isQuitting) return;
            if (!Application.isPlaying) return; // Only run in play mode
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
            int processedCount = 0;
            int currentTail;

            lock (lockObject)
            {
                currentTail = tail;
            }

            while (head != currentTail && processedCount < RING_BUFFER_SIZE)
            {
                var action = actionRingBuffer[head];
                if (action != null)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    actionRingBuffer[head] = null; // Help GC
                }

                head = (head + 1) % RING_BUFFER_SIZE;
                processedCount++;
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

        private void FixedUpdate()
        {
            if (!isInitialized || isProcessing || isQuitting) return;
            isProcessing = true;

            try
            {
                ProcessPerFixedFrameActions();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in UTaskScheduler FixedUpdate: {e}");
            }
            finally
            {
                isProcessing = false;
            }
        }

        private void ProcessPerFixedFrameActions()
        {
            // Clear and fill temporary list with current fixed update actions
            lock (lockObject)
            {
                tempActionsList.Clear();
                foreach (var action in perFixedFrameActions)
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
                    totalProcessedPerFixedFrameActions++;
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
                        perFixedFrameActions.Remove(action);
                    }
                }
            }
        }

#if UNITY_EDITOR
        private static void ProcessMainThreadActionsStatic()
        {
            int processedCount = 0;
            int currentTail;
            lock (lockObject)
            {
                currentTail = tail;
            }
            while (head != currentTail && processedCount < RING_BUFFER_SIZE)
            {
                var action = actionRingBuffer[head];
                if (action != null)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                }
                head = (head + 1) % RING_BUFFER_SIZE;
                processedCount++;
                totalProcessedActions++;
            }
        }

        private static void ProcessPerFrameActionsStatic()
        {
            lock (lockObject)
            {
                tempActionsList.Clear();
                foreach (var action in perFrameActions)
                {
                    tempActionsList.Add(action);
                }
            }
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
#endif
    }
}