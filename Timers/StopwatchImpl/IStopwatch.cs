namespace Damntry.Utils.Timers.StopwatchImpl {

	public interface IStopwatch {

		public bool IsRunning { get; }

		public long ElapsedMilliseconds { get; }

		public long ElapsedSeconds { get; }

		public double ElapsedMillisecondsPrecise { get; }

		public double ElapsedSecondsPrecise { get; }

		public void Start();

		public void Stop();

		public void Reset();

		public void Restart();
	}

}
