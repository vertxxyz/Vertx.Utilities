using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vertx.Utilities
{
	public interface IComponentPool<TInstanceType> : IEnumerable<TInstanceType> where TInstanceType : Component
	{
		/// <summary>
		/// The capacity of the pool. For expandable pools this is only used when <see cref="TrimExcess"/> is called.
		/// </summary>
		int Capacity { get; set; }

		/// <summary>
		/// The prefab used to create instances for this pool, and act as a key for <see cref="InstancePool"/>.
		/// </summary>
		TInstanceType Prefab { get; }

		/// <summary>
		/// The amount of pooled instances.
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="Prefab"/> pooled.
		/// </summary>
		/// <param name="count">The amount to ensure is pooled. The pool will not grow past its capacity.</param>
		/// <param name="parent">Optional parent.</param>
		void Warmup(int count, Transform parent = null);

		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="prefab"/> pooled.
		/// </summary>
		/// <param name="count">The amount to ensure is pooled. The pool will not grow past its capacity.</param>
		/// <param name="parent">Optional parent.</param>
		/// <param name="instancesPerFrame">The amount of instances created per frame.</param>
		IEnumerator WarmupCoroutine(int count, Transform parent = null, int instancesPerFrame = 1);

		/// <summary>
		/// Retrieves a positioned instance from the pool.
		/// </summary>
		/// <param name="parent">The parent to parent instances under.</param>
		/// <param name="position">Position of the instance</param>
		/// <param name="rotation">Rotation of the instance</param>
		/// <param name="localScale">Local Scale of the instance</param>
		/// <param name="space">Which space the position and rotation is applied in</param>
		/// <returns>An instance retrieved from the pool.</returns>
		TInstanceType Get(Transform parent, Vector3 position, Quaternion rotation, Vector3 localScale, Space space = Space.World);

		/// <summary>
		/// Returns a Component instance to the pool.
		/// </summary>
		/// <param name="instance">The instance to return to the pool.</param>
		void Pool(TInstanceType instance);

		/// <summary>
		/// Queries whether the pool contains a specific instance of a prefab.
		/// </summary>
		/// <param name="instance">The instance we are querying.</param>
		/// <returns>True if the pool contains the queried instance.</returns>
		bool Contains(TInstanceType instance);

		/// <summary>
		/// Destroys extra instances beyond the set capacity.
		/// </summary>
		void TrimExcess();
	}
}