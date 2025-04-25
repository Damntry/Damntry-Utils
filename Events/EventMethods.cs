using System;
using Damntry.Utils.Logging;

namespace Damntry.Utils.Events {

	public static class EventMethods {


		/// <summary>
		/// Invokes each of the methods attached to this event, if any. If a method throws 
		/// an error, the exception is catched, logged and we continue with the next method.
		/// </summary>
		/// <param name="ev">The event instance.</param>
		/// <returns>True if all methods succeded without exceptions.</returns>
		/// <remarks>
		/// This method is slow as it has to invoke each subscription through reflection.
		/// Only use when really necessary, in the rare cases where we know that 
		/// a partial execution is desirable and should not stop execution.
		/// </remarks>
		public static bool TryTriggerEvents(Action ev) {
			bool allOk = true;
			if (ev != null) {
				foreach (Action del in ev.GetInvocationList()) {
					try {
						del.Invoke();
					} catch (Exception e) {
						allOk = false;
						TimeLogger.Logger.LogTimeExceptionWithMessage($"Exception while invoking method \"{del.Method.Name}\" " +
							$"from event {ev.GetType()}", e, LogCategories.Events);
					}
				}
			}

			return allOk;
		}

		//TODO Global 5 - Create all the Tx with templating up to 10 parameters or so.

		/// <summary>
		/// Invokes each of the methods attached to this event, if any. If a method throws 
		/// an error, the exception is catched, logged and we continue with the next method.
		/// </summary>
		/// <param name="ev">The event instance.</param>
		/// <param name="arg1">The first argument of the event.</param>
		/// <returns>True if all methods succeded without exceptions.</returns>
		/// <remarks>
		/// This method is slow as it has to invoke each subscription through reflection.
		/// Only use when really necessary, in the rare cases where we know that 
		/// a partial execution is desirable and should not stop execution.
		/// </remarks>
		public static bool TryTriggerEvents<T>(Action<T> ev, T arg1) {
			bool allOk = true;
			if (ev != null) {
				foreach (Action<T> del in ev.GetInvocationList()) {
					try {
						del.Invoke(arg1);
					} catch (Exception e) {
						allOk = false;
						TimeLogger.Logger.LogTimeExceptionWithMessage($"Exception while invoking method \"{del.Method.Name}\" " +
							$"from event {ev.GetType()}", e, LogCategories.Events);
					}
				}
			}

			return allOk;
		}

		/// <summary>
		/// Invokes each of the methods attached to this event, if any. If a method throws 
		/// an error, the exception is catched, logged and we continue with the next method.
		/// </summary>
		/// <param name="ev">The event instance.</param>
		/// <param name="arg1">The first argument of the event.</param>
		/// <param name="arg2">The second argument of the event.</param>
		/// <returns>True if all methods succeded without exceptions.</returns>
		/// <remarks>
		/// This method is slow as it has to invoke each subscription through reflection.
		/// Only use when really necessary, in the rare cases where we know that 
		/// a partial execution is desirable and should not stop execution.
		/// </remarks>
		public static bool TryTriggerEvents<T1, T2>(Action<T1, T2> ev, T1 arg1, T2 arg2) {
			bool allOk = true;
			if (ev != null) {
				foreach (Action<T1, T2> del in ev.GetInvocationList()) {
					try {
						del.Invoke(arg1, arg2);
					} catch (Exception e) {
						allOk = false;
						TimeLogger.Logger.LogTimeExceptionWithMessage($"Exception while invoking method \"{del.Method.Name}\" " +
							$"from event {ev.GetType()}", e, LogCategories.Events);
					}
				}
			}

			return allOk;
		}

		/// <summary>
		/// Invokes each of the methods attached to this event, if any. If a method throws 
		/// an error, the exception is catched, logged and we continue with the next method.
		/// </summary>
		/// <param name="ev">The event instance.</param>
		/// <param name="arg1">The first argument of the event./param>
		/// <param name="arg2">The second argument of the event.</param>
		/// <param name="arg3">The third argument of the event.</param>
		/// <returns>True if all methods succeded without exceptions.</returns>
		/// <remarks>
		/// This method is slow as it has to invoke each subscription through reflection.
		/// Only use when really necessary, in the rare cases where we know that 
		/// a partial execution is desirable and should not stop execution.
		/// </remarks>
		public static bool TryTriggerEvents<T1, T2, T3>(Action<T1, T2> ev, T1 arg1, T2 arg2, T3 arg3) {
			bool allOk = true;
			if (ev != null) {
				foreach (Action<T1, T2, T3> del in ev.GetInvocationList()) {
					try {
						del.Invoke(arg1, arg2, arg3);
					} catch (Exception e) {
						allOk = false;
						TimeLogger.Logger.LogTimeExceptionWithMessage($"Exception while invoking method \"{del.Method.Name}\" " +
							$"from event {ev.GetType()}", e, LogCategories.Events);
					}
				}
			}

			return allOk;
		}

