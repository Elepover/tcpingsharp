using System;
using System.Threading;
using System.Threading.Tasks;

namespace TcpingSharp.Tcping
{
    internal static class Extensions
    {
        /// <summary>
        /// Wait for a task to complete asynchronously.<br />
        /// Internally, this method checks for status every 10ms.
        /// </summary>
        /// <param name="task">Task to wait for.</param>
        /// <param name="token"><see cref="CancellationToken"/> to cancel the waiting action.</param>
        /// <exception cref="System.AggregateException">Exception thrown while executing this task.</exception>
        /// <exception cref="TaskCanceledException">The task has been canceled.</exception>
        /// <returns></returns>
        public static async Task WaitAsync(this Task task, CancellationToken token)
        {
            while (!(task.IsCompleted || token.IsCancellationRequested))
            {
                await Task.Delay(10, token);
            }

            if (task.IsFaulted) throw task.Exception!;
        }

        /// <summary>
        /// Unwrap this <see cref="Exception"/>.
        /// </summary>
        /// <param name="exception">Exception to unwrap.</param>
        /// <returns></returns>
        public static Exception Unwrap(this Exception exception)
        {
            while (!(exception.InnerException is null))
            {
                exception = exception.InnerException;
            }

            return exception;
        }
    }
}
