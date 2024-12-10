using System.Diagnostics;

namespace Damntry.Utils.Timers.StopwatchImpl {

	/// <summary>
	/// Stopwatch that implements the IStopwatch interface.
	/// To be used in any of the classes that accept this interface
	///	in case the default Stopwatch functionality is needed.
	/// </summary>
	public class StopwatchDiag : Stopwatch, IStopwatch {

		/// <summary>
		/// Gets the total elapsed time measured by the current instance, in milliseconds.
		/// </summary>
		public long ElapsedSeconds {
			get {
				return base.ElapsedMilliseconds * 1000;
			}
		}

		/// <summary>
		/// Gets the value of the current System.TimeSpan structure expressed in whole and fractional milliseconds.
		/// </summary>
		public double ElapsedMillisecondsPrecise {
			get {
				return base.Elapsed.TotalMilliseconds;
			}
		}

		/// <summary>
		/// Gets the value of the current System.TimeSpan structure expressed in whole and fractional seconds.
		/// </summary>
		public double ElapsedSecondsPrecise {
			get {
				return base.Elapsed.TotalSeconds;
			}
		}

	}

}