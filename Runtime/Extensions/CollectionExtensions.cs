using System.Collections.Generic;

namespace Vertx.Utilities
{
	public static class CollectionExtensions
	{
		/// <summary>
		/// Removes an item from a list without caring about maintaining order.<br/>
		/// (Moves the last element into the hole and removes it from the end)
		/// </summary>
		public static void RemoveUnordered<T>(this IList<T> list, T item)
		{
			int indexOf = list.IndexOf(item);
			if (indexOf < 0)
				return;
			list.RemoveUnorderedAt(indexOf);
		}

		/// <summary>
		/// Removes an item from a list without caring about maintaining order.<br/>
		/// (Moves the last element into the hole and removes it from the end)
		/// </summary>
		public static void RemoveUnorderedAt<T>(this IList<T> list, int index)
		{
			int endIndex = list.Count - 1;
			list[index] = list[endIndex];
			list.RemoveAt(endIndex);
		}
	}
}