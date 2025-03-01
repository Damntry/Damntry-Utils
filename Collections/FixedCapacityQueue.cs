using System;
using System.Collections.Generic;

namespace Damntry.Utils.Collections {
	public class FixedCapacityQueue<T> : Queue<T> {

		private int maxCapacity;

		public FixedCapacityQueue(int maxCapacity) : base(maxCapacity) {
			this.maxCapacity = maxCapacity;
		}

		/// <summary>Use <see cref="Enqueue(T, out T)"/> instead.</summary>
		/// <exception cref="NotImplementedException"></exception>
		[Obsolete("Use \"Enqueue(T item, out T dequeuedItem)\" instead.")]
		public new T Enqueue(T item) {
			throw new NotImplementedException("Use \"Enqueue(T item, out T dequeuedItem)\" instead.");
		}

		/// <summary>
		/// Enqueues a new item. If the queue is full, the oldest items at the front of the 
		/// queue will be dequeued and returned in <paramref name="dequeuedItem"/>
		/// </summary>
		/// <param name="item">The item to queue.</param>
		/// <param name="dequeuedItem">The oldest item at the front of the queued,
		/// if over capacity, otherwise default(T)</param>
		/// <returns>True if an item was dequeued.</returns>
		/// <exception cref="InvalidOperationException">Exception if the Queue is over capacity.</exception>
		public bool Enqueue(T item, out T dequeuedItem) {
			bool itemDequeued = false;
			dequeuedItem = default;

			if (Count > maxCapacity) {
				throw new InvalidOperationException("This LimitedQueue contains more items than allowed. " +
					"Avoid casting this FixedCapacityQueue instance into a Queue to call the base Enqueue(T) method.");
			}
			if (Count == maxCapacity) {
				dequeuedItem = Dequeue();
				itemDequeued = true;
			}

			base.Enqueue(item);

			return itemDequeued;
		}
	}

}
