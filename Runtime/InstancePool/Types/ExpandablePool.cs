using UnityEngine;

namespace Vertx.Utilities
{
	/// <summary>
	/// A pool that will expand to contain all instances pooled into it.
	/// Only instances that are freed to the pool can be returned by <see cref="ExpandablePool{TInstanceType}.Get"/>.
	/// </summary>
	/// <typeparam name="TInstanceType">The type of the root component on the prefab we want to pool.</typeparam>
	public class ExpandablePool<TInstanceType> : ComponentPoolBase<TInstanceType> where TInstanceType : Component
	{
		/// <summary>
		/// Create a pool that will expand to contain all instances pooled into it.
		/// </summary>
		/// <param name="prefab">The prefab used to create instances when <see cref="ExpandablePool{TInstanceType}.Get"/> is called.</param>
		/// <param name="capacity">The capacity used when <see cref="ExpandablePool{TInstanceType}.TrimExcess"/> is called.</param>
		public ExpandablePool(TInstanceType prefab, int capacity = 20) : base(new PoolHashSet<TInstanceType>(), prefab, capacity) { }
	}
}