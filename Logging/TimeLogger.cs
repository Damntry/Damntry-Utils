using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Damntry.Utils.Logging {

	public enum PreprocessType {
		/// <summary>Preprocessing the text to log to disk.</summary>
		FileLogging,
		/// <summary>Preprocessing the text to send an in-game notification.</summary>
		GameNotification,
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


	//TODO Global 2 - This needs to be expandable by the external project.
	//	I ll have to convert this into a class and make it behave enum-esque.
	[Flags]
	public enum LogCategories {
		Null = 0,			//Not intended for logging. Only used internally for defaults.
		TempTest = 1,		//Temporary tests not meant for release.
		Vanilla = 1 << 1,
		PerfTest = 1 << 2,	//Temporary performance tests not meant for release.
		Loading = 1 << 3,
		Task = 1 << 4,      //Task, threaded or not, related logic.
		Reflect = 1 << 5,	//Reflection
		Config = 1 << 6,
		PerfCheck = 1 << 7,	//Performance checks that calculate and/or does dynamic throttling.
		Events = 1 << 8,
		Network = 1 << 9,
		Cache = 1 << 10,

		//↓ Specific to a project below ↓
		Notifs = 1 << 20,
		AutoPatch = 1 << 21,
		MethodChk = 1 << 22,
		OtherMod = 1 << 23,
		JobSched = 1 << 24,
		KeyMouse = 1 << 25,
		Highlight = 1 << 26,
		AI = 1 << 27,



		Other = 1 << 63,	//Too specific to make a category for it.
		All = ~Null
	}


	/// <summary>
	/// Provides logging functionality with features to add log times, categories
	/// per log, and automatically call custom functions when logging.
	/// This class cant be used directly as is, and instead must have a derived
	/// class that provides the logging implementation.
	/// See <see cref="DefaultTimeLogger"/> for a basic example of use.
	/// </summary>
	public abstract class TimeLogger {

		private static TimeLogger instance;

		public static TimeLogger Logger { 
			get {
				if (instance == null) {
					TimeLogger.InitializeTimeLogger<DefaultTimeLogger>(false);
					instance.LogTimeWarning($"TimeLogger has been automatically initialized with a DefaultTimeLogger. " +
						$"If you want to use a different custom logger, call a InitializeTimeLogger...() method earlier.", 
						LogCategories.Loading);
				}
				return instance;
			}
		}
		

		private Dictionary<LogCategories, string> logCategoryStringCache;

		//TODO 4 - This ended up forgotten. I should probably change it into an error prefix of sorts, so
		//		it shows when Loglevel error/fatal. This would make it so in ErrorMessageOnAutoPatchFail
		//		I dont need to pass the mod name manually on every single one.
		//		Actually it would be better to have a new parameter in the LogTime... methods with notification
		//		support where you specify if the prefix is added or not.
		private static string notificationMsgPrefix;

		/// <summary>
		/// Action method to send a notification.
		/// </summary>
		private static Action<string, LogTier> notificationAction;

		/// <summary>
		/// Function called before both logging and showing the in game notification,
		/// to do specific modifications to the message in each case.
		/// </summary>
		/// <param name="message">The original unformatted message passed to the logging method.</param>
		/// <param name="logLevel">The level of logging.</param>
		/// <param name="category">The category of the logging.</param>
		/// <param name="showInGameNotification">If this message is being sent as an ingame notification.</param>
		/// <param name="preprocType">The type of action that the message will be modified for.</param>
		/// <returns>The text message that will be used for the specified action in <paramref name="preprocType"/></returns>
		/// <remarks>
		/// If the method returns null, the original message (<paramref name="message"/>) will be used instead.
		/// </remarks>
		public delegate string PreprocessMessageFunc(string message, LogTier logLevel, 
			LogCategories category, bool showInGameNotification, PreprocessType preprocType);

		/// <summary>
		/// Custom method to modify the message before logging and notifying. 
		/// Receives the original, unformatted string message as parameter.
		/// </summary>
		private static PreprocessMessageFunc globalPreprocessMessageFunc;


		public static bool DebugEnabled { get; set; }


		protected TimeLogger() {
			logCategoryStringCache = new Dictionary<LogCategories, string>();
		}


		/// <summary>Custom log implementation called when logging is invoked.</summary>
		protected abstract void LogMessage(string logMessage, LogTier logLevel);

		/// <summary>
		/// Initialize any specific functionality of the log system to use.
		/// </summary>
		/// <param name="argsT">Custom arguments that the method may receive.</param>
		protected abstract void InitializeLogger(params object[] argsT);

		public static void InitializeTimeLogger<T>(bool debugEnabled = false, params object[] argsT) 
				where T : TimeLogger {
			InitializeTimeLogger<T>(null, debugEnabled, argsT);
		}

		public static void InitializeTimeLogger<T>(PreprocessMessageFunc preprocessMessageFunc, 
				bool debugEnabled = false, params object[] argsT) 
					where T : TimeLogger {
			TimeLogger.DebugEnabled = debugEnabled;

			TimeLogger.globalPreprocessMessageFunc = preprocessMessageFunc;

			instance = Activator.CreateInstance<T>();
			instance.InitializeLogger(argsT);
		}

		public static void InitializeTimeLoggerWithGameNotifications<T>(Action<string, LogTier> notificationAction, 
				string notificationMsgPrefix, bool debugEnabled = false, params object[] argsT) 
					where T : TimeLogger {
			InitializeTimeLoggerWithGameNotifications<T>(null, notificationAction, 
				notificationMsgPrefix, debugEnabled, argsT);
		}

		public static void InitializeTimeLoggerWithGameNotifications<T>(PreprocessMessageFunc preprocessMessageFunc, 
				Action<string, LogTier> notificationAction, 
				string notificationMsgPrefix, bool debugEnabled = false, params object[] argsT) 
					where T : TimeLogger {

			if (notificationAction == null) {
				throw new ArgumentNullException(nameof(notificationAction), "The argument notificationAction cannot be null. Call InitializeTimeLogger(...) instead.");
			}
			
			InitializeTimeLogger<T>(preprocessMessageFunc, debugEnabled, argsT);

			AddGameNotificationSupport(notificationAction, notificationMsgPrefix);
		}

		//TODO Global 6 - Both AddGameNotificationSupport and RemoveGameNotificationSupport need
		//	to be locked while logging is happening, in case there is multithreading.
		//	And most probably some others. Revise.

		/// <summary>Useful for when you want to delay adding the game notifications until some time after the logger itself was initialized.</summary>
		public static void AddGameNotificationSupport(Action<string, LogTier> notificationAction, string notificationMsgPrefix) {
			if (notificationAction == null) {
				throw new ArgumentNullException(nameof(notificationAction));
			}

			TimeLogger.notificationAction = notificationAction;
			TimeLogger.notificationMsgPrefix = notificationMsgPrefix != null ? notificationMsgPrefix : "";
		}

		public static void RemoveGameNotificationSupport() {
			TimeLogger.notificationAction = null;
			TimeLogger.notificationMsgPrefix = null;
		}
				

		/// <summary>
		/// Stores the length of the longest name in the enum of categories, for later padding.
		/// </summary>
		private int maxCategoryLength = Enum.GetNames(typeof(LogCategories)).Aggregate("", (max, cur) => max.Length > cur.Length ? max : cur).Length;

		/// <summary>
		/// Mostly for testing. Set to LogCategories.None to disable. Assign multiple with the bitwise OR operator '|'.
		/// </summary>
		private LogCategories AllowedCategories = LogCategories.Null;



		public void LogTimeInfo(string text, LogCategories category) {
			LogTimeInternal(LogTier.Info, text, category, false, null);
		}

		public void LogTimeInfoShowInGame(string text, LogCategories category) {
			LogTimeInternal(LogTier.Info, text, category, true, null);
		}

		public void LogTimeMessage(string text, LogCategories category) {
			LogTimeInternal(LogTier.Message, text, category, false, null);
		}

		public void LogTimeMessageShowInGame(string text, LogCategories category) {
			LogTimeInternal(LogTier.Message, text, category, true, null);
		}

		public void LogTimeDebug(string text, LogCategories category) {
			LogTimeInternal(LogTier.Debug, text, category, false, null);
		}

		public void LogTimeDebugShowInGame(string text, LogCategories category) {
			LogTimeInternal(LogTier.Debug, text, category, true, null);
		}

		public void LogTimeDebug(string text, LogCategories category, bool showInGameNotification) {
			LogTimeInternal(LogTier.Debug, text, category, showInGameNotification, null);
		}

		/// <summary>Check LogTimeDebugFunc((Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs)</summary>
		public void LogTimeDebugFunc(Func<string> textLambda, LogCategories category) {
			LogTimeInternalFunc(LogTier.Debug, textLambda, category, false);
		}

		/// <summary>Check LogTimeDebugFunc((Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs)</summary>
		public void LogTimeDebugFunc(Func<string> textLambda, LogCategories category, params (Action<string> action, bool useFullLog)[] actionsArgs) {
			LogTimeInternalFunc(LogTier.Debug, textLambda, category, false, actionsArgs);
		}

		/// <summary>Check LogTimeDebugFunc((Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs)</summary>
		public void LogTimeDebugFuncShowInGame(Func<string> textLambda, LogCategories category) {
			LogTimeInternalFunc(LogTier.Debug, textLambda, category, true);
		}

		/// <summary>Check LogTimeDebugFunc((Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs)</summary>
		public void LogTimeDebugFunc(Func<string> textLambda, LogCategories category, bool showInGameNotification) {
			LogTimeInternalFunc(LogTier.Debug, textLambda, category, showInGameNotification);
		}

		/// <summary>
		///	Logs a debug errorMessage, passing the originalText string as a lambda. <para />
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
		/// <param name="showInGameNotification">True if the originalText will be sent to the preconfigured nofitication Action.</param>
		/// <param name="actionsArgs">The methods we want to execute after logging is finished. This is intended to be used for logging too.</param>
		public void LogTimeDebugFunc(Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs) {
			LogTimeInternalFunc(LogTier.Debug, textLambda, category, showInGameNotification, actionsArgs);
		}

		public void LogTimeWarning(string text, LogCategories category) {
			LogTimeInternal(LogTier.Warning, text, category, false, null);
		}

		public void LogTimeWarningShowInGame(string text, LogCategories category) {
			LogTimeInternal(LogTier.Warning, text, category, true, null);
		}

		public void LogTimeException(Exception e, LogCategories category) {
			LogTimeExceptionInternal(null, e, category, false);
		}

		public void LogTimeExceptionWithMessage(string text, Exception e, LogCategories category) {
			LogTimeExceptionInternal(text, e, category, false);
		}

		public void LogTimeExceptionShowInGame(Exception e, LogCategories category) {
			LogTimeExceptionInternal(null, e, category, true);
		}

		public void LogTimeExceptionShowInGameWithMessage(string text, Exception e, LogCategories category) {
			LogTimeExceptionInternal(text, e, category, true);
		}

		private void LogTimeExceptionInternal(string text, Exception e, LogCategories category, bool showInGame) {

			PreprocessMessageFunc prepMsgFunc = (string msg, LogTier _, LogCategories _, bool _, PreprocessType prepType) => {
				if (prepType == PreprocessType.FileLogging) {
					return FormatException(e, msg);
				} else if (prepType == PreprocessType.GameNotification) {
					return msg ?? e.Message;
				}
				return null;
			};

			LogTimeInternal(LogTier.Fatal, text, category, prepMsgFunc, showInGame);
		}

		public void LogTimeFatal(string text, LogCategories category) {
			LogTimeInternal(LogTier.Fatal, text, category, false, null);
		}

		public void LogTimeFatalShowInGame(string text, LogCategories category) {
			LogTimeInternal(LogTier.Fatal, text, category, true, null);
		}

		public void LogTimeError(string text, LogCategories category) {
			LogTimeInternal(LogTier.Error, text, category, false, null);
		}

		public void LogTimeErrorShowInGame(string text, LogCategories category) {
			LogTimeInternal(LogTier.Error, text, category, true, null);
		}

		public void LogTimeFunc(LogTier logLevel, Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs) {
			LogTimeInternalFunc(logLevel, textLambda, category, showInGameNotification, actionsArgs);
		}


		private void LogTimeInternalFunc(LogTier logLevel, Func<string> textLambda, LogCategories category, bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs) {
			//Return so we skip executing the text lambda and save processing time.
			if (logLevel == LogTier.Debug && !DebugEnabled) {
				return;
			}

			LogTimeInternal(logLevel, textLambda(), category, showInGameNotification, actionsArgs);
		}

		public void LogTime(LogTier logLevel, string text, LogCategories category) {
			LogTimeInternal(logLevel, text, category, false, null);
		}

		public void LogTime(LogTier logLevel, string text, LogCategories category, bool showInGameNotification) {
			LogTimeInternal(logLevel, text, category, showInGameNotification, null);
		}

		/// <remarks>
		/// Be aware that by using this method, any global preprocessor set
		/// during the initialization of this time logger, wont be used.
		/// </remarks>
		public void LogTime(LogTier logLevel, string text, LogCategories category, PreprocessMessageFunc preprocessMsgFunc, 
				bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs) {
			LogTimeInternal(logLevel, text, category, preprocessMsgFunc, showInGameNotification, null);
		}

		private void LogTimeInternal(LogTier logLevel, string originalText, LogCategories category, 
				bool showInGameNotification, params (Action<string> action, bool useFullLog)[] actionsArgs) {

			LogTimeInternal(logLevel, originalText, category, globalPreprocessMessageFunc, 
				showInGameNotification, actionsArgs);
		}

		private void LogTimeInternal(LogTier logLevel, string originalText, LogCategories category,
				PreprocessMessageFunc preprocessMsgFunc, bool showInGameNotification,  
				params (Action<string> action, bool useFullLog)[] actionsArgs) {
			
			if (logLevel == LogTier.Debug && !DebugEnabled || AllowedCategories != LogCategories.Null && !AllowedCategories.HasFlag(category)) {
				return;
			}
			
			string preprocessedLog = PreprocessMessage(originalText, logLevel, category, 
				showInGameNotification, preprocessMsgFunc, PreprocessType.FileLogging);

			string fullFormattedLog = FormatLogMessageWithTime(preprocessedLog, category);

			//TODO Global 6 - I need to lock all of this below (logging, notification, and action) so we are sure its executed all together, since the action
			//	is meant to log too and do it right after the normal logging, and not let any other logging threads put originalText in between.
			//	But this means the notification or the Action might also log themselves using this method, so I cant use a lock.
			//  I need some sort of semaphore system where I assign an ID to this call, and any consequent calls with the same ID are allowed
			//	to pass, while blocking any others.
			//	I cant create a custom context because I might break the notification or Action caller logic, so honestly I dont even know 
			//	if what I want is possible, but do some research.

			LogMessage(fullFormattedLog, logLevel);

			if (showInGameNotification) {
				string notifMessage = PreprocessMessage(originalText, logLevel, category, 
					true, preprocessMsgFunc, PreprocessType.GameNotification);

				SendMessageNotification(logLevel, notifMessage);
			}
			
			//Extra methods that the caller wants executed with our processed originalText errorMessage
			if (actionsArgs != null) {
				foreach (var arg in actionsArgs) {
					arg.action(arg.useFullLog ? fullFormattedLog : preprocessedLog);
				}
			}
		}

		private string PreprocessMessage(string text, LogTier logLevel, LogCategories category, 
			bool showInGameNotification, PreprocessMessageFunc preprocessMsgFunc, PreprocessType prepType) {

			if (preprocessMsgFunc != null) {
				return preprocessMsgFunc(text, logLevel, category, showInGameNotification, prepType) 
					?? text;
			}

			return text;    //Return unmodified
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

			errorMessage += $"(In {e.TargetSite})\n{e.StackTrace}";
			if (e.InnerException != null) {
				errorMessage += $"\n * InnerException: {e.InnerException.Message} " +
					$"(In {e.InnerException.TargetSite})\n{e.InnerException.StackTrace}";
			}
			return errorMessage;
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

	}
}
