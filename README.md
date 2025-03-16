# UTask

UTask is a lightweight and efficient task scheduling system for Unity, providing a modern async/await pattern implementation specifically designed for Unity game development.

## Description

UTask is a powerful and flexible task management solution that brings the convenience of C#'s async/await pattern to Unity development. It offers a more intuitive alternative to coroutines with better error handling, cancellation support, and dependency management. This package is designed to help Unity developers write cleaner, more maintainable asynchronous code while maintaining high performance.

### Key Benefits:

- 🚀 **High Performance**: Optimized scheduler with ring buffer implementation
- 🎯 **Type Safety**: Full generic support and compile-time type checking
- 🔄 **Easy Coroutine Migration**: Seamless conversion from Unity's coroutines
- 🛡️ **Robust Error Handling**: Better exception propagation and handling
- 🎮 **Unity-Specific**: Built with Unity's architecture in mind
- 🧩 **Modular Design**: Easy to integrate into existing projects
- 📦 **Lightweight**: Minimal overhead and memory footprint
- ♻️ **Memory Efficient**: Object pooling for task continuations

## Performance Optimizations

### Task Scheduler

- Ring buffer implementation for efficient action queuing
- Optimized per-frame action handling
- Reduced lock contention
- Automatic cleanup of completed tasks

### Memory Management

- Object pooling for continuation wrappers
- Efficient delay implementation using min-heap
- Reduced garbage collection pressure
- Smart handling of task completions

### Delay System

- Centralized delay management
- Efficient time tracking
- Minimal per-frame overhead
- Optimized scheduling of delayed tasks

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
    await UTask.Delay(1000); // Wait for 1 second
}

// With return value
async UTask<int> MyAsyncMethodWithResult()
{
    await UTask.Delay(1000);
    return 42;
}
```

### Time Control Operations

```csharp
// Different ways to handle time
await UTask.Wait(1f);                  // Basic wait
await 2f.Seconds();                    // Extension method on float
await 500f.Ms();                       // Wait milliseconds
await 0.5f.Minutes();                  // Wait minutes

// Frame control
await UTask.Next();                    // Wait for next frame
await UTask.Fixed();                   // Wait for next fixed update
```

### Parallel Task Execution

```csharp
// Running multiple tasks in parallel
UTask task1 = SlowOperation("Task 1", 1f);
UTask task2 = SlowOperation("Task 2", 2f);
UTask task3 = SlowOperation("Task 3", 3f);

await UTask.All(task1, task2, task3);  // Wait for all tasks to complete

async UTask SlowOperation(string name, float delay)
{
    Debug.Log($"{name} starting...");
    await UTask.Delay(delay);
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
await UTask.Until(() => condition);

// Wait while condition is true
await UTask.While(() => condition);
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

        // Works with any UnityEngine.Object
        await LongRunningTask()
            .ToDepend(this);

        // Works with generic tasks too
        int result = await CalculateWithDelay()
            .ToDepend(dependency);
    }

    private async UTask LongRunningTask()
    {
        await UTask.Wait(2f);
    }

    private async UTask<int> CalculateWithDelay()
    {
        await UTask.Wait(1f);
        return 42;
    }
}
```

## Components

- `UTask` - Base task implementation with static utility methods
- `UTaskUtility` - Extension methods and utility functions
- `UTaskScheduler` - High-performance Unity-optimized scheduler
- `UTaskCompletionSource` - Memory-efficient task completion control
- `UTaskParamertrized` - Generic task implementation

## Best Practices

1. Use static methods from `UTask` for core functionality (e.g., `UTask.Delay`, `UTask.Next`)
2. Use extension methods from `UTaskUtility` for convenience (e.g., `2f.Seconds()`)
3. Leverage object pooling for better memory management
4. Use task dependencies to prevent memory leaks
5. Handle exceptions appropriately in async methods
6. Avoid creating unnecessary task continuations

## 🚀 About Me

Umair Saifullah ~ a unity developer from Pakistan.

## License

[MIT](https://choosealicense.com/licenses/mit/)
