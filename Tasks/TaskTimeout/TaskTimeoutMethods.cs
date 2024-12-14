using System;
using System.Threading;
using System.Threading.Tasks;
using Damntry.Utils.Logging;
using Damntry.Utils.Tasks.AsyncDelay;

namespace Damntry.Utils.Tasks.TaskTimeout {

	internal class TaskTimeoutMethods<T> where T : AsyncDelayBase<T> {

		internal static async Task<bool> AwaitTaskWithTimeoutAsync(Task task, string taskLogName, int maxStopTimeMillis, bool throwTimeoutException) {
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
				await Task.WhenAny(task, AsyncDelayBase<T>.Instance.Delay(maxStopTimeMillis, cancelToken));
			} catch (Exception e) {
				if (e is TaskCanceledException || e is OperationCanceledException) {
					TimeLogger.Logger.LogTimeDebugFunc(() => $"Task \"{taskLogName}\" successfully canceled.", TimeLogger.LogCategories.Task);
				} else {
					throw;
				}
			}
		}

	}
}
