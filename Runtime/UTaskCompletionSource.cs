using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using UnityEngine;

namespace THEBADDEST.Tasks
{
    public class UTaskCompletionSource : IUTaskSource
    {
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
                continuation(state);
            }
            else
            {
                this.continuation = continuation;
                this.continuationState = state;
            }
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

            // Execute continuation immediately if we're on the main thread
            if (Application.isPlaying && Thread.CurrentThread.ManagedThreadId == 1)
            {
                cont(state);
            }
            else
            {
                UTaskScheduler.Schedule(() => cont(state));
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
}