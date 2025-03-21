using System;
using System.Threading;
using System.Threading.Tasks;
using Damntry.Utils.Logging;
using Damntry.Utils.Tasks.AsyncDelay;

namespace Damntry.Utils.Tasks.TaskTimeout {

	public class TaskTimeout<T> where T : AsyncDelayBase<T> {

		/// <summary>
		/// Starts a cancellable task asynchronously, and returns its task to wait for its completion.
		/// If it takes longer to finish than the time passed through parameter, it will signal to cancel
		/// the worker function, waits for it to end, and throws a TimeoutException.
		/// </summary>
		/// <param name="asyncWorkerFunction">
		/// Async method to run. It must have a CancellationToken argument that is checked to exit the function, for when the timeout triggers.
		/// You can pass a variable number of parameters using Lambdas.
		/// Example:   (cancelToken) => AsyncWorkMethod(isXValidMethod, cancelToken, "23")
		/// </param>
		/// <param name="taskLogName">Descriptive name of the task to show in logs and exceptions.</param>
		/// <param name="maxCompletionTimeMillis">Milliseconds to wait for the task to finish. If it doesnt after this time, a TimeoutException will be thrown.</param>
		/// <exception cref="TimeoutException">Thrown when the task took longer than maxCompletionTimeMillis.</exception>
		/// <exception cref="Exception">Cancellation related exceptions are controlled and consumed automatically. Any other exceptions keep propagating.</exception>
		public static async Task StartAwaitableTaskWithTimeoutAsync(Func<CancellationToken, Task> asyncWorkerFunction, string taskLogName, int maxCompletionTimeMillis) {
			await StartTaskStaticAndWaitWithTimeoutAsync(asyncWorkerFunction, taskLogName, newThread: false, maxCompletionTimeMillis);
		}

		/// <summary>
		/// Starts a cancellable task asynchronously, in the thread pool, and returns its task to wait for its completion.
		/// If it takes longer to finish than the time passed through parameter, it will signal to cancel
		/// the worker function, waits for it to end, and throws a TimeoutException.
		/// </summary>
		/// Throws a TimeoutException if it doesnt finish in time.</summary>
		/// <param name="asyncWorkerFunction">
		/// Async method to run. It must have a CancellationToken argument that is checked to exit the function, for when the timeout triggers.
		/// You can pass a variable number of parameters using Lambdas.
		/// Example:   (cancelToken) => AsyncWorkMethod(isXValidMethod, cancelToken, "23")
		/// </param>
		/// <param name="taskLogName">Descriptive name of the task to show in logs and exceptions.</param>
		/// <param name="maxCompletionTimeMillis">Milliseconds to wait for the task to finish. If it doesnt after this time, a TimeoutException will be thrown.</param>
		/// <exception cref="TimeoutException">Thrown when the task took longer than maxCompletionTimeMillis.</exception>
		/// <exception cref="Exception">Cancellation related exceptions are controlled and consumed automatically. Any other exceptions keep propagating.</exception>
		public static async Task StartAwaitableThreadedTaskWithTimeoutAsync(Func<CancellationToken, Task> asyncWorkerFunction, string taskLogName, int maxCompletionTimeMillis) {
			await StartTaskStaticAndWaitWithTimeoutAsync(asyncWorkerFunction, taskLogName, newThread: true, maxCompletionTimeMillis);
		}

		private static async Task StartTaskStaticAndWaitWithTimeoutAsync(Func<CancellationToken, Task> asyncWorkerFunction, string taskLogName, bool newThread, int maxCompletionTimeMillis) {
			TimeLogger.Logger.LogTimeDebugFunc(() => $"Task \"{taskLogName}\" is now going to run {(newThread ? "in a new thread" : "asynchronously")}.", LogCategories.Task);

			Task workTask;
			CancellationTokenSource cancelWorker = new CancellationTokenSource();
			if (newThread) {
				workTask = Task.Run(() => asyncWorkerFunction(cancelWorker.Token));
			} else {
				workTask = asyncWorkerFunction(cancelWorker.Token);
			}

			bool timeoutOk = await TaskTimeoutMethods<T>.AwaitTaskWithTimeoutAsync(workTask, taskLogName, maxCompletionTimeMillis, throwTimeoutException: false);

			if (!timeoutOk) {
				cancelWorker.Cancel();
				await workTask; //If even after all of that, it still gets stuck here, its not my problem anymore.

				throw new TimeoutException($"Task \"{taskLogName}\" is finished but it took longer than " + maxCompletionTimeMillis + "ms.");
			}
		}

	}
}
