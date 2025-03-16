using System;
using System.Runtime.CompilerServices;
using System.Security;


namespace THEBADDEST.Tasks
{
    [AsyncMethodBuilder(typeof(UTaskMethodBuilder<>))]
    public readonly partial struct UTask<T>
    {
        internal readonly IUTaskSource<T> source;
        private readonly short token;

        public UTask(IUTaskSource<T> source, short token)
        {
            this.source = source;
            this.token = token;
        }

        internal short Token   => token;
        public   bool  IsValid => source != null;
        public UTaskStatus Status
        {
            get
            {
                if (source == null) throw new InvalidOperationException("Cannot get status of default UTask<T>");
                return source.GetStatus(token);
            }
        }

        public bool IsCompleted
        {
            get
            {
                if (source == null) throw new InvalidOperationException("Cannot check completion of default UTask<T>");
                return source.GetStatus(token).IsCompleted();
            }
        }

        public T GetResult()
        {
            if (source == null) throw new InvalidOperationException("Cannot get result of default UTask<T>");
            return source.GetResult(token);
        }

        public UTaskAwaiter<T> GetAwaiter()
        {
            return new UTaskAwaiter<T>(this);
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
    }

    public readonly struct UTaskAwaiter<T> : INotifyCompletion
    {
        private readonly UTask<T> task;

        public UTaskAwaiter(UTask<T> task)
        {
            this.task = task;
        }

        public bool IsCompleted => task.IsCompleted;

        public T GetResult() => task.GetResult();

        public void OnCompleted(Action continuation)
        {
            task.OnCompleted(continuation);
        }
    }

    public struct UTaskMethodBuilder<T>
    {
        private UTaskCompletionSource<T> tcs;

        public static UTaskMethodBuilder<T> Create()
        {
            return new UTaskMethodBuilder<T> { tcs = new UTaskCompletionSource<T>() };
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        public void SetResult(T result)
        {
            tcs.TrySetResult(result);
        }

        public void SetException(Exception exception)
        {
            tcs.TrySetException(exception);
        }

        public UTask<T> Task => tcs.Task;

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        [SecuritySafeCritical]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }
    }
}