using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Damntry.Utils.Logging;

namespace Damntry.Utils.Tasks {

	/// <summary>
	/// Simplifies the creation and cancellation of a task that can only run one at a time. That is, a new task cant be started if the old one is still running.
	/// </summary>
	public class CancellableSingleTask {

		/// <summary>
		/// Semaphore to lock Start/Stop actions from being executed at the same time. It can also be accessed from outside this class via accessibility methods.
		/// </summary>
		private SemaphoreSlim semaphoreLock;

		/// <summary>The task we are running.</summary>
		private Task task;

		/// <summary>Descriptive name of the task to show in logs and exceptions.</summary>
		private string taskLogName;

		private CancellationTokenSource cancelTokenSource;

		/// <summary>Indicates if the semaphore is being acquired from an outside source.</summary>
		private bool isSemaphoreAcquiredManually;

		/// <summary>
		/// The maximum expected amount of time to acquire the semaphore when used externally, in milliseconds.
		/// Any internal, aka Start or Stop, use of the semaphore that fails to acquire it after this time, while its
		/// being used externally, will throw an exception to signal that the external use of the semaphore went wrong.
		/// </summary>
		private int maxExternalSemaphoreAcquireTimeMillis;



		public bool IsCancellationRequested {
			get {
				if (cancelTokenSource != null) {
					return cancelTokenSource.IsCancellationRequested;
				} else {
					throw new InvalidOperationException("There is no cancellation token since no task has been started.");
				}
			}
		}

		public CancellationToken CancellationToken {
			get {
				if (cancelTokenSource != null) {
					return cancelTokenSource.Token;
				} else {
					throw new InvalidOperationException("There is no cancellation token since no task has been started.");
				}
			}
		}


		public CancellableSingleTask() {
			semaphoreLock = new SemaphoreSlim(1, 1);
			isSemaphoreAcquiredManually = false;
		}

		/// <summary>Starts a cancellable task asynchronously.</summary>
		/// <param name="asyncWorkerFunction">
		/// Async method to run. You can pass a variable number of parameters using Lambdas.
		/// Example:   () => AsyncWorkMethod(isXValid, "23")
		/// </param>
		/// <param name="taskLogName">Descriptive name of the task to show in logs and exceptions.</param>
		/// <param name="throwExceptionIfRunning">If a exception should be thrown if the task was previously started and is currently still running.</param>
		/// <remarks>
		/// Note that Start and Stop operations wait for each other so, if this call is awaited, it
		/// could potentially take a long time if it was recently stopped but hasnt finished yet.
		/// </remarks>
		public async Task StartTaskAsync(Func<Task> asyncWorkerFunction, string taskLogName, bool throwExceptionIfRunning) {
			await StartTaskAsync(asyncWorkerFunction, taskLogName, throwExceptionIfRunning, newThread: false);
		}

		/// <summary>Starts a cancellable task asynchronously, in a new thread.</summary>
		/// <param name="asyncWorkerFunction">
		/// Async method to run. You can pass a variable number of parameters using Lambdas.
		/// Example:   () => AsyncWorkMethod(isXValid, "23")
		/// </param>
		/// <param name="taskLogName">Descriptive name of the task to show in logs and exceptions.</param>
		/// <param name="throwExceptionIfRunning">If a exception should be thrown if the task was previously started and is currently still running.</param>
		/// <remarks>
		/// Note that Start and Stop operations wait for each other so, if this call is awaited, it
		/// could potentially take a long time if it was recently stopped but hasnt finished yet.
		/// </remarks>
		public async Task StartTaskNewThreadAsync(Func<Task> asyncWorkerFunction, string taskLogName, bool throwExceptionIfRunning) {
			await StartTaskAsync(asyncWorkerFunction, taskLogName, throwExceptionIfRunning, newThread: true);
		}

