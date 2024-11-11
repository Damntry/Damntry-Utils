using System;
using System.Xml.Linq;

namespace Damntry.Utils.Logging {

	public sealed class DefaultTimeLogger : TimeLoggerBase {


		public static DefaultTimeLogger Logger {
			get {
				if (instance == null) {
					InitializeTimeLogger("", false);	//In normal circumstances this would be called from the entry point of the application.
				}
				return (DefaultTimeLogger)GetLogInstance(nameof(DefaultTimeLogger));
			}
		}

		public static void InitializeTimeLogger(string sourceNamePrefix, bool debugEnabled = false) {
			Lazy<TimeLoggerBase> instance = new Lazy<TimeLoggerBase>(() => new DefaultTimeLogger());

			TimeLoggerBase.InitializeTimeLogger(instance, debugEnabled);
		}

		protected override void Log(string message, LogTier logLevel) {
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
