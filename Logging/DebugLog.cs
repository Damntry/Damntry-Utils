using System;

namespace Damntry.Utils.Logging {

	public class LOG {

		public static void TEMPDEBUG(string message, bool onlyIfTrue = true) {
#if DEBUG
			Log(LogTier.Debug, message, onlyIfTrue);
#endif
		}

		public static void TEMPDEBUG_FUNC(Func<string> textLambda, bool onlyIfTrue = true) {
#if DEBUG
			LogFunc(LogTier.Debug, textLambda, onlyIfTrue);
#endif
		}

		
		public static void Debug(string message, LogCategories logCategory, bool onlyIfTrue = true) {
#if DEBUG
			Log(LogTier.Debug, message, onlyIfTrue, logCategory);
#endif
		}

		public static void Debug_func(Func<string> textLambda, LogCategories logCategory, bool onlyIfTrue = true) {
#if DEBUG
			LogFunc(LogTier.Debug, textLambda, onlyIfTrue, logCategory);
#endif
		}
		

		public static void TEMPWARNING(string message, bool onlyIfTrue = true) {
#if DEBUG
			Log(LogTier.Warning, message, onlyIfTrue);
#endif
		}

		public static void TEMPWARNING_FUNC(Func<string> textLambda, bool onlyIfTrue = true) {
#if DEBUG
			LogFunc(LogTier.Warning, textLambda, onlyIfTrue);
#endif
		}

		public static void TEMPFATAL(string message, bool onlyIfTrue = true) {
#if DEBUG
			Log(LogTier.Fatal, message, onlyIfTrue);
#endif
		}

		public static void TEMP(LogTier logLevel, string message, bool onlyIfTrue = true) {
#if DEBUG
			Log(logLevel, message, onlyIfTrue);
#endif
		}

		private static void Log(LogTier logLevel, string message, bool onlyIfTrue = true, LogCategories logCategory = LogCategories.TempTest) {
#if DEBUG
			if (onlyIfTrue && TimeLogger.DebugEnabled) {
				TimeLogger.Logger.LogTime(logLevel, message, logCategory);
			}
#endif
		}

		private static void LogFunc(LogTier logLevel, Func<string> textLambda, bool onlyIfTrue = true, LogCategories logCategory = LogCategories.TempTest) {
#if DEBUG
			if (onlyIfTrue && TimeLogger.DebugEnabled) {
				TimeLogger.Logger.LogTimeFunc(logLevel, textLambda, logCategory, false, null);
			}
#endif
		}

	}
}
