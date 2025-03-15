using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace THEBADDEST.Tasks
{
    [AsyncMethodBuilder(typeof(UTaskMethodBuilder))]
    public readonly struct UTask
    {
        internal readonly IUTaskSource source;
        private readonly short token;

        public UTask(IUTaskSource source, short token)
        {
            this.source = source;
            this.token = token;
        }

        internal short Token => token;

        internal void OnCompleted(Action continuation)
        {
            if (source == null)
            {
                UTaskScheduler.Schedule(continuation);
                return;
            }
            source.OnCompleted(state => ((Action)state)(), continuation, Token);
        }

        public UTaskStatus Status
        {
            get
            {
                if (source == null) return UTaskStatus.Succeeded;
                return source.GetStatus(token);
            }
        }

        public bool IsCompleted
        {
            get
            {
                if (source == null) return true;
                return source.GetStatus(token).IsCompleted();
            }
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

        public static UTask CompletedTask => new UTask();
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