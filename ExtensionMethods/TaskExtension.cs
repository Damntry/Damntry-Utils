using System;
using System.Threading;
using System.Threading.Tasks;
using Damntry.Utils.Logging;

namespace Damntry.Utils.ExtensionMethods {

	public static class TaskExtensionMethods {

		/// <summary>
		/// Return if the task has no more work to do.
		/// It consists of the status Completed, Cancelled, and Faulted.
		/// </summary>
		public static bool IsTaskEnded(this Task task) =>
			//IsCompleted already includes IsCancelled
			task.IsCompleted || task.IsFaulted;

		/// <summary>
		/// Hides the CS4014 warning, and indicates that we dont care if the task finishes or what its result is.
		/// Logs all exceptions it throws except "task canceled" type exceptions (TaskCanceledException and OperationCanceledException).
		/// </summary>
		/// <param name="category">The log category to use if an exception occurs.</param>
		public static async void FireAndForgetCancels(this Task task, LogCategories category, bool dismissCancelLog = false) {
			try {
				if (!task.IsCompleted || task.IsFaulted) {
					await task.ConfigureAwait(false);
				}
			} catch (Exception e) {
				if (e is TaskCanceledException || e is OperationCanceledException || e is ThreadAbortException) {
					if (!dismissCancelLog) {
						TimeLogger.Logger.LogTimeDebug("\"Fire and Forget\" task successfully canceled.", category);
					}
				} else {
					TimeLogger.Logger.LogTimeExceptionWithMessage("Error while awaiting \"Fire and Forget\" type of task:", e, category);
				}
			}
		}


		/// <summary>
		/// Hides the CS4014 warning, and indicates that we dont care if the task finishes or what its result is.
		/// Logs all exceptions it throws.
		/// </summary>
		/// <param name="category">The log category to use if an exception occurs.</param>
		public static async void FireAndForget(this Task task, LogCategories category) {
			try {
				if (!task.IsCompleted || task.IsFaulted) {
					await task.ConfigureAwait(false);
				}
			} catch (Exception e) {
				TimeLogger.Logger.LogTimeExceptionWithMessage("Error while awaiting \"Fire and Forget\" type of task:", e, category);
			}
		}

	}



}
