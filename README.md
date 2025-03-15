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

## Installation

Add this package to your Unity project through the Package Manager.

## Usage

```csharp
// Basic usage
async UTask MyAsyncMethod()
{
    await UTaskScheduler.Delay(1000); // Wait for 1 second
}

// With return value
async UTask<int> MyAsyncMethodWithResult()
{
    await UTaskScheduler.Delay(1000);
    return 42;
}
```

## Components

- `UTask` - Base task implementation
- `UTaskX` - Extended task functionality
- `UTaskScheduler` - Custom Unity-optimized scheduler
- `UTaskCompletionSource` - Task completion control
- `UTaskParamertrized` - Generic task implementation

## Version

Current Version: 1.0

## License

[Your License Type] - See LICENSE file for details
