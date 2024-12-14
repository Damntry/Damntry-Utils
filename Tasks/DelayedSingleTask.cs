using System;
using System.Threading;
using System.Threading.Tasks;
using Damntry.Utils.ExtensionMethods;
using Damntry.Utils.Logging;
using Damntry.Utils.Tasks.AsyncDelay;

namespace Damntry.Utils.Tasks {

	//TODO Global 5 - Add a way of making it work so it just ignores repeated calls in the period since the first one was made.
	//		So as an example, if its being called constantly, and the period is set too 500, it would take the first call, wait
	//		500ms while ignoring every other call, and then do the Action. Then the cycle repeats.

	/// <summary>
	/// Starts a task after a configurable delay. If a new call to start this 
	/// task happens while the delay of the previous one has not finished yet, the
	/// previous task is cancelled and the delay starts over again to execute the task.
	/// Its meant so a task cant be spammed, and will only execute if it hasnt 
	/// been called for the specified delay duration.
	/// </summary>
	public class DelayedSingleTask<T> where T : AsyncDelayBase<T> {

		private Task delayedTask;

		private CancellationTokenSource taskCancel;

		private Action actionTask;


		public DelayedSingleTask(Action actionTask) {
			if (actionTask == null) {
				throw new ArgumentNullException(nameof(actionTask));
			}

			this.actionTask = actionTask;
			taskCancel = new CancellationTokenSource();
		}

		public async void Start(int delayMillis) {
			if (delayedTask != null && !delayedTask.IsCompleted && !delayedTask.IsCanceled) {
				//Task is already ongoing. Cancel and wait for it to end.
				taskCancel.Cancel();

				try {
					await delayedTask;
				} catch (TaskCanceledException) {
					//Expected. Eat it up.
				} catch (Exception ex) {
					TimeLogger.Logger.LogTimeExceptionWithMessage("Exception while starting and executing delayed task.", ex, TimeLogger.LogCategories.Task);
				}

				taskCancel = new CancellationTokenSource();
			}

			delayedTask = StartDelayedCancellableTask(delayMillis);

			//Process it to log any possible exceptions
			delayedTask.FireAndForgetCancels(TimeLogger.LogCategories.Task, true);
		}

		private async Task StartDelayedCancellableTask(int delayMillis) {
			await AsyncDelayBase<T>.Instance.Delay(delayMillis, taskCancel.Token);

			if (!taskCancel.IsCancellationRequested) {
				actionTask();
			}
		}

	}
}
