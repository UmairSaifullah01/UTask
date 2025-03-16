using UnityEngine;
using System;
using System.Collections;
using THEBADDEST.Tasks;

namespace THEBADDEST.Tasks.Examples
{
    public class UTaskCoroutineComparison : MonoBehaviour
    {
        [Header("Test Selection")]
        [SerializeField] private bool runBasicDelayComparison = true;
        [SerializeField] private bool runSequentialOperationsComparison = true;
        [SerializeField] private bool runParallelOperationsComparison = true;
        [SerializeField] private bool runErrorHandlingComparison = true;
        [SerializeField] private bool runConditionalWaitingComparison = true;

        [Header("Debug Options")]
        [SerializeField] private bool runCoroutineVersion = true;
        [SerializeField] private bool runUTaskVersion = true;

        private void Start()
        {
            if (runCoroutineVersion)
            {
                StartCoroutine(CoroutineExample());
            }

            if (runUTaskVersion)
            {
                UTaskExample();
            }
        }

        #region Coroutine Implementation

        private IEnumerator CoroutineExample()
        {
            Debug.Log("[Coroutine] Starting example...");

            if (runBasicDelayComparison)
            {
                // 1. Basic delay
                Debug.Log("[Coroutine] Waiting for 1 second...");
                yield return new WaitForSeconds(1f);
                Debug.Log("[Coroutine] Delay completed!");
            }

            if (runSequentialOperationsComparison)
            {
                // 2. Sequential operations
                yield return StartCoroutine(LoadDataCoroutine());
                yield return StartCoroutine(ProcessDataCoroutine());
            }

            if (runParallelOperationsComparison)
            {
                // 3. Parallel operations (more complex with coroutines)
                var operation1 = StartCoroutine(ParallelOperationCoroutine(1, 2f));
                var operation2 = StartCoroutine(ParallelOperationCoroutine(2, 3f));
                var operation3 = StartCoroutine(ParallelOperationCoroutine(3, 1f));

                // Wait for all parallel operations (need manual tracking)
                yield return StartCoroutine(WaitForAllCoroutines(operation1, operation2, operation3));
                Debug.Log("[Coroutine] All parallel operations completed!");
            }

            if (runErrorHandlingComparison)
            {
                // 4. Error handling (limited with coroutines)
                bool success = false;
                yield return StartCoroutine(TryOperationCoroutine((result) => success = result));
                Debug.Log($"[Coroutine] Operation {(success ? "succeeded" : "failed")}");
            }

            if (runConditionalWaitingComparison)
            {
                // 5. Conditional waiting
                condition = false;
                StartCoroutine(SetConditionAfterDelay()); // Set condition after delay
                yield return new WaitUntil(() => condition);
                Debug.Log("[Coroutine] Condition met!");
            }

            Debug.Log("[Coroutine] Example completed!");
        }

        private IEnumerator LoadDataCoroutine()
        {
            Debug.Log("[Coroutine] Loading data...");
            yield return new WaitForSeconds(1f);
            Debug.Log("[Coroutine] Data loaded!");
        }

        private IEnumerator ProcessDataCoroutine()
        {
            Debug.Log("[Coroutine] Processing data...");
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[Coroutine] Data processed!");
        }

        private IEnumerator ParallelOperationCoroutine(int id, float duration)
        {
            Debug.Log($"[Coroutine] Starting operation {id}");
            yield return new WaitForSeconds(duration);
            Debug.Log($"[Coroutine] Operation {id} completed");
        }

        private IEnumerator WaitForAllCoroutines(params Coroutine[] operations)
        {
            foreach (var operation in operations)
            {
                yield return operation;
            }
        }

        private IEnumerator TryOperationCoroutine(Action<bool> callback)
        {
            yield return new WaitForSeconds(1f);
            try
            {
                // Simulate some work
                callback(UnityEngine.Random.value >= 0.5f);
            }
            catch
            {
                callback(false);
            }
        }

        private IEnumerator SetConditionAfterDelay()
        {
            yield return new WaitForSeconds(2f);
            condition = true;
        }

        #endregion

        #region UTask Implementation

        private async void UTaskExample()
        {
            Debug.Log("[UTask] Starting example...");

            try
            {
                if (runBasicDelayComparison)
                {
                    // 1. Basic delay
                    Debug.Log("[UTask] Waiting for 1 second...");
                    await UTask.Delay(1f);
                    Debug.Log("[UTask] Delay completed!");
                }

                if (runSequentialOperationsComparison)
                {
                    // 2. Sequential operations
                    await LoadDataUTask();
                    await ProcessDataUTask();
                }

                if (runParallelOperationsComparison)
                {
                    // 3. Parallel operations (much simpler with UTask)
                    var operation1 = ParallelOperationUTask(1, 2f);
                    var operation2 = ParallelOperationUTask(2, 3f);
                    var operation3 = ParallelOperationUTask(3, 1f);

                    await UTask.All(operation1, operation2, operation3);
                    Debug.Log("[UTask] All parallel operations completed!");
                }

                if (runErrorHandlingComparison)
                {
                    // 4. Error handling (robust with UTask)
                    try
                    {
                        await TryOperationUTask();
                        Debug.Log("[UTask] Operation succeeded");
                    }
                    catch (Exception ex)
                    {
                        Debug.Log($"[UTask] Operation failed: {ex.Message}");
                    }
                }

                if (runConditionalWaitingComparison)
                {
                    // 5. Conditional waiting
                    condition = false;
                    _ = SetConditionAfterDelayUTask(); // Fire and forget
                    await UTask.WaitUntil(() => condition);
                    Debug.Log("[UTask] Condition met!");
                }

                Debug.Log("[UTask] Example completed!");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UTask] Error in example: {ex}");
            }
        }

        private async UTask LoadDataUTask()
        {
            Debug.Log("[UTask] Loading data...");
            await UTask.Delay(1f);
            Debug.Log("[UTask] Data loaded!");
        }

        private async UTask ProcessDataUTask()
        {
            Debug.Log("[UTask] Processing data...");
            await UTask.Delay(0.5f);
            Debug.Log("[UTask] Data processed!");
        }

        private async UTask ParallelOperationUTask(int id, float duration)
        {
            Debug.Log($"[UTask] Starting operation {id}");
            await UTask.Delay(duration);
            Debug.Log($"[UTask] Operation {id} completed");
        }

        private async UTask TryOperationUTask()
        {
            await UTask.Delay(1f);
            // Can throw exceptions naturally
            if (UnityEngine.Random.value < 0.5f)
            {
                throw new Exception("Random failure");
            }
        }

        private async UTask SetConditionAfterDelayUTask()
        {
            await UTask.Delay(2f);
            condition = true;
        }

        private bool condition = false;

        #endregion

        private void OnDestroy()
        {
            // Clean up any running tasks/coroutines
            StopAllCoroutines();
        }
    }
}