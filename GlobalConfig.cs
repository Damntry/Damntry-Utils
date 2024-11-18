using System;
using Damntry.Utils.Logging;

namespace Damntry.Utils {

	public static class GlobalConfig {

		//TODO Global 4 - Create an Interface and inherit it both here and in TimeLoggerBase so any classes deriving
		//	TimeLoggerBase must override this GlobalConfig.Logger value.

		public static TimeLoggerBase Logger = DefaultTimeLogger.Logger;


		public static TimeLoggerBase TimeLoggerLog {
			get {
				if (Logger == null) {
					throw new NullReferenceException("The Logger for globals cant be null.");
				}

				return Logger;
			}
		}

	}
}
