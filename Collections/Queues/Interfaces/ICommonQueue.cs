namespace Damntry.Utils.Collections.Queues.Interfaces {

	/// <summary>
	/// Defines some common Queue methods so we can use both different queue implementations interchangeably.
	/// </summary>
	public interface ICommonQueue<T> {

		bool TryEnqueue(T item);

		bool TryDequeue(out T item);

		int Count { get; }

	}

}