		/// <summary>
		/// Starts a cancellable task asynchronously, and waits for its completion. 
		/// If it takes longer to finish than the time passed through parameter, it calls to cancel
		/// the worker funcion, waits for it to end, and throws a TimeoutException.
		/// </summary>
		/// <param name="asyncWorkerFunction">
		/// Async method to run. It must have a CancellationToken argument that cancels the work done in the function.
		/// You can pass a variable number of parameters using Lambdas.
		/// Example:   (cancelToken) => AsyncWorkMethod(isXValid, cancelToken, "23")
		/// </param>
		/// <param name="taskLogName">Descriptive name of the task to show in logs and exceptions.</param>
		/// <param name="maxCompletionTimeMillis">Milliseconds to wait for the task to finish. If it doesnt after this time, a TimeoutException will be thrown.</param>
		/// <remarks>
		/// Note that Start and Stop operations wait for each other so, if this call is awaited, it
		/// could potentially take a long time if it was recently stopped but hasnt finished yet.
		/// </remarks>
		/// <exception cref="TimeoutException">Thrown when the task took longer than maxCompletionTimeMillis.</exception>
		/// <exception cref="Exception">Cancellation related exceptions are controlled and consumed automatically. Any other exceptions keep propagating.</exception>
		public static async Task StartTaskAndWaitAsync(Func<CancellationToken, Task> asyncWorkerFunction, string taskLogName, int maxCompletionTimeMillis) {
			await StartTaskStaticAndWaitAsync(asyncWorkerFunction, taskLogName, newThread: false, maxCompletionTimeMillis);
		}

		/// <summary>
		/// Starts a cancellable task asynchronously, in a new thread, and waits for its completion. 
		/// If it takes longer to finish than the time passed through parameter, it calls to cancel
		/// the worker funcion, waits for it to end, and throws a TimeoutException.
		/// </summary>
		/// Throws a TimeoutException if it doesnt finish in time.</summary>
		/// <param name="asyncWorkerFunction">
		/// Async method to run. It must have a CancellationToken argument that cancels the work done in the function.
		/// You can pass a variable number of parameters using Lambdas.
		/// </param>
		/// <param name="taskLogName">Descriptive name of the task to show in logs and exceptions.</param>
		/// <param name="maxCompletionTimeMillis">Milliseconds to wait for the task to finish. If it doesnt after this time, a TimeoutException will be thrown.</param>
		/// <remarks>
		/// Note that Start and Stop operations wait for each other so, if this call is awaited, it
		/// could potentially take a long time if it was recently stopped but hasnt finished yet.
		/// </remarks>
		/// <exception cref="TimeoutException">Thrown when the task takes longer than maxCompletionTimeMillis.</exception>
		/// <exception cref="Exception">Cancellation related exceptions are controlled and consumed automatically. Any other exceptions keep propagating.</exception>
		public static async Task StartTaskNewThreadAndWaitAsync(Func<CancellationToken, Task> asyncWorkerFunction, string taskLogName, int maxCompletionTimeMillis) {
			await StartTaskStaticAndWaitAsync(asyncWorkerFunction, taskLogName, newThread: true, maxCompletionTimeMillis);
		}

		private async Task StartTaskAsync(Func<Task> asyncWorkerFunction, string taskLogName, bool throwIfAlreadyRunning, bool newThread) {
			await GetSemaphoreLock();

			try {
				if (task != null && !task.IsCompleted) {
					if (throwIfAlreadyRunning) {
						throw new InvalidOperationException(GetTextAlreadyRunningTask(taskLogName));
					} else {
						GlobalConfig.TimeLoggerLog.LogTimeDebugFunc(() => GetTextAlreadyRunningTask(taskLogName), TimeLoggerBase.LogCategories.Task);
						return;
					}
				}
				this.taskLogName = taskLogName;

				cancelTokenSource = new CancellationTokenSource();

				GlobalConfig.TimeLoggerLog.LogTimeDebugFunc(() => $"Task \"{taskLogName}\" is now going to run {(newThread ? "in a new thread" : "asynchronously")}.", TimeLoggerBase.LogCategories.Task);
				if (newThread) {
					task = Task.Run(() => asyncWorkerFunction());
				} else {
					task = asyncWorkerFunction();
				}
			} finally {
				semaphoreLock.Release();
			}
		}


