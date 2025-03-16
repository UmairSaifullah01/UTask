using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace THEBADDEST.Tasks
{
    [AsyncMethodBuilder(typeof(UTaskMethodBuilder))]
    public readonly partial struct UTask : IEquatable<UTask>
    {
        internal readonly IUTaskSource source;
        private readonly short token;

        public UTask(IUTaskSource source, short token)
        {
            this.source = source;
            this.token = token;
        }

        internal short Token => token;

        public bool IsValid => source != null;

        public bool IsCompleted => source?.GetStatus(token).IsCompleted() ?? false;

        public bool IsFaulted => source?.GetStatus(token) == UTaskStatus.Faulted;

        public bool IsCanceled => source?.GetStatus(token) == UTaskStatus.Canceled;

        public UTaskStatus Status
        {
            get
            {
                if (source == null) return UTaskStatus.Canceled;
                return source.GetStatus(token);
            }
        }

        // Instance methods
        public bool Equals(UTask other)
        {
            return source == other.source && token == other.token;
        }

        public override bool Equals(object obj)
        {
            return obj is UTask other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((source != null ? source.GetHashCode() : 0) * 397) ^ token.GetHashCode();
            }
        }

        internal void OnCompleted(Action continuation)
        {
            if (source == null)
            {
                UTaskScheduler.Schedule(continuation);
                return;
            }
            source.OnCompleted(state => ((Action)state)(), continuation, Token);
        }

        public void GetResult()
        {
            if (source == null) return;
            source.GetResult(token);
        }

        public UTaskAwaiter GetAwaiter()
        {
            return new UTaskAwaiter(this);
        }

        public static bool operator ==(UTask left, UTask right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(UTask left, UTask right)
        {
            return !left.Equals(right);
        }
    }

    public readonly struct UTaskAwaiter : INotifyCompletion
    {
        private readonly UTask task;

        public UTaskAwaiter(UTask task)
        {
            this.task = task;
        }

        public bool IsCompleted => task.IsCompleted;

        public void GetResult() => task.GetResult();

        public void OnCompleted(Action continuation)
        {
            task.OnCompleted(continuation);
        }
    }

    public enum UTaskStatus
    {
        Pending,
        Succeeded,
        Faulted,
        Canceled
    }

    public static class UTaskStatusExtensions
    {
        public static bool IsCompleted(this UTaskStatus status)
        {
            return status != UTaskStatus.Pending;
        }
    }
}