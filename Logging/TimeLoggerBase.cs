using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using static System.Net.Mime.MediaTypeNames;


namespace Damntry.Utils.Logging {


	/// <summary>
	/// Provides logging functionality with features to add log times, categories per log, and automatically call custom functions when logging.
	/// This class cant be used directly as is, and instead must be inherited.
	/// See <see cref="DefaultTimeLogger"/> for a basic example of use.
	/// </summary>
	public abstract class TimeLoggerBase {

		protected abstract void Log(string logMessage, LogTier logLevel);

		protected static Lazy<TimeLoggerBase> instance;

		private Dictionary<LogCategories, string> logCategoryStringCache;

		private static string notificationMsgPrefix;

		private static Action<string, LogTier> notificationAction;



		protected static TimeLoggerBase GetLogInstance(string derivedClassName) {
			if (instance == null) {
				throw new InvalidOperationException($"{derivedClassName} must be initialized first by calling an InitializeTimeLogger...() method.");
			}
			return instance.Value;
		}


		public static bool DebugEnabled { get; set; }


		protected static void InitializeTimeLogger(Lazy<TimeLoggerBase> instance, bool debugEnabled = false) {
			if (instance == null) {
				throw new ArgumentNullException(nameof(instance));
			}

			TimeLoggerBase.instance = instance;
			DebugEnabled = debugEnabled;
		}

		protected static void InitializeTimeLoggerWithGameNotifications(Lazy<TimeLoggerBase> instance, Action<string, LogTier> notificationAction, 
				string notificationMsgPrefix, bool debugEnabled = false) {

			if (notificationAction == null) {
				throw new ArgumentNullException(nameof(notificationAction), "The argument notificationAction cannot be null. Call InitializeTimeLogger(...) instead.");
			}

			InitializeTimeLogger(instance, debugEnabled);

			AddGameNotificationSupport(notificationAction, notificationMsgPrefix);
		}

		//TODO Global 6 - Both AddGameNotificationSupport and RemoveGameNotificationSupport need
		//	to be locked while logging is happening, in case there is multithreading.

		/// <summary>Useful for when you want to delay adding the game notifications until some time after the logger itself was initialized.</summary>
		public static void AddGameNotificationSupport(Action<string, LogTier> notificationAction, string notificationMsgPrefix) {
			if (notificationAction == null) {
				throw new ArgumentNullException(nameof(notificationAction));
			}

			TimeLoggerBase.notificationAction = notificationAction;
			TimeLoggerBase.notificationMsgPrefix = notificationMsgPrefix != null ? notificationMsgPrefix : "";
		}

		public static void RemoveGameNotificationSupport() {
			TimeLoggerBase.notificationAction = null;
			TimeLoggerBase.notificationMsgPrefix = null;
		}


		protected TimeLoggerBase() {
			logCategoryStringCache = new Dictionary<LogCategories, string>();
		}



		[Flags]
		public enum LogTier {
			None = 0,
			Fatal = 1,
			Error = 2,
			Warning = 4,
			Message = 8,
			Info = 0x10,
			Debug = 0x20,
			All = ~None
		}

		//TODO Global 2 - This needs to be expandable by the external project. I ll have to convert this into a class and make it behave enum-esque.
		[Flags]
		public enum LogCategories {
			Null = 0,  //Not intended for logging. Only used internally for defaults.
			TempTest = 1,
			Vanilla = 2,
			PerfTest = 4,
			Loading = 8,
			Task = 0x10,
			Reflect = 0x20,
			Config = 0x30,
			//↓ New ones below as needed ↓
			Highlight = 0x40,
			Notifs = 0x50,
			AutoPatch = 0x60,

			All = ~Null
		}

		//Stores the length of the longest name in the enum of categories, for later padding.
		private int maxCategoryLength = Enum.GetNames(typeof(LogCategories)).Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur).Length;

		//Mostly for testing. Set to LogCategories.Null to disable. Assign multiple with the bitwise OR operator '|'.
		private static LogCategories AllowedCategories = LogCategories.Null;


		public void LogTimeInfo(string text, LogCategories category) {
			LogTime(LogTier.Info, text, category, false, null);
		}

		public void LogTimeInfoShowInGame(string text, LogCategories category) {
			LogTime(LogTier.Info, text, category, true, null);
		}

		public void LogTimeDebug(string text, LogCategories category) {
			LogTime(LogTier.Debug, text, category, false, null);
		}

		public void LogTimeDebugShowInGame(string text, LogCategories category) {
			LogTime(LogTier.Debug, text, category, true, null);
		}