		private static async Task StartTaskStaticAndWaitAsync(Func<CancellationToken, Task> asyncWorkerFunction, string taskLogName, bool newThread, int maxCompletionTimeMillis) {
			GlobalConfig.TimeLoggerLog.LogTimeDebugFunc(() => $"Task \"{taskLogName}\" is now going to run {(newThread ? "in a new thread" : "asynchronously")}.", TimeLoggerBase.LogCategories.Task);

			Task workTask;
			CancellationTokenSource cancelWorker = new CancellationTokenSource();
			if (newThread) {
				workTask = Task.Run(() => asyncWorkerFunction(cancelWorker.Token));
			} else {
				workTask = asyncWorkerFunction(cancelWorker.Token);
			}

			bool timeoutOk = await AwaitTaskWithTimeoutAsync(workTask, taskLogName, maxCompletionTimeMillis, throwTimeoutException: false);

			if (!timeoutOk) {
				cancelWorker.Cancel();
				await workTask; //If even after all of that, it still gets stuck here, its not my problem anymore.

				throw new TimeoutException($"Task \"{taskLogName}\" is finished but it took longer than " + maxCompletionTimeMillis + "ms.");
			}
		}

		private string GetTextAlreadyRunningTask(string taskLogName) {
			StringBuilder sbBuilder = new StringBuilder(25);

			if (this.taskLogName != taskLogName) {
				sbBuilder.Append("Cant start task \"");
				sbBuilder.Append(taskLogName);
				sbBuilder.Append("\": ");
			}
			sbBuilder.Append("Task \"");
			sbBuilder.Append(this.taskLogName);
			sbBuilder.Append("\" is already running.");

			return sbBuilder.ToString();
		}

		/// <summary>Stops the task and waits until it is finished.</summary>
		/// <param name="maxStopTimeMillis">
		/// The maximum expected amount of time that takes to stop this task, in milliseconds.
		/// If it takes longer than this, a TimeoutException will be thrown to signal that something went wrong.
		/// </param>
		/// <exception cref="TimeoutException">Thrown when stopping the task takes longer than maxStopTimeMillis.</exception>
		/// <exception cref="Exception">Cancellation related exceptions are controlled and consumed automatically. Any other exceptions keep propagating.</exception>
		public async Task StopTaskAndWaitAsync(int maxStopTimeMillis) {
			await StopTaskAndWaitAsync(null, null, maxStopTimeMillis);
		}

		/// <summary>Stops the task and waits until it is finished.</summary>
		/// <param name="onTaskStopped">Method that will be called after the task is stopped, but before releasing the semaphore.</param>
		/// <param name="maxStopTimeMillis">
		/// The maximum expected amount of time that takes to both stop this task and execute the Action argument, in milliseconds.
		/// If it takes longer than this, a TimeoutException will be thrown to signal that something went wrong.
		/// </param>
		/// <exception cref="TimeoutException">Thrown when stopping the task and executing the Action argument takes longer than maxStopTimeMillis.</exception>
		/// <exception cref="Exception">Cancellation related exceptions are controlled and consumed automatically. Any other exceptions keep propagating.</exception>
		public async Task StopTaskAndWaitAsync(Action onTaskStopped, int maxStopTimeMillis) {
			await StopTaskAndWaitAsync(null, null, maxStopTimeMillis);
		}

		/// <summary>Stops the task and waits until it is finished.</summary>
		/// <param name="onTaskStoppedAsync">Async method that will be called and awaited after the task is stopped, but before releasing the semaphore.</param>
		/// <param name="maxStopTimeMillis">
		/// The maximum expected amount of time that takes to both stop this task and execute the Func<Task> argument, in milliseconds.
		/// If it takes longer than this, a TimeoutException will be thrown to signal that something went wrong.
		/// </param>
		/// <exception cref="TimeoutException">Thrown when stopping the task and executing the Func<Task> argument takes longer than maxStopTimeMillis.</exception>
		/// <exception cref="Exception">Cancellation related exceptions are controlled and consumed automatically. Any other exceptions keep propagating.</exception>
		public async Task StopTaskAndWaitAsync(Func<Task> onTaskStoppedAsync, int maxStopTimeMillis) {
			await StopTaskAndWaitAsync(null, null, maxStopTimeMillis);
		}

