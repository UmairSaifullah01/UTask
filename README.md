# UTask

UTask is a lightweight and efficient task scheduling system for Unity, providing a modern async/await pattern implementation specifically designed for Unity game development.

## Features

- Lightweight task scheduling system
- Async/await pattern support
- Generic and non-generic task implementations
- Custom task scheduler optimized for Unity
- Parameterized task support
- Completion source pattern implementation
- Thread-safe task execution
- Unity-specific optimizations
- Coroutine conversion support
- Scene management integration
- Custom yield instruction support
- Object dependency handling

## Installation

Add this package to your Unity project through the Package Manager.

## Usage

### Basic Usage

```csharp
// Basic usage with delay
async UTask MyAsyncMethod()
{
    await UTaskX.Delay(1000); // Wait for 1 second
}

// With return value
async UTask<int> MyAsyncMethodWithResult()
{
    await UTaskX.Delay(1000);
    return 42;
}
```

### Time Control Operations

```csharp
// Different ways to handle time
await UTaskX.Wait(1f);                  // Basic wait
await 2f.Seconds();                     // Extension method on float
await 500f.Ms();                        // Wait milliseconds
await 0.5f.Minutes();                   // Wait minutes

// Frame control
await UTaskX.NextFrame();               // Wait for next frame
await UTaskX.Fixed();                   // Wait for next fixed update
```

### Parallel Task Execution

```csharp
// Running multiple tasks in parallel
UTask task1 = SlowOperation("Task 1", 1f);
UTask task2 = SlowOperation("Task 2", 2f);
UTask task3 = SlowOperation("Task 3", 3f);

await UTaskX.All(task1, task2, task3);  // Wait for all tasks to complete

async UTask SlowOperation(string name, float delay)
{
    Debug.Log($"{name} starting...");
    await UTaskX.Delay(delay);
    Debug.Log($"{name} completed after {delay} seconds");
}
```

### Coroutine Conversion

```csharp
// Convert Unity Coroutines to UTask
IEnumerator MyCoroutine()
{
    yield return new WaitForSeconds(1f);
    yield return new WaitForFixedUpdate();
}

// Use it with UTask
await MyCoroutine().ToUTask();

// Direct conversion of yield instructions
await new WaitForSeconds(1f).ToUTask();
```

### Scene Management

```csharp
// Async scene loading
var operation = SceneManager.LoadSceneAsync("MyScene", LoadSceneMode.Additive);
operation.allowSceneActivation = true;
await operation.ToUTask();

// Unloading
await SceneManager.UnloadSceneAsync("MyScene").ToUTask();
```

### Conditional Operations

```csharp
// Wait until condition is met
await UTaskX.Until(() => condition);

// Wait while condition is true
await UTaskX.While(() => condition);
```

### Error Handling

```csharp
try
{
    await SomeAsyncOperation();
}
catch (Exception e)
{
    Debug.LogError($"Operation failed: {e.Message}");
}
```

### Custom Yield Instructions

```csharp
public class CustomTimedYield : CustomYieldInstruction
{
    private float endTime;

    public CustomTimedYield(float duration)
    {
        endTime = Time.time + duration;
    }

    public override bool keepWaiting => Time.time < endTime;
}

// Use it with UTask
await new CustomTimedYield(3f).ToUTask();
```

### Object Dependencies

```csharp
// Make a task dependent on a GameObject
public class Example : MonoBehaviour
{
    [SerializeField] private GameObject dependency;

    async void Start()
    {
        // Task will be canceled if the GameObject is destroyed
        await LongRunningTask()
            .ToDepend(dependency);

        // Works with any UnityEngine.Object (MonoBehaviour, ScriptableObject, etc.)
        await LongRunningTask()
            .ToDepend(this);  // Depend on this MonoBehaviour

        // Works with generic tasks too
        int result = await CalculateWithDelay()
            .ToDepend(dependency);

        // Example of handling cancellation
        try
        {
            GameObject temp = new GameObject();
            var task = LongRunningTask().ToDepend(temp);
            Destroy(temp);  // Destroying dependency
            await task;     // This will throw OperationCanceledException
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Task was canceled because dependency was destroyed!");
        }
    }

    private async UTask LongRunningTask()
    {
        await UTaskX.Wait(2f);
    }

    private async UTask<int> CalculateWithDelay()
    {
        await UTaskX.Wait(1f);
        return 42;
    }
}
```

## Components

- `UTask` - Base task implementation
- `UTaskX` - Extended task functionality with utility methods
- `UTaskScheduler` - Custom Unity-optimized scheduler
- `UTaskCompletionSource` - Task completion control
- `UTaskParamertrized` - Generic task implementation

## Version

Current Version: 1.0

## License

[Your License Type] - See LICENSE file for details
