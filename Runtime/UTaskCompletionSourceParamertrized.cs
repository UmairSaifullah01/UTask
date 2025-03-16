using System;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace THEBADDEST.Tasks
{
    public class UTaskCompletionSource<T> : IUTaskSource<T>
    {
        private Action<object> continuation;
        private object continuationState;
        private UTaskStatus status;
        private ExceptionDispatchInfo exception;
        private T result;
        private short version;

        public UTask<T> Task => new UTask<T>(this, version);

        public UTaskStatus GetStatus(short token)
        {
            if (token != version) throw new InvalidOperationException("Invalid task token");
            return status;
        }

        void IUTaskSource.GetResult(short token)
        {
            GetResult(token);
        }

        public T GetResult(short token)
        {
            if (token != version) throw new InvalidOperationException("Invalid task token");

            if (status == UTaskStatus.Succeeded) return result;
            if (status == UTaskStatus.Faulted && exception != null)
            {
                exception.Throw();
            }
            throw new InvalidOperationException($"Task is in {status} state");
        }

        public void OnCompleted(Action<object> continuation, object state, short token)
        {
            if (token != version) throw new InvalidOperationException("Invalid task token");

            if (status.IsCompleted())
            {
                continuation(state);
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

        public bool TrySetResult(T result)
        {
            if (status.IsCompleted()) return false;

            this.result = result;
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

            UTaskScheduler.Schedule(() => cont(state));
        }

        public void Reset()
        {
            status = UTaskStatus.Pending;
            exception = null;
            continuation = null;
            continuationState = null;
            result = default;
            version++;
        }
    }
}