		private async Task StopTaskAndWaitAsync(Action onTaskStopped, Func<Task> onTaskStoppedAsync, int maxStopTimeMillis) {

			await GetSemaphoreLock();

			try {
				Task stopTask = new Task(async () => {

					if (task == null) {
						GlobalConfig.TimeLoggerLog.LogTimeDebugFunc(() => $"Cant stop task \"{taskLogName}\". It was never started, or already stopped.", TimeLoggerBase.LogCategories.Task);
						return;
					}

					if (cancelTokenSource != null && !task.IsCompleted) {
						GlobalConfig.TimeLoggerLog.LogTimeDebugFunc(() => $"Canceling task \"{taskLogName}\"", TimeLoggerBase.LogCategories.Task);

						cancelTokenSource.Cancel();
					} else {
						GlobalConfig.TimeLoggerLog.LogTimeDebugFunc(() => $"Cant stop task \"{taskLogName}\". It is already finished.", TimeLoggerBase.LogCategories.Task);
					}

					//Wait for task end, and control cancelled exception errors.
					try {
						await task;
					} catch (Exception e) {
						if (e is TaskCanceledException || e is OperationCanceledException) {
							GlobalConfig.TimeLoggerLog.LogTimeDebugFunc(() => $"Task \"{taskLogName}\" successfully canceled.", TimeLoggerBase.LogCategories.Task);
						} else {
							throw;
						}
					}

					task.Dispose();
					task = null;

					//Execute argument methods if any
					if (onTaskStopped != null) {
						onTaskStopped();
					}
					if (onTaskStoppedAsync != null) {
						await onTaskStoppedAsync();
					}

				});

				stopTask.Start();

				await AwaitTaskWithTimeoutAsync(stopTask, taskLogName, maxStopTimeMillis, throwTimeoutException: true);

			} finally {
				semaphoreLock.Release();
			}
		}

		private static async Task<bool> AwaitTaskWithTimeoutAsync(Task task, string taskLogName, int maxStopTimeMillis, bool throwTimeoutException) {
			CancellationTokenSource cancelDelay = new CancellationTokenSource();

			try {
				await AwaitTaskWithTimeoutAsync(task, taskLogName, maxStopTimeMillis, cancelDelay.Token);

				if (!task.IsCompleted) {
					if (throwTimeoutException) {
						throw new TimeoutException($"Task \"{taskLogName}\" took longer than the specified {maxStopTimeMillis} ms to stop.");
					} else {
						return false;
					}
				}

				return true;
			} finally {
				cancelDelay.Cancel();
			}
		}

		private static async Task AwaitTaskWithTimeoutAsync(Task task, string taskLogName, int maxStopTimeMillis, CancellationToken cancelToken) {
			try {
				await Task.WhenAny(task, Task.Delay(maxStopTimeMillis, cancelToken));
			} catch (Exception e) {
				if (e is TaskCanceledException || e is OperationCanceledException) {
					GlobalConfig.TimeLoggerLog.LogTimeDebugFunc(() => $"Task \"{taskLogName}\" successfully canceled.", TimeLoggerBase.LogCategories.Task);
				} else {
					throw;
				}
			}
		}

		private async Task GetSemaphoreLock() {
			int timeout = -1;
			if (isSemaphoreAcquiredManually) {
				timeout = maxExternalSemaphoreAcquireTimeMillis;
			}

			bool lockAcquired = await semaphoreLock.WaitAsync(timeout);

			if (!lockAcquired) {
				throw new ExternalSemaphoreHeldException("The operation could not complete because the semaphore was held externally for too long.");
			}
		}


