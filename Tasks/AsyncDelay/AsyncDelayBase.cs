using System;
using System.Threading;
using System.Threading.Tasks;

namespace Damntry.Utils.Tasks.AsyncDelay {

	/// <summary>
	/// Base class to use in methods that use an async Delay so they 
	/// can be decoupled from the specific Delay functionality.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class AsyncDelayBase<T> where T : AsyncDelayBase<T> {

		private static AsyncDelayBase<T> instance = Activator.CreateInstance<T>();

		public static AsyncDelayBase<T> Instance {
			get { return instance; }
		}

		public abstract Task Delay(int millisecondsDelay);

		public abstract Task Delay(int millisecondsDelay, CancellationToken cancellationToken);

	}

}