		/// <summary>
		/// Invokes each of the methods attached to this event, if any. If a method throws 
		/// an error, the exception is catched, logged and we continue with the next method.
		/// </summary>
		/// <param name="ev">The event instance.</param>
		/// <param name="arg1">The first argument of the event./param>
		/// <param name="arg2">The second argument of the event.</param>
		/// <param name="arg3">The third argument of the event.</param>
		/// <param name="arg4">The fourth argument of the event.</param>
		/// <returns>True if all methods succeded without exceptions.</returns>
		/// <remarks>
		/// This method is slow as it has to invoke each subscription through reflection.
		/// Only use when really necessary, in the rare cases where we know that 
		/// a partial execution is desirable and should not stop execution.
		/// </remarks>
		public static bool TryTriggerEvents<T1, T2, T3, T4>(Action<T1, T2> ev, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
			bool allOk = true;
			if (ev != null) {
				foreach (Action<T1, T2, T3, T4> del in ev.GetInvocationList()) {
					try {
						del.Invoke(arg1, arg2, arg3, arg4);
					} catch (Exception e) {
						allOk = false;
						TimeLogger.Logger.LogTimeExceptionWithMessage($"Exception while invoking method \"{del.Method.Name}\" " +
							$"from event {ev.GetType()}", e, LogCategories.Events);
					}
				}
			}

			return allOk;
		}

		/// <summary>
		/// Invokes each of the methods attached to this event, if any. If a method throws 
		/// an error, the exception is catched, logged and we continue with the next method.
		/// </summary>
		/// <param name="ev">The event instance.</param>
		/// <param name="arg1">The first argument of the event./param>
		/// <param name="arg2">The second argument of the event.</param>
		/// <param name="arg3">The third argument of the event.</param>
		/// <param name="arg4">The fourth argument of the event.</param>
		/// <param name="arg5">The fifth argument of the event.</param>
		/// <returns>True if all methods succeded without exceptions.</returns>
		/// <remarks>
		/// This method is slow as it has to invoke each subscription through reflection.
		/// Only use when really necessary, in the rare cases where we know that 
		/// a partial execution is desirable and should not stop execution.
		/// </remarks>
		public static bool TryTriggerEvents<T1, T2, T3, T4, T5>(Action<T1, T2> ev, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) {
			bool allOk = true;
			if (ev != null) {
				foreach (Action<T1, T2, T3, T4, T5> del in ev.GetInvocationList()) {
					try {
						del.Invoke(arg1, arg2, arg3, arg4, arg5);
					} catch (Exception e) {
						allOk = false;
						TimeLogger.Logger.LogTimeExceptionWithMessage($"Exception while invoking method \"{del.Method.Name}\" " +
							$"from event {ev.GetType()}", e, LogCategories.Events);
					}
				}
			}

			return allOk;
		}

		/// <summary>
		/// Invokes each of the methods attached to this event, if any. If a method throws 
		/// an error, the exception is catched, logged and we continue with the next method.
		/// </summary>
		/// <param name="ev">The event instance.</param>
		/// <param name="arg1">The first argument of the event./param>
		/// <param name="arg2">The second argument of the event.</param>
		/// <param name="arg3">The third argument of the event.</param>
		/// <param name="arg4">The fourth argument of the event.</param>
		/// <param name="arg5">The fifth argument of the event.</param>
		/// <param name="arg6">The sixth argument of the event.</param>
		/// <returns>True if all methods succeded without exceptions.</returns>
		/// <remarks>
		/// This method is slow as it has to invoke each subscription through reflection.
		/// Only use when really necessary, in the rare cases where we know that 
		/// a partial execution is desirable and should not stop execution.
		/// </remarks>
		public static bool TryTriggerEvents<T1, T2, T3, T4, T5, T6>(Action<T1, T2> ev, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) {
			bool allOk = true;
			if (ev != null) {
				foreach (Action<T1, T2, T3, T4, T5, T6> del in ev.GetInvocationList()) {
					try {
						del.Invoke(arg1, arg2, arg3, arg4, arg5, arg6);
					} catch (Exception e) {
						allOk = false;
						TimeLogger.Logger.LogTimeExceptionWithMessage($"Exception while invoking method \"{del.Method.Name}\" " +
							$"from event {ev.GetType()}", e, LogCategories.Events);
					}
				}
			}

			return allOk;
		}



		[Obsolete("Barely faster than TryTriggerEventsDelegate. Use TryTriggerEvents instead.")]
		private static bool TryTriggerEventsLambda<T>(Action<T> ev, T arg1) {
			return TryTriggerEventsLambda(ev, (Delegate del) => {
				((Action<T>)del).Invoke(arg1);
			});
		}

		/// <summary>
		/// Created to reduce the code duplication of having multiple TryTriggerEvents
		/// methods, but it is 5 times slower in benchmark tests. Not really worth it.
		/// </summary>
		private static bool TryTriggerEventsLambda(Delegate ev, Action<Delegate> action) {
			bool allOk = true;
			if (ev != null) {
				foreach (Delegate del in ev.GetInvocationList()) {
					try {
						action(del);
					} catch (Exception e) {
						allOk = false;
						TimeLogger.Logger.LogTimeExceptionWithMessage($"Exception while invoking method \"{del.Method.Name}\" " +
							$"from event {ev.GetType()}", e, LogCategories.Events);
					}
				}
			}

			return allOk;
		}

		[Obsolete("Too slow.")]
		private static bool TryTriggerEventsDelegate(Delegate ev, params object[] args) {
			bool allOk = true;
			if (ev != null) {
				foreach (Delegate del in ev.GetInvocationList()) {
					try {
						del?.Method.Invoke(del.Target, args);
					} catch (Exception e) {
						allOk = false;
						TimeLogger.Logger.LogTimeExceptionWithMessage($"Exception while invoking method \"{del.Method.Name}\" " +
							$"from event {ev.GetType()}", e, LogCategories.Events);
					}
				}
			}

			return allOk;
		}

	}
}