		/// <summary>
		/// Waits for the internal semaphore, used to start and stop the task, to be free, and acquires its lock.
		/// This is only useful in very specific scenarios. Like in a multi-threading use case where the Start and
		/// Stop may be called at any moment, but you want to hold them until you process the results of the Action/Func
		/// executed on the Stop.
		/// </summary>
		/// <param name="maxSemaphoreAcquireTimeMillis">
		/// The acceptable amount of time that we expect to own this semaphore, in milliseconds.
		/// Any calls to this Start or Stop instance that fail to acquire the semaphore after this time, will throw
		/// a ExternalSemaphoreHeldException to signal that this external use of the semaphore, held it for too long.
		/// </param>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the semaphore is already acquired from a previous call to this method. 
		/// To avoid this make sure to call ReleaseSemaphore after you are finished.
		/// </exception>
		public async Task WaitSemaphoreAsync(int maxSemaphoreAcquireTimeMillis) {
			await WaitSemaphoreTimeoutAsync(-1, maxSemaphoreAcquireTimeMillis);
		}


		/// <summary>
		/// Waits for the internal semaphore, used to start and stop the task, to be free, and acquires its lock.
		/// This is only useful in very specific scenarios. Like in a multi-threading use case where the Start and
		/// Stop may be called at any moment, but you want to hold them until you process the results of the Action/Func
		/// executed on the Stop.
		/// </summary>
		/// <param name="millisecondTimeout">Time in milliseconds to wait to acquire the semaphore lock. After the time passes, returns false.</param>
		/// <param name="maxSemaphoreAcquireTimeMillis">
		/// The acceptable amount of time that we expect to own this semaphore, in milliseconds.
		/// Any calls to this Start or Stop instance that fail to acquire the semaphore after this time, will throw
		/// a ExternalSemaphoreHeldException to signal that this external use of the semaphore, held it for too long.
		/// </param>
		/// <returns>True is the async was successfully acquired. False if the wait reached the timeout time.</returns>
		/// <exception cref="InvalidOperationException">
		/// Thrown if the semaphore is already acquired from a previous call to this method. 
		/// To avoid this make sure to call ReleaseSemaphore after you are finished.
		/// </exception>
		/// <remarks></remarks>
		public async Task<bool> WaitSemaphoreTimeoutAsync(int millisecondTimeout, int maxSemaphoreAcquireTimeMillis) {
			if (isSemaphoreAcquiredManually) {
				throw new InvalidOperationException("Semaphore lock has already been manually acquired. You must release it first by calling ReleaseSemaphore().");
			}
			//TODO Global 6 - All current uses of maxExternalSemaphoreAcquireTimeMillis and isSemaphoreAcquiredManually are not threadsafe, since im not locking shit.
			//	I think a new second semaphore, acquired before changing or reading any of those 2 vars, and released after semaphoreLock is acquired, would work.
			maxExternalSemaphoreAcquireTimeMillis = maxSemaphoreAcquireTimeMillis;

			isSemaphoreAcquiredManually = await semaphoreLock.WaitAsync(millisecondTimeout);

			return isSemaphoreAcquiredManually;
		}

		/// <summary>Releases the semaphore acquired manually with WaitSemaphoreAsync or WaitSemaphoreTimeoutAsync.</summary>
		/// <exception cref="InvalidOperationException">Thrown when no lock was previously manually acquired.</exception>
		public void ReleaseSemaphore() {
			if (isSemaphoreAcquiredManually) {
				isSemaphoreAcquiredManually = false;
				semaphoreLock.Release();
			} else {
				throw new InvalidOperationException("No lock for the semaphore was acquired manually. Call WaitSemaphoreAsync() first.");
			}

		}


	}

	public class ExternalSemaphoreHeldException : Exception {

		public ExternalSemaphoreHeldException() : base() { }
		public ExternalSemaphoreHeldException(string message) : base(message) { }
		public ExternalSemaphoreHeldException(string message, Exception inner) : base(message, inner) { }

	}

}
