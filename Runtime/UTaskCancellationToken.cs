using System;
using System.Collections.Generic;

namespace THEBADDEST.Tasks
{
    public readonly struct UTaskCancellationToken
    {
        private readonly UTaskCancellationTokenSource source;

        internal UTaskCancellationToken(UTaskCancellationTokenSource source)
        {
            this.source = source;
        }

        public bool IsCancellationRequested => source?.IsCancellationRequested ?? false;

        public bool CanBeCanceled => source != null;

        public void ThrowIfCancellationRequested()
        {
            if (IsCancellationRequested)
                throw new OperationCanceledException();
        }

        public UTaskCancellationRegistration Register(Action callback)
        {
            if (source == null)
                return new UTaskCancellationRegistration();

            return source.Register(callback);
        }

        public static UTaskCancellationToken None => new UTaskCancellationToken(null);
    }

    public class UTaskCancellationTokenSource : IDisposable
    {
        private bool disposed;
        private bool cancellationRequested;
        private readonly List<Action> registeredCallbacks;
        private readonly object lockObject = new object();

        public UTaskCancellationTokenSource()
        {
            registeredCallbacks = new List<Action>();
        }

        public UTaskCancellationToken Token => new UTaskCancellationToken(this);

        public bool IsCancellationRequested
        {
            get
            {
                ThrowIfDisposed();
                return cancellationRequested;
            }
        }

        public void Cancel()
        {
            ThrowIfDisposed();

            bool shouldNotify = false;
            Action[] callbacks = null;

            lock (lockObject)
            {
                if (!cancellationRequested)
                {
                    cancellationRequested = true;
                    shouldNotify = true;
                    callbacks = registeredCallbacks.ToArray();
                    registeredCallbacks.Clear();
                }
            }

            if (shouldNotify && callbacks != null)
            {
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback();
                    }
                    catch (Exception)
                    {
                        // Swallow exceptions from callbacks
                    }
                }
            }
        }

        internal UTaskCancellationRegistration Register(Action callback)
        {
            ThrowIfDisposed();

            if (callback == null)
                throw new ArgumentNullException(nameof(callback));

            bool shouldInvokeImmediately = false;
            lock (lockObject)
            {
                if (cancellationRequested)
                {
                    shouldInvokeImmediately = true;
                }
                else
                {
                    registeredCallbacks.Add(callback);
                }
            }

            if (shouldInvokeImmediately)
            {
                callback();
            }

            return new UTaskCancellationRegistration(this, callback);
        }

        internal void Unregister(Action callback)
        {
            if (disposed || callback == null)
                return;

            lock (lockObject)
            {
                registeredCallbacks.Remove(callback);
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(UTaskCancellationTokenSource));
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;
            lock (lockObject)
            {
                registeredCallbacks.Clear();
            }
        }
    }

    public readonly struct UTaskCancellationRegistration : IDisposable
    {
        private readonly UTaskCancellationTokenSource source;
        private readonly Action callback;

        internal UTaskCancellationRegistration(UTaskCancellationTokenSource source, Action callback)
        {
            this.source = source;
            this.callback = callback;
        }

        public void Dispose()
        {
            source?.Unregister(callback);
        }
    }
}