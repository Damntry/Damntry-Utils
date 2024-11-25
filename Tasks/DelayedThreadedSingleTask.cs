using System;
using System.Threading;
using System.Threading.Tasks;
using Damntry.Utils.Logging;

namespace Damntry.Utils.Tasks {

	/// <summary>
	/// Starts a threaded task after a configurable delay. If a new call to start 
	/// this task happens while the previous one has not yet been executed, the
	/// previous task is cancelled and the delay starts over again to execute the task.
	/// Its meant so a task cant be spammed, and will only execute if it hasnt 
	/// been called for the specified delay duration.
	/// </summary>
	public class DelayedThreadedSingleTask {

		private Task delayedTask;

		private CancellationTokenSource taskCancel;

		private Action actionTask;


		public DelayedThreadedSingleTask(Action actionTask) {
			if (actionTask == null) {
				throw new ArgumentNullException(nameof(actionTask));
			}

			this.actionTask = actionTask;
			taskCancel = new CancellationTokenSource();
		}


		public async void Start(int delayMillis) {
			if (delayedTask != null && !delayedTask.IsCompleted) {
				//Task is already ongoing. Cancel and wait for it to end.
				taskCancel.Cancel();
				try {
					await delayedTask;
				} catch (TaskCanceledException) { } //Expected. Eat it up.

				taskCancel = new CancellationTokenSource();
			}
			 
			StartDelayedCancellableTask(delayMillis);
		}

		private void StartDelayedCancellableTask(int delayMillis) {
			delayedTask = Task.Run(async () => {
				await Task.Delay(delayMillis, taskCancel.Token);

				if (!taskCancel.IsCancellationRequested) {
					try {
						actionTask();
					} catch (Exception ex) {
						GlobalConfig.TimeLoggerLog.LogTimeExceptionWithMessage("Exception while executing delayed task.", ex, TimeLoggerBase.LogCategories.Task);
						throw;
					}
				}

			}, taskCancel.Token);
		}

	}
}
