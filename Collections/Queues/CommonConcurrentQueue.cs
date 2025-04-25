using System.Collections.Concurrent;
using Damntry.Utils.Collections.Queues.Interfaces;

namespace Damntry.Utils.Collections.Queues {
	public class CommonConcurrentQueue<T> : ConcurrentQueue<T>, ICommonQueue<T> {

		public bool TryEnqueue(T item) {
			Enqueue(item);
			return true;
		}

		public new bool TryDequeue(out T item) {
			return base.TryDequeue(out item);
		}
	}
}
