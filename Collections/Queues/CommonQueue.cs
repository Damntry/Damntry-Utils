using System.Collections.Generic;
using Damntry.Utils.Collections.Queues.Interfaces;

namespace Damntry.Utils.Collections.Queues {

	public class CommonQueue<T> : Queue<T>, ICommonQueue<T> {
		
		public bool TryEnqueue(T item) {
			base.Enqueue(item);
			return true;
		}

		public bool TryDequeue(out T item) {
			item = Dequeue();
			return true;
		}
	}

}
