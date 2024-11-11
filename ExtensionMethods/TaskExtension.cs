using System;
using System.Threading.Tasks;
using static Damntry.Utils.Logging.TimeLoggerBase;

namespace Damntry.Utils.ExtensionMethods {

	public static class TaskExtensionMethods {


		/// <summary>
		/// Hides the CS4014 warning, and indicates that we dont care if the task finishes or what its result is.
		/// Logs all exceptions it throws except "task canceled" type exceptions (TaskCanceledException and OperationCanceledException).
		/// </summary>
		/// <param name="category">The log category to use if an exception occurs.</param>
		public static async void FireAndForgetCancels(this Task task, LogCategories category) {
			try {
				if (!task.IsCompleted || task.IsFaulted) {
					await task.ConfigureAwait(false);
				}
			} catch (Exception e) {
				if (e is TaskCanceledException || e is OperationCanceledException) {
					GlobalConfig.TimeLoggerLog.LogTimeDebug("\"Fire and Forget\" task canceled.", category);
				} else {
					GlobalConfig.TimeLoggerLog.LogTimeExceptionWithMessage("Error while awaiting \"Fire and Forget\" type of task:", e, category);
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
				GlobalConfig.TimeLoggerLog.LogTimeExceptionWithMessage("Error while awaiting \"Fire and Forget\" type of task:", e, category);
			}
		}

	}



}
