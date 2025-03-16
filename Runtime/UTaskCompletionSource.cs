using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using UnityEngine;

namespace THEBADDEST.Tasks
{
    public class UTaskCompletionSource : IUTaskSource
    {
        private static readonly Action<object> ExecuteContinuationDelegate = ExecuteContinuation;
        private Action<object> continuation;
        private object continuationState;
        private UTaskStatus status;
        private ExceptionDispatchInfo exception;
        private short version;

        public UTask Task => new UTask(this, version);

        public UTaskStatus GetStatus(short token)
        {
            if (token != version) throw new InvalidOperationException("Invalid task token");
            return status;
        }

        public void GetResult(short token)
        {
            if (token != version) throw new InvalidOperationException("Invalid task token");

            if (status == UTaskStatus.Succeeded) return;
            if (status == UTaskStatus.Faulted && exception != null)
            {
                exception.Throw();
            }
            //throw new InvalidOperationException($"Task is in {status} state");
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (token != version) throw new InvalidOperationException("Invalid task token");

            if (status.IsCompleted())
            {
                UTaskScheduler.Schedule(() => continuation(state));
            }
            else
            {
                this.continuation = continuation;
                this.continuationState = state;
            }
        }

        public bool TrySetCanceled(short token)
        {
            if (token != version) throw new InvalidOperationException("Invalid task token");
            return TrySetCanceled();
        }

        public bool TrySetResult()
        {
            if (status.IsCompleted()) return false;

            status = UTaskStatus.Succeeded;
            TriggerContinuation();
            return true;
        }

        public bool TrySetException(Exception exception)
        {
            if (status.IsCompleted()) return false;

            status = UTaskStatus.Faulted;
            this.exception = ExceptionDispatchInfo.Capture(exception);
            TriggerContinuation();
            return true;
        }

        public bool TrySetCanceled()
        {
            if (status.IsCompleted()) return false;

            status = UTaskStatus.Canceled;
            TriggerContinuation();
            return true;
        }

        private void TriggerContinuation()
        {
            if (continuation == null) return;

            var cont = continuation;
            var state = continuationState;
            continuation = null;
            continuationState = null;

            var wrapper = ContinuationWrapper.Get(cont, state);
            UTaskScheduler.Schedule(() => ExecuteContinuationDelegate(wrapper));
        }

        private static void ExecuteContinuation(object state)
        {
            var wrapper = (ContinuationWrapper)state;
            try
            {
                wrapper.Continuation(wrapper.State);
            }
            finally
            {
                wrapper.Return();
            }
        }

        private class ContinuationWrapper
        {
            private static readonly ObjectPool<ContinuationWrapper> Pool = new ObjectPool<ContinuationWrapper>(() => new ContinuationWrapper());

            public Action<object> Continuation;
            public object State;

            public static ContinuationWrapper Get(Action<object> continuation, object state)
            {
                var wrapper = Pool.Get();
                wrapper.Continuation = continuation;
                wrapper.State = state;
                return wrapper;
            }

            public void Return()
            {
                Continuation = null;
                State = null;
                Pool.Return(this);
            }
        }

        public void Reset()
        {
            status = UTaskStatus.Pending;
            exception = null;
            continuation = null;
            continuationState = null;
            version++;
        }
    }

    internal class ObjectPool<T> where T : class
    {
        private readonly Func<T> createFunc;
        private readonly Stack<T> pool;
        private readonly int maxSize;

        public ObjectPool(Func<T> createFunc, int initialSize = 32, int maxSize = 1024)
        {
            this.createFunc = createFunc;
            this.maxSize = maxSize;
            pool = new Stack<T>(initialSize);
        }

        public T Get()
        {
            lock (pool)
            {
                if (pool.Count > 0)
                    return pool.Pop();
            }
            return createFunc();
        }

        public void Return(T item)
        {
            lock (pool)
            {
                if (pool.Count < maxSize)
                    pool.Push(item);
            }
        }
    }
}