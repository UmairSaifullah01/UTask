using UnityEngine;
using System;

namespace THEBADDEST.Tasks.Examples
{
    public class UTaskDependencyExample : MonoBehaviour
    {
        [SerializeField] private GameObject dependencyObject;
        [SerializeField] private ScriptableObject dataAsset;

        private void Start()
        {
            RunExamples();
        }

        private async void RunExamples()
        {
            try
            {
                // Example 1: Simple GameObject dependency
                await LongRunningTask()
                    .ToDepend(dependencyObject);
                Debug.Log("Task completed because GameObject still exists!");

                // Example 2: ScriptableObject dependency
                // var result = await CalculateWithDelay()
                //     .ToDepend(dataAsset);
                // Debug.Log($"Calculation result: {result} (ScriptableObject still exists)");

                // Example 3: Component dependency (this MonoBehaviour)
                await LongRunningTask()
                    .ToDepend(this);
                Debug.Log("Task completed because MonoBehaviour still exists!");

                // Example 4: Task will be canceled if object is destroyed
                GameObject tempObject = new GameObject("Temporary");
                var task = LongRunningTask()
                    .ToDepend(tempObject);

                // Destroy the dependency object
                Destroy(tempObject);

                try
                {
                    await task;
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("Task was canceled because GameObject was destroyed!");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in examples: {ex}");
            }
        }

        private async UTask LongRunningTask()
        {
            Debug.Log("Starting long running task...");
            await UTask.Wait(2f);
            Debug.Log("Long running task completed!");
        }

        private async UTask<int> CalculateWithDelay()
        {
            Debug.Log("Starting calculation...");
            await UTask.Wait(1f);
            return 42;
        }
    }
}