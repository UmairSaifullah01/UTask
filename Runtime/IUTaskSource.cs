using System;

namespace THEBADDEST.Tasks
{
    public interface IUTaskSource
    {
        UTaskStatus GetStatus(short token);
        void GetResult(short token);
        void OnCompleted(Action<object> continuation, object state, short token);
    }

    public interface IUTaskSource<out T> : IUTaskSource
    {
        new T GetResult(short token);
    }
}