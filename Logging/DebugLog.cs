using System;

namespace Damntry.Utils.Logging {

	public class LOG {

		public static void DEBUG(string message, bool onlyIfTrue = true) {
			Log(LogTier.Debug, message, onlyIfTrue);
		}

		public static void DEBUG_FUNC(Func<string> textLambda, bool onlyIfTrue = true) {
			LogFunc(LogTier.Debug, textLambda, onlyIfTrue);
		}

		public static void DEBUGWARNING(string message, bool onlyIfTrue = true) {
			Log(LogTier.Warning, message, onlyIfTrue);
		}

		public static void DEBUGWARNING_FUNC(Func<string> textLambda, bool onlyIfTrue = true) {
			LogFunc(LogTier.Warning, textLambda, onlyIfTrue);
		}

		private static void Log(LogTier logLevel, string message, bool onlyIfTrue = true) {
			if (onlyIfTrue && TimeLogger.DebugEnabled) {
				TimeLogger.Logger.LogTime(logLevel, message, LogCategories.TempTest);
			}
		}

		private static void LogFunc(LogTier logLevel, Func<string> textLambda, bool onlyIfTrue = true) {
			if (onlyIfTrue && TimeLogger.DebugEnabled) {
				TimeLogger.Logger.LogTimeFunc(logLevel, textLambda, LogCategories.TempTest, false, null);
			}
		}

	}
}
