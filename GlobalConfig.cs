using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Damntry.Utils.Logging;

namespace Damntry.Utils {

	public static class GlobalConfig {


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
