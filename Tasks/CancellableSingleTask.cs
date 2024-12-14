using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Damntry.Utils.Logging;
using Damntry.Utils.Tasks.AsyncDelay;
using Damntry.Utils.Tasks.TaskTimeout;

namespace Damntry.Utils.Tasks {

	/// <summary>
	/// Simplifies the creation and cancellation of a task that can only run one at a time. That is, a new task cant be started if the old one is still running.
	/// </summary>
	public class CancellableSingleTask<T> where T : AsyncDelayBase<T> {

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


		public bool IsTaskRunning {
			get {
				return task != null && task.Status == TaskStatus.Running;
			}
		}

		public bool IsCancellationRequested {
			get {
				if (cancelTokenSource != null) {
					return cancelTokenSource.IsCancellationRequested;
				} else {
					return false;
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
		/// Example:   () => AsyncWorkMethod(isXValidMethod, "23")
		/// </param>
		/// <param name="taskLogName">Descriptive name of the task to show in logs and exceptions.</param>
		/// <param name="throwExceptionIfRunning">If a exception should be thrown if the task was previously started and is currently still running.</param>
		/// <remarks>
		/// Awaiting this call will only wait for the creation of the task, not the completion of the task itself.
		/// If you want to wait for completion, use the method StartAwaitableTaskAsync instead.
		/// <para/>
		/// Note that Start and Stop operations wait for each other, so if this call is awaited, it
		/// could potentially take a long time if it was recently stopped but hasnt finished yet.
		/// </remarks>
		public async Task StartTaskAsync(Func<Task> asyncWorkerFunction, string taskLogName, bool throwExceptionIfRunning) {
			await StartTaskAsync(asyncWorkerFunction, taskLogName, awaitTask: false, throwExceptionIfRunning, newThread: false);
		}

		/// <summary>Starts a cancellable task asynchronously, in the thread pool.</summary>
		/// <param name="asyncWorkerFunction">
		/// Async method to run. You can pass a variable number of parameters using Lambdas.
		/// Example:   () => AsyncWorkMethod(isXValidMethod, "23")
		/// </param>
		/// <param name="taskLogName">Descriptive name of the task to show in logs and exceptions.</param>
		/// <param name="throwExceptionIfRunning">If a exception should be thrown if the task was previously started and is currently still running.</param>
		/// <remarks>
		/// Awaiting this call will only wait for the creation of the task, not the completion of the task itself.
		/// If you want to wait for completion, use the method StartAwaitableThreadedTaskAsync instead.
		/// <para/>
		/// Note that Start and Stop operations wait for each other, so if this call is awaited, it
		/// could potentially take a long time if it was recently stopped but hasnt finished yet.
		/// </remarks>
		public async Task StartThreadedTaskAsync(Func<Task> asyncWorkerFunction, string taskLogName, bool throwExceptionIfRunning) {
			await StartTaskAsync(asyncWorkerFunction, taskLogName, awaitTask: false, throwExceptionIfRunning, newThread: true);
		}

		/// <summary>
		/// Starts a cancellable task asynchronously that can be awaited until completion.
		/// </summary>
		/// <param name="asyncWorkerFunction">
		/// Async method to run. You can pass a variable number of parameters using Lambdas.
		/// Example:   () => AsyncWorkMethod(isXValidMethod, "23")
		/// </param>
		/// <param name="taskLogName">Descriptive name of the task to show in logs and exceptions.</param>
		/// <param name="throwExceptionIfRunning">If a exception should be thrown if the task was previously started and is currently still running.</param>
		/// <remarks>
		/// Note that Start and Stop operations wait for each other, so if this call is awaited, it
		/// could potentially take a long time if it was recently stopped but hasnt finished yet.
		/// </remarks>
		public async Task StartAwaitableTaskAsync(Func<Task> asyncWorkerFunction, string taskLogName, bool throwExceptionIfRunning) {
			await StartTaskAsync(asyncWorkerFunction, taskLogName, awaitTask: true, throwExceptionIfRunning, newThread: false);
		}

		/// <summary>Starts a cancellable task asynchronously, in the thread pool, that can be awaited until completion</summary>
		/// <param name="asyncWorkerFunction">
		/// Async method to run. You can pass a variable number of parameters using Lambdas.
		/// Example:   () => AsyncWorkMethod(isXValidMethod, "23")
		/// </param>
		/// <param name="taskLogName">Descriptive name of the task to show in logs and exceptions.</param>
		/// <param name="throwExceptionIfRunning">If a exception should be thrown if the task was previously started and is currently still running.</param>
		/// <remarks>
		/// Note that Start and Stop operations wait for each other, so if this call is awaited, it
		/// could potentially take a long time if it was recently stopped but hasnt finished yet.
		/// </remarks>
		public async Task StartAwaitableThreadedTaskAsync(Func<Task> asyncWorkerFunction, string taskLogName, bool throwExceptionIfRunning) {
			await StartTaskAsync(asyncWorkerFunction, taskLogName, awaitTask: true, throwExceptionIfRunning, newThread: true);
		}

		

		private async Task StartTaskAsync(Func<Task> asyncWorkerFunction, string taskLogName, bool awaitTask, bool throwIfAlreadyRunning, bool newThread) {
			await GetSemaphoreLock();

			try {
				if (task != null && !task.IsCompleted) {
					if (throwIfAlreadyRunning) {
						throw new InvalidOperationException(GetTextAlreadyRunningTask(taskLogName));
					} else {
						TimeLogger.Logger.LogTimeDebugFunc(() => GetTextAlreadyRunningTask(taskLogName), TimeLogger.LogCategories.Task);
						return;
					}
				}
				this.taskLogName = taskLogName;

				cancelTokenSource = new CancellationTokenSource();

				TimeLogger.Logger.LogTimeDebugFunc(() => $"Task \"{taskLogName}\" is now going to run {(newThread ? "in a new thread" : "asynchronously")}.", TimeLogger.LogCategories.Task);
				if (newThread) {
					task = Task.Run(() => asyncWorkerFunction());
				} else {
					task = asyncWorkerFunction();
				}
			} finally {
				semaphoreLock.Release();
			}

			if (awaitTask) {
				await task;
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
		/// <exception cref="Exception">Cancellation related exceptions are controlled and consumed automatically. Any other exceptions keep propagating.</exception>
		public async Task StopTaskAndWaitAsync() {
			await StopTaskAndWaitAsync(null, null, -1);
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
						TimeLogger.Logger.LogTimeDebugFunc(() => $"Cant stop task \"{taskLogName}\". It was never started, or already stopped.", TimeLogger.LogCategories.Task);
						return;
					}

					if (cancelTokenSource != null && !task.IsCompleted) {
						TimeLogger.Logger.LogTimeDebugFunc(() => $"Canceling task \"{taskLogName}\"", TimeLogger.LogCategories.Task);

						cancelTokenSource.Cancel();
					} else {
						TimeLogger.Logger.LogTimeDebugFunc(() => $"Cant stop task \"{taskLogName}\". It is already finished.", TimeLogger.LogCategories.Task);
					}

					//Wait for task end, and control cancelled exception errors.
					try {
						await task;
					} catch (Exception e) {
						if (e is TaskCanceledException || e is OperationCanceledException) {
							TimeLogger.Logger.LogTimeDebugFunc(() => $"Task \"{taskLogName}\" successfully canceled.", TimeLogger.LogCategories.Task);
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

				await TaskTimeoutMethods<T>.AwaitTaskWithTimeoutAsync(stopTask, taskLogName, maxStopTimeMillis, throwTimeoutException: true);

			} finally {
				semaphoreLock.Release();
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
