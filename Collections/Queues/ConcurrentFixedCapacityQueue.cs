using System;
using System.Collections.Concurrent;
using Damntry.Utils.Collections.Queues.Interfaces;

namespace Damntry.Utils.Collections.Queues {
	public class ConcurrentFixedCapacityQueue<T> : ConcurrentQueue<T>, ICommonQueue<T> {

		private readonly int maxCapacity;

		private readonly bool keepOld;

		private readonly object queueLock;

		/// <param name="maxCapacity">Max number of queued elements.</param>
		/// <param name="keepOld">
		/// If when queuing a new element where the Queue is already at capacity, should we 
		/// remove the oldest item and queue the new one, or skip queuing the new element.
		/// </param>
		public ConcurrentFixedCapacityQueue(int maxCapacity, bool keepOld) {
			if (maxCapacity <= 0) {
				throw new ArgumentOutOfRangeException("Only values over zero are allowed.");
			}
			this.maxCapacity = maxCapacity;
			this.keepOld = keepOld;

			queueLock = new();
		}

		/// <summary>Use <see cref="TryEnqueue(T, out T)"/> instead.</summary>
		/// <exception cref="NotImplementedException"></exception>
		[Obsolete("Use \"Enqueue(T item, out T dequeuedItem)\" instead.")]
		public new T Enqueue(T item) {
			throw new NotImplementedException("Use \"Enqueue(T item, out T dequeuedItem)\" instead.");
		}

		public bool TryEnqueue(T item) {
			return TryEnqueue(item, out _);
		}

		/// <summary>
		/// Enqueues a new item. If the queue is full, and <see cref="keepOld"/> is false, the 
		/// oldest items at the front of the queue will be dequeued and returned in 
		/// <paramref name="dequeuedItem"/>
		/// </summary>
		/// <param name="item">The item to queue.</param>
		/// <param name="dequeuedItem">The oldest item at the front of the queued,
		/// if over capacity and <see cref="keepOld"/> is false, otherwise default(T).</param>
		/// <returns>True if item was queued.</returns>
		/// <exception cref="InvalidOperationException">Exception when the Queue has more items 
		/// than its capacity.</exception>
		public bool TryEnqueue(T item, out T dequeuedItem) {
			dequeuedItem = default;

			lock (queueLock) {
				if (Count > maxCapacity) {
					throw new InvalidOperationException("This LimitedQueue contains more items than allowed. " +
						"Avoid casting this FixedCapacityQueue instance into a Queue to call the base Enqueue(T) method.");
				}
				if (!keepOld && Count == maxCapacity) {
					base.TryDequeue(out dequeuedItem);
				}

				if (Count >= maxCapacity) {
					return false;
				}

				base.Enqueue(item);
			}

			return true;
		}

		public new bool TryDequeue(out T item) {
			//Its a waste having to lock when already using ConcurrentQueue, but its needed because of the Enqueue capacity checks.
			lock (queueLock) {
				return base.TryDequeue(out item);
			}
		}
	}
}
