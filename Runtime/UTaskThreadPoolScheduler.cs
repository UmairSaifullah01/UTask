using System;
using System.Threading.Tasks;

namespace THEBADDEST.Tasks
{
    /// <summary>
    /// Schedules actions on the .NET ThreadPool for multithreaded execution.
    /// </summary>
    public static class UTaskThreadPoolScheduler
    {
        /// <summary>
        /// Schedules an action to run on a background thread using the thread pool.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        public static void Schedule(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            Task.Run(action);
        }
    }
}