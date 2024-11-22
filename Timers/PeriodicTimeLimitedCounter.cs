using System.Diagnostics;

namespace Damntry.Utils.Timers {

	/// <summary>
	/// Functionality to check if an external event has happened more than a defined number of times within a time period.
	/// </summary>
	public class PeriodicTimeLimitedCounter {

		private Stopwatch swLimitHit;

		private bool constantPeriodTimer;

		private int hitCounter;

		private int hitCounterMax;

		private double maxPeriodTimeMillis;

		private bool triggerOncePerPeriod;


		/// <summary>
		/// Initializes the time limited counter.
		/// </summary>
		/// <param name="constantPeriodTimer">
		/// True so the internal timer only resets itself when it reaches <paramref name="maxPeriodTimeMillis"/>. 
		/// Otherwise it also resets when the counter hits <paramref name="hitCounterMax"/>.
		/// </param>
		/// <param name="hitCounterMax">The max amount of calls to <see cref="TryIncreaseCounter"/> before we
		/// trigger the limit warning if less than <paramref name="maxPeriodTimeMillis"/> has elapsed in the internal timer.</param>
		/// <param name="maxPeriodTimeMillis">If the hit counter reaches <paramref name="hitCounterMax"/> before this amount of milliseconds, it will trigger a warning.</param>
		/// <param name="ignoreFirstHit">Since the first run is usually slower, we can ignore it for the counter.</param>
		/// <param name="triggerOncePerPeriod">
		/// True if the warning will be triggered only once per time period. 
		/// Otherwise it will keep triggering on each hit until the time period ends and the process starts over.
		/// </param>
		public PeriodicTimeLimitedCounter(bool constantPeriodTimer, int hitCounterMax, double maxPeriodTimeMillis, bool ignoreFirstHit, bool triggerOncePerPeriod = true) {
			this.constantPeriodTimer = constantPeriodTimer;
			this.hitCounterMax = hitCounterMax;
			this.maxPeriodTimeMillis = maxPeriodTimeMillis;
			this.hitCounter = ignoreFirstHit ? -1 : 0;
			this.triggerOncePerPeriod = triggerOncePerPeriod;

			swLimitHit = new Stopwatch();
		}

		/// <summary>
		/// Increases the internal counter and checks if it triggers the warning.
		/// The warning can only be triggered once per period.
		/// </summary>
		/// <returns>True if the counter increases normally. False if counter hits its max value within the time limit.</returns>
		public bool TryIncreaseCounter() {
			bool restartTimePeriod = false;
			bool warningTriggered = false;

			hitCounter++;

			if (swLimitHit.Elapsed.TotalMilliseconds <= maxPeriodTimeMillis) {
				if (!constantPeriodTimer) {
					restartTimePeriod = true;
				}

				warningTriggered = triggerOncePerPeriod ? hitCounter == hitCounterMax : hitCounter >= hitCounterMax;
			} else {
				restartTimePeriod = true;
			}

			if (restartTimePeriod) {
				swLimitHit.Restart();
				hitCounter = 0;
			}

			//This can happen if the max counter is set to 1 and they are handling the stopwatch
			//	manually (because otherwise it makes no sense to use this class with that counter).
			if (!swLimitHit.IsRunning) {
				swLimitHit.Start();
			}

			return !warningTriggered;
		}

		/// <summary>Manually starts (or restarts) the internal stopwatch.</summary>
		public void StartTime() {
			swLimitHit.Restart();
		}

		/// <summary>Resets and stops the internal stopwatch and set the counter to zero.</summary>
		public void ResetAll() {
			hitCounter = 0;
			swLimitHit.Reset();
		}

	}
}
