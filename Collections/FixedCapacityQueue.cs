﻿using System;
using System.Collections.Generic;

namespace Damntry.Utils.Collections {
	public class FixedCapacityQueue<T> : Queue<T> {

		private int maxCapacity;

		private bool keepOld;


		/// <param name="maxCapacity">Max number of queued elements.</param>
		/// <param name="keepOld">
		/// If when queuing a new element where the Queue is already at capacity, should we 
		/// remove the oldest item and queue the new one, or skip queuing the new element.
		/// </param>
		public FixedCapacityQueue(int maxCapacity, bool keepOld) : base(maxCapacity) {
			this.maxCapacity = maxCapacity;
			this.keepOld = keepOld;
		}

		/// <summary>Use <see cref="TryEnqueue(T, out T)"/> instead.</summary>
		/// <exception cref="NotImplementedException"></exception>
		[Obsolete("Use \"Enqueue(T item, out T dequeuedItem)\" instead.")]
		public new T Enqueue(T item) {
			throw new NotImplementedException("Use \"Enqueue(T item, out T dequeuedItem)\" instead.");
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
			bool itemQueued = false;
			dequeuedItem = default;

			if (Count > maxCapacity) {
				throw new InvalidOperationException("This LimitedQueue contains more items than allowed. " +
					"Avoid casting this FixedCapacityQueue instance into a Queue to call the base Enqueue(T) method.");
			}
			if (!keepOld && Count == maxCapacity) {
				dequeuedItem = Dequeue();
			}

			if (Count < maxCapacity) {
				base.Enqueue(item);
				itemQueued = true;
			}
			
			return itemQueued;
		}
	}

}
