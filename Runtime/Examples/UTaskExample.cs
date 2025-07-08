using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

namespace THEBADDEST.Tasks.Examples
{
    public class UTaskExample : MonoBehaviour
    {
        private void Start()
        {
            Debug.Log("Starting UTaskExample...");
            RunAllExamples();
        }

        private async void RunAllExamples()
        {
            try
            {
                // await RunBasicExample();
                // await RunGenericExample();
                // await RunParallelExample();
                // await RunCoroutineConversionExample();
                // await RunSceneLoadExample();
                // await RunErrorHandlingExample();
                // await RunCustomYieldExample();
                // await RunFixedUpdateExample();
                // await RunCanceledExample();
                //await RunCancellationExample();
                //await RunMultiThreadedExample(); // <--- Uncomment to test MultiThreaded extension
                 await RunParallelLoopExample(); // <--- Uncomment to test parallel loop with MultiThreaded
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in examples: {ex}");
            }
        }

        // Basic non-generic UTask example
        private async UTask RunBasicExample()
        {
            Debug.Log("Starting basic example...");

            // Test different wait syntaxes
            Debug.Log("Testing different wait syntaxes...");
            await UTask.Wait(1f);                  // Basic wait
            await 2f.Seconds();              // Extension method on float
            await 500f.Ms();                 // Wait milliseconds
            await 0.5f.Minutes();            // Wait minutes
            Debug.Log("All waits completed!");

            // Test next frame
            Debug.Log("Testing next frame...");
            await UTask.Next();
            Debug.Log("Now we're on the next frame!");

            // Test fixed update
            Debug.Log("Testing fixed update...");
            await UTask.Fixed();
            Debug.Log("After fixed update!");

            // Test Until and While
            bool flag = false;
            Debug.Log("Testing Until...");

            // Schedule flag to be set after 1 second
            UTaskScheduler.Schedule(async () =>
            {
                await UTask.Wait(1f);
                flag = true;
            });

            await UTask.Until(() => flag);
            Debug.Log("Flag was set!");

            Debug.Log("Testing While...");
            await UTask.While(() => !flag);
            Debug.Log("Flag remained set!");

            Debug.Log("Basic example completed!");
        }

        // Example with return value using UTask<T>
        private async UTask<int> CalculateWithDelay()
        {
            Debug.Log("Starting calculation...");
            await UTask.Delay(1f);
            Debug.Log("Calculation complete!");
            return 42;
        }

        private async UTask RunGenericExample()
        {
            Debug.Log("Starting generic example...");
            int result = await CalculateWithDelay();
            Debug.Log($"Got result: {result}");
        }

        // Example of running multiple tasks in parallel
        private async UTask RunParallelExample()
        {
            Debug.Log("Starting parallel operations...");

            UTask task1 = SlowOperation("Task 1", 1f);
            UTask task2 = SlowOperation("Task 2", 2f);
            UTask task3 = SlowOperation("Task 3", 3f);

            await UTask.All(task1, task2, task3);  // Using shorter All instead of WhenAll
            Debug.Log("All parallel operations completed!");
        }

        private async UTask SlowOperation(string name, float delay)
        {
            Debug.Log($"{name} starting...");
            await UTask.Delay(delay);
            Debug.Log($"{name} completed after {delay} seconds");
        }

        // Example of converting coroutine to UTask
        private async UTask RunCoroutineConversionExample()
        {
            Debug.Log("Starting coroutine conversion example...");

            await TestCoroutine().ToUTask();
            Debug.Log("Coroutine conversion completed!");

            // Test custom yield instruction
            await new WaitForSeconds(1f).ToUTask();
            Debug.Log("WaitForSeconds completed!");
        }

        private IEnumerator TestCoroutine()
        {
            Debug.Log("Coroutine started");
            yield return new WaitForSeconds(1f);
            Debug.Log("After 1 second");
            yield return new WaitForFixedUpdate();
            Debug.Log("After fixed update");
            yield return new WaitUntil(() => Time.time > 2f);
            Debug.Log("After condition met");
        }

