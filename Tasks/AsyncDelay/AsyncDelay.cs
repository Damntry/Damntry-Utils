using System;
using System.Threading;
using System.Threading.Tasks;

namespace Damntry.Utils.Tasks.AsyncDelay {

	/// <summary>
	/// Most default implementation of AsyncDelayBase, using Task.Delay.
	/// </summary>
	public class AsyncDelay : AsyncDelayBase<AsyncDelay> {

		public override Task Delay(int millisecondsDelay) {
			return Task.Delay(millisecondsDelay);
		}

		public override Task Delay(int millisecondsDelay, CancellationToken cancellationToken) {
			return Task.Delay(millisecondsDelay, cancellationToken);
		}

	}

}