		public void LogTimeDebug(string text, LogCategories category, bool showInGameNotification) {
			LogTime(LogTier.Debug, text, category, showInGameNotification, null);
		}

		/// <summary>Check LogTimeDebugFunc((Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs)</summary>
		public void LogTimeDebugFunc(Func<string> textLambda, LogCategories category) {
			LogTimeFunc(LogTier.Debug, textLambda, category, false);
		}

		/// <summary>Check LogTimeDebugFunc((Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs)</summary>
		public void LogTimeDebugFunc(Func<string> textLambda, LogCategories category, params (Action<string> action, bool useFullLog)[] actionsArgs) {
			LogTimeFunc(LogTier.Debug, textLambda, category, false, actionsArgs);
		}

		/// <summary>Check LogTimeDebugFunc((Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs)</summary>
		public void LogTimeDebugFuncShowInGame(Func<string> textLambda, LogCategories category) {
			LogTimeFunc(LogTier.Debug, textLambda, category, true);
		}

		/// <summary>Check LogTimeDebugFunc((Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs)</summary>
		public void LogTimeDebugFunc(Func<string> textLambda, LogCategories category, bool showInGameNotification) {
			LogTimeFunc(LogTier.Debug, textLambda, category, showInGameNotification);
		}

		/// <summary>
		///	Logs a debug errorMessage, passing the text string as a lambda. <para />
		///	This is done for performance reasons for when EnableDebug is disabled, since creating a string that includes something 
		///	other than string literals or constants, forces it to be composed at runtime, which is always more expensive than the 
		///	tiny performance hit of creating a lambda.<para />
		///	So instead, we pass a lambda that contains the string. This allows us to defer the string composition until it is
		///	used to log, and can potentially save a considerable amount of processing time since when debug logging is disabled
		///	the string wont get used.<para />
		///	Still, though a lambda has an almost neligible performance hit, its not free an slightly harder to read, so if we are only 
		///	passing a literal/constant, no matter if it is using concatenation or string interpolation, it is recommended to use 
		///	LogTimeDebug instead for simplicity.
		///	</summary>
		/// <param name="textLambda">A string lambda. Examples:  <para /> 
		///		() => var1 + "-" + var2  <para />
		///		() => $"Count is: {list.Count}" <para />
		///	You can save the string lambda as a local function, for when you want to reuse it or create before the Logger call: <para />
		///		string methodNameExample() => var1 + "-" + var2; <para />
		///		LogTimeDebugFunc(methodNameExample, ...)
		/// </param>
		/// <param name="category">The category to show at the beginning of the log.</param>
		/// <param name="showInGameNotification">True if the text will be sent to the preconfigured nofitication Action.</param>
		/// <param name="actionsArgs">The methods we want to execute after logging is finished. This is intended to be used for logging too.</param>
		public void LogTimeDebugFunc(Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs) {
			LogTimeFunc(LogTier.Debug, textLambda, category, showInGameNotification, actionsArgs);
		}

		public void LogTimeWarning(string text, LogCategories category) {
			LogTime(LogTier.Warning, text, category, false, null);
		}

		public void LogTimeWarningShowInGame(string text, LogCategories category) {
			LogTime(LogTier.Warning, text, category, true, null);
		}

		public void LogTimeException(Exception e, LogCategories category) {
			LogTimeExceptionShowInGameWithMessage(null, e, category, false);
		}

		public void LogTimeExceptionWithMessage(string text, Exception e, LogCategories category) {
			LogTimeExceptionShowInGameWithMessage(text, e, category, false);
		}

		public void LogTimeExceptionShowInGame(Exception e, LogCategories category) {
			LogTimeExceptionShowInGameWithMessage(null, e, category, true);
		}

		public void LogTimeExceptionShowInGameWithMessage(string text, Exception e, LogCategories category) {
			LogTimeExceptionShowInGameWithMessage(text, e, category, true);
		}
		private void LogTimeExceptionShowInGameWithMessage(string text, Exception e, LogCategories category, bool showInGame) {
			string exceptionMessage = FormatException(e, text);
			string notifText = text != null ? text : e.Message;

			LogTime(LogTier.Fatal, exceptionMessage, category, false, null);
			if (showInGame) {
				SendMessageNotificationError(notifText);
			}
		}

		public void LogTimeFatal(string text, LogCategories category) {
			LogTime(LogTier.Fatal, text, category, false, null);
		}

		public void LogTimeFatalShowInGame(string text, LogCategories category) {
			LogTime(LogTier.Fatal, text, category, true, null);
		}

		public void LogTimeError(string text, LogCategories category) {
			LogTime(LogTier.Error, text, category, false, null);
		}