        // Example of loading a scene asynchronously
        private async UTask RunSceneLoadExample()
        {
            Debug.Log("Starting scene load example...");

            // Get the current scene name
            string currentScene = SceneManager.GetActiveScene().name;

            // Load the same scene additively as an example
            var operation = SceneManager.LoadSceneAsync(currentScene, LoadSceneMode.Additive);
            operation.allowSceneActivation = true;

            Debug.Log("Waiting for scene to load...");
            await operation.ToUTask();
            Debug.Log("Scene loaded!");

            // Wait a bit and then unload the scene
            await UTask.Delay(1f);
            Debug.Log("Unloading scene...");

            operation = SceneManager.UnloadSceneAsync(currentScene);
            await operation.ToUTask();
            Debug.Log("Scene unloaded!");
        }

        // Example of error handling
        private async UTask RunErrorHandlingExample()
        {
            try
            {
                Debug.Log("Starting error handling example...");
                await ThrowErrorAfterDelay();
                Debug.Log("This line will never be reached");
            }
            catch (Exception e)
            {
                Debug.LogError($"Caught error: {e.Message}");
            }
        }

        private async UTask ThrowErrorAfterDelay()
        {
            await UTask.Delay(1f);
            throw new Exception("This is a test error");
        }

        // Example of custom yield instruction
        private async UTask RunCustomYieldExample()
        {
            Debug.Log("Starting custom yield example...");

            var customYield = new CustomTimedYield(3f);
            await customYield.ToUTask();

            Debug.Log("Custom yield completed!");
        }

        private class CustomTimedYield : CustomYieldInstruction
        {
            private float endTime;

            public CustomTimedYield(float duration)
            {
                endTime = Time.time + duration;
            }

            public override bool keepWaiting => Time.time < endTime;
        }

        // Example of using PerFixedFrame scheduling
        private async UTask RunFixedUpdateExample()
        {
            Debug.Log("Starting FixedUpdate scheduling example...");

            float elapsedTime = 0f;
            var tcs = UTask.CreateManualTask();

            // Schedule a fixed update action that runs for 3 seconds
            UTaskScheduler.SchedulePerFixedFrame(() =>
            {
                elapsedTime += Time.fixedDeltaTime;
                Debug.Log($"Fixed Update: {elapsedTime:F2} seconds");

                if (elapsedTime >= 3f)
                {
                    UTaskScheduler.RemovePerFixedFrame(() => { }); // Remove this action
                    tcs.TrySetResult();
                }
            });

            await tcs.Task;
            Debug.Log("FixedUpdate example completed!");
        }

        // Example of using Break
        private async UTask RunCanceledExample()
        {
            Debug.Log("Starting Break example...");

            try
            {
                // Example 1: Direct usage of Break
                Debug.Log("Testing direct Break...");
                await UTask.Break();
                Debug.Log("This line will not be reached");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Task was canceled as expected!");
            }

            try
            {
                // Example 2: Using Break in a conditional operation
                Debug.Log("Testing conditional Break...");
                await ConditionalOperation(shouldCancel: true);
                Debug.Log("This line will not be reached");
            }
            catch (OperationCanceledException)
            {
                Debug.Log("Conditional operation was canceled!");
            }

            // Example 3: Using Break in error handling
            Debug.Log("Testing error handling with Break...");
            UTask task = await TryOperation();
            Debug.Log("Operation completed or was canceled");
        }

        // Helper method to demonstrate conditional cancellation
        private UTask ConditionalOperation(bool shouldCancel)
        {
            if (shouldCancel)
            {
                return UTask.Break();
            }

            return SlowOperation("Normal Operation", 1f);
        }

