using System;

namespace Damntry.Utils.Logging {

	public sealed class DefaultTimeLogger : TimeLogger {


		protected override void InitializeLogger(params object[] args) { }


		protected override void LogMessage(string message, LogTier logLevel) {
			//Since the console doesnt have a use for log levels like log libraries, we simply convert each to a color.
			ConsoleColor consoleColor = ConsoleColor.Gray;

			switch (logLevel) {
				case LogTier.All:
					consoleColor = ConsoleColor.White;
					break;
				case LogTier.Debug:
					consoleColor = ConsoleColor.DarkGray;
					break;
				case LogTier.Info:
					consoleColor = ConsoleColor.Gray;
					break;
				case LogTier.Warning:
					consoleColor = ConsoleColor.Yellow;
					break;
				case LogTier.Error:
					consoleColor = ConsoleColor.DarkRed;
					break;
				case LogTier.Fatal:
					consoleColor = ConsoleColor.Red;
					break;
				case LogTier.Message:
					consoleColor = ConsoleColor.Gray;
					break;
				case LogTier.None:
					return;
				default:
					throw new InvalidOperationException("Invalid log level.");
			}

			Console.ForegroundColor = consoleColor;

			Console.WriteLine(message);

			Console.ResetColor();
		}

	}

}