		public void LogTimeErrorShowInGame(string text, LogCategories category) {
			LogTime(LogTier.Error, text, category, true, null);
		}

		private void LogTimeFunc(LogTier logLevel, Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs) {
			if (logLevel == LogTier.Debug && !DebugEnabled) {
				return;
			}

			LogTime(logLevel, textLambda(), category, showInGameNotification, actionsArgs);
		}

		public void LogTime(LogTier logLevel, string text, LogCategories category) {
			LogTime(logLevel, text, category, false, null);
		}

		public void LogTime(LogTier logLevel, string text, LogCategories category, bool showInGameNotification) {
			LogTime(logLevel, text, category, showInGameNotification, null);
		}

		private void LogTime(LogTier logLevel, string text, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs) {
			if (logLevel == LogTier.Debug && !DebugEnabled || AllowedCategories != LogCategories.Null && AllowedCategories.HasFlag(category)) {
				return;
			}

			string logMessage = FormatLogMessageWithTime(text, category);

			//TODO Global 6 - I need to lock all of this below (logging, notification, and action) so we are sure its executed all together, since the action
			//	is meant to log too and do it right after the normal logging, and not let any other logging threads put text in between.
			//	But this means the notification or the Action might also log themselves using this method, so I cant use a lock.
			//  I need some sort of semaphore system where I assign an ID to this call, and any consequent calls with the same ID are allowed
			//	to pass, while blocking any others.
			//	I cant create a custom context because I might break the notification or Action caller logic, so honestly I dont even know 
			//	if what I want is possible, but do some research.

			Log(logMessage, logLevel);

			if (showInGameNotification) {
				SendMessageNotification(logLevel, text);
			}
			
			//Extra functionality that the caller wants executed with our processed text errorMessage
			if (actionsArgs != null) {
				foreach (var arg in actionsArgs) {
					arg.action(arg.useFullLog ? logMessage : text);
				}
			}
		}

		public string GetCategoryStringFromCache(LogCategories category) {
			if (!logCategoryStringCache.TryGetValue(category, out string categoryStr)) {
				//Doesnt exist in cache yet, add.
				categoryStr = category.ToString();
				logCategoryStringCache.Add(category, categoryStr);
			}

			return categoryStr;
		}

		private string FormatLogMessageWithTime(string text, LogCategories category) {
			//Pad the category string to keep the log format nice.
			StringBuilder sbLog = new StringBuilder(100);

			string categoryStr = category == default ? "" : GetCategoryStringFromCache(category);

			sbLog.Append("[");
			sbLog.Append(categoryStr.PadRight(maxCategoryLength, ' '));
			sbLog.Append("] ");
			//TODO Global 6 - Make this format configurable
			sbLog.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
			sbLog.Append(" - ");
			sbLog.Append(text);

			return sbLog.ToString();
		}

		public static string FormatException(Exception e, string text = null) {
			string errorMessage = text != null ? $"{text}\n{e.Message}" : e.Message;

			return $"{errorMessage} (In {e.TargetSite})\n{e.StackTrace}";
		}

		public void SendMessageNotificationError(string message) {
			SendMessageNotification(LogTier.Error, message);
		}

		public void SendMessageNotification(LogTier logLevel, string message) {
			if (logLevel == LogTier.Debug && !DebugEnabled) {
				return;
			}

			if (notificationAction != null) {
				notificationAction(message, logLevel);
			}
		}


		/// <summary>Gets all property values (ToString) of an object through reflection.</summary>
		/// <param name="target">The object from which we ll get all properties</param>
		/// <returns>A string where each line is a property.</returns>
		public static string GetPropertiesLog(object target) {
			var properties =
				from property in target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
				select new {
					property.Name,
					Value = property.GetValue(target, null)
				};

			var builder = new StringBuilder(12 * properties.Count());

			foreach (var property in properties) {
				builder
					.Append(property.Name)
					.Append(" = ")
					.Append(property.Value)
					.AppendLine();
			}

			return builder.ToString();
		}

		//This is cool and all but it doesnt work without pdb symbols (from the exe? from this dll only?). Anyway, not usable in another environment.
		/*
		private static ILogger CreateLoggingException<ILogger>(string errorMessage) where ILogger : Exception {
			System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace(10, true);
			string stacktraceString = string.Join("", stackTrace.GetFrames().Skip(2));
			//string stackTraceString = Environment.StackTrace;

			Logger.LogDebugTime($"Exception: {errorMessage}\n{stacktraceString}", true);
			return (ILogger)Activator.CreateInstance(typeof(ILogger), new string[] { errorMessage });
		}
		*/

	}
}
