using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vertx.Utilities
{
	/// <summary>
	/// A pool that will expand to contain all instances pooled into it.<br/>
	/// Only instances that are freed to the pool can be returned by <see cref="ExpandablePoolUnchecked{TInstanceType}.Get"/>.<br/>
	/// In comparison to <see cref="ExpandablePool{TInstanceType}"/>, there are no safety checks performed in high-frequency operations.<br/>
	/// If abused, you can enter instances into the pool multiple times or destroy objects that are in the pool, which will result in logic errors and unhandled exceptions.
	/// </summary>
	/// <typeparam name="TInstanceType">The type of the root component on the prefab we want to pool.</typeparam>
	public class ExpandablePoolUnchecked<TInstanceType> : IComponentPool<TInstanceType> where TInstanceType : Component
	{
		/// <inheritdoc />
		public TInstanceType Prefab => _prefab;

		/// <inheritdoc />
		public int Capacity
		{
			get => _capacity;
			set
			{
				if (value <= 0)
				{
					Debug.LogError($"{GetType().Name} had its capacity set to {value}. Capacity was forced to be 1.");
					_capacity = 1;
					return;
				}

				_capacity = value;
			}
		}

		/// <inheritdoc />
		public int Count => _instances.Count;

		private readonly List<TInstanceType> _instances;
		private readonly TInstanceType _prefab;
		private int _capacity;

		private ExpandablePoolUnchecked() { }

		/// <summary>
		/// Create a pool that will expand to contain all instances pooled into it.
		/// </summary>
		/// <param name="prefab">The prefab used to create instances when <see cref="ExpandablePool{TInstanceType}.Get"/> is called.</param>
		/// <param name="capacity">The capacity used when <see cref="ExpandablePool{TInstanceType}.TrimExcess"/> is called.</param>
		public ExpandablePoolUnchecked(TInstanceType prefab, int capacity = InstancePool.DefaultPoolCapacity)
		{
			Capacity = capacity;
			_prefab = prefab;
			_instances = new List<TInstanceType>(capacity);
		}

		/// <inheritdoc />
		public void Warmup(int count, Transform parent = null) => ComponentPoolHelper.Warmup(this, count, parent);

		/// <inheritdoc />
		public IEnumerator WarmupCoroutine(int count, Transform parent = null, int instancesPerFrame = 1)
			=> ComponentPoolHelper.WarmupCoroutine(this, count, parent, instancesPerFrame);

		/// <inheritdoc />
		public TInstanceType Get(Transform parent, Vector3 position, Quaternion rotation, Vector3 localScale, Space space = Space.World)
		{
			if (_instances.Count == 0)
				return ComponentPoolHelper.CreateInstance(Prefab, parent, position, rotation, localScale, space);
			// ReSharper disable once UseIndexFromEndExpression
			int lastIndex = _instances.Count - 1;
			var instance = _instances[lastIndex];
			_instances.RemoveAt(lastIndex);
			ComponentPoolHelper.PositionInstance(instance, parent, position, rotation, localScale, space);
			return instance;
		}

		/// <inheritdoc />
		public void Pool(TInstanceType instance)
		{
			_instances.Add(instance);
			ComponentPoolHelper.DisableAndMoveToInstancePoolScene(instance);
		}

		/// <inheritdoc />
		public bool Contains(TInstanceType instance) => _instances.Contains(instance);

		/// <inheritdoc />
		public void TrimExcess()
		{
			for (int i = _instances.Count - 1; i >= Capacity; i--)
			{
				var instance = _instances[i];
				Object.Destroy(instance.gameObject);
				_instances.RemoveAt(i);
			}
		}

		IEnumerator<TInstanceType> IEnumerable<TInstanceType>.GetEnumerator() => ((IEnumerable<TInstanceType>)_instances).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _instances.GetEnumerator();
	}
}