        // Helper method to demonstrate error handling with cancellation
        private async UTask<UTask> TryOperation()
        {
            try
            {
                // Simulate some condition that might cause cancellation
                if (Time.time % 2 == 0)
                {
                    Debug.Log("Condition met, breaking operation");
                    return UTask.Break();
                }

                Debug.Log("Condition not met, proceeding with operation");
                return SlowOperation("Normal Operation", 1f);
            }
            catch (Exception)
            {
                return UTask.Break();
            }
        }

        // Example of using cancellation
        private async UTask RunCancellationExample()
        {
            Debug.Log("Starting cancellation example...");

            // Create a cancellation source
            var cts = new UTaskCancellationTokenSource();
            try
            {
                // Start a long running task with cancellation
                Debug.Log("Starting long running task with cancellation...");
                var task = LongRunningTaskWithCancellation(cts.Token);

                // Wait for 2 seconds then cancel
                await UTask.Delay(2f);
                Debug.Log("Canceling the task...");
                cts.Cancel();

                try
                {
                    await task;
                    Debug.Log("This line should not be reached");
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("Task was successfully canceled!");
                }
            }
            finally
            {
                cts.Dispose();
            }

            // Example of canceling an already running task
            Debug.Log("\nTesting cancellation of running task...");
            cts = new UTaskCancellationTokenSource();
            try
            {
                var longTask = SlowOperation("Cancellable Task", 5f)
                    .WithCancellation(cts.Token);

                await UTask.Delay(2f);
                Debug.Log("Canceling the running task...");
                cts.Cancel();

                try
                {
                    await longTask;
                    Debug.Log("This line should not be reached");
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("Running task was successfully canceled!");
                }
            }
            finally
            {
                cts.Dispose();
            }
        }

        private async UTask LongRunningTaskWithCancellation(UTaskCancellationToken cancellationToken)
        {
            Debug.Log("Long running task started");

            for (int i = 0; i < 10; i++)
            {
                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                Debug.Log($"Working... {i + 1}/10");
                await UTask.Delay(1f);
            }

            Debug.Log("Long running task completed!");
        }

        /// <summary>
        /// Demonstrates running UTask continuations on a background thread using .MultiThreaded().
        /// </summary>
        private async UTask RunMultiThreadedExample()
        {
            Debug.Log($"[MainThread] Before await: Thread={System.Threading.Thread.CurrentThread.ManagedThreadId}, IsMainThread={UnityEngine.Application.isPlaying}");

            // Run a simple UTask and switch its continuation to a background thread
            await UTask.Delay(1f).MultiThreaded();
            Debug.Log($"[MultiThreaded] After await: Thread={System.Threading.Thread.CurrentThread.ManagedThreadId}, IsMainThread={UnityEngine.Application.isPlaying}");

            // Run a UTask<T> and switch its continuation to a background thread
            int result = await CalculateWithDelay().MultiThreaded();
            Debug.Log($"[MultiThreaded] Got result: {result} on Thread={System.Threading.Thread.CurrentThread.ManagedThreadId}");

            // Show that you can chain back to main thread if needed (not implemented here, but possible with a .MainThreaded() extension)
            Debug.Log("MultiThreaded example completed!");
        }

        /// <summary>
        /// Demonstrates running a parallel loop using UTask and .MultiThreaded().
        /// </summary>
        private async UTask RunParallelLoopExample()
        {
            int numTasks = 5;
            UTask[] tasks = new UTask[numTasks];

            Debug.Log($"[MainThread] Starting parallel loop with {numTasks} tasks...");

            for (int i = 0; i < numTasks; i++)
            {
                int taskIndex = i; // Capture loop variable
                tasks[i] = UTask.Delay(UnityEngine.Random.Range(0.5f, 2f)).MultiThreaded().ContinueWith(() =>
                {
                    Debug.Log($"[MultiThreaded] Task {taskIndex} completed on Thread={System.Threading.Thread.CurrentThread.ManagedThreadId}");
                });
            }

            await UTask.All(tasks);
            Debug.Log("[MainThread] All parallel loop tasks completed!");
        }
        
    }
}