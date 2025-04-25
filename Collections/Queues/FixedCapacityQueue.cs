using System;
using System.Collections.Generic;
using Damntry.Utils.Collections.Queues.Interfaces;

namespace Damntry.Utils.Collections.Queues {

	public class FixedCapacityQueue<T> : Queue<T>, ICommonQueue<T> {

		private readonly int maxCapacity;

		private readonly bool keepOld;


		/// <param name="maxCapacity">Max number of queued elements.</param>
		/// <param name="keepOld">
		/// If when queuing a new element where the Queue is already at capacity, should we 
		/// remove the oldest item and queue the new one, or skip queuing the new element.
		/// </param>
		public FixedCapacityQueue(int maxCapacity, bool keepOld) : base(maxCapacity) {
			if (maxCapacity <= 0) {
				throw new ArgumentOutOfRangeException("Only values over zero are allowed.");
			}
			this.maxCapacity = maxCapacity;
			this.keepOld = keepOld;
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

		public bool TryEnqueue(T item, out T dequeuedItem) {
			dequeuedItem = default;

			if (Count > maxCapacity) {
				throw new InvalidOperationException("This LimitedQueue contains more items than allowed. " +
					"Avoid casting this FixedCapacityQueue instance into a Queue to call the base Enqueue(T) method.");
			}
			if (!keepOld && Count == maxCapacity) {
				dequeuedItem = Dequeue();
			}

			if (Count >= maxCapacity) {
				return false;
			}

			base.Enqueue(item);
			return true;
		}

		public bool TryDequeue(out T item) {
			item = Dequeue();
			return true;
		}
	}

}
