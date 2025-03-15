using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace THEBADDEST.Tasks
{
    public struct UTaskMethodBuilder
    {
        private UTaskCompletionSource tcs;

        public static UTaskMethodBuilder Create()
        {
            return new UTaskMethodBuilder { tcs = new UTaskCompletionSource() };
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        public void SetResult()
        {
            tcs.TrySetResult();
        }

        public void SetException(Exception exception)
        {
            tcs.TrySetException(exception);
        }

        public UTask Task => tcs.Task;

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