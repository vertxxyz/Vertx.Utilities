using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Vertx.Utilities
{
	/// <summary>
	/// A fixed-capacity pool that uses the least recent entry when <see cref="CircularPool{TInstanceType}.Get"/> is called.<br/>
	/// Instances are always considered a part of the pool, and can be requested at any moment.<br/>
	/// The pool will grow to the internal size unless warmed up beforehand.<br/>
	/// This pool use useful in circumstances where there's a fixed count of an unimportant resource, like bullet hole decals for example.
	/// </summary>
	/// <typeparam name="TInstanceType">The type of the root component on the prefab we want to pool.</typeparam>
	public class CircularPool<TInstanceType> : IComponentPool<TInstanceType> where TInstanceType : Component
	{
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

		/// <inheritdoc />
		public TInstanceType Prefab => _prefab;

		private readonly List<TInstanceType> _instances;
		private readonly TInstanceType _prefab;
		private int _capacity;
		private int _currentIndex;

		private CircularPool() { }

		public CircularPool(TInstanceType prefab, int capacity = InstancePool.DefaultPoolCapacity)
		{
			Capacity = capacity;
			_prefab = prefab;
			_instances = new List<TInstanceType>(capacity);
		}

		/// <inheritdoc />
		public void Warmup(int count, Transform parent = null)
			=> ComponentPoolHelper.Warmup(this, count, parent);

		/// <inheritdoc />
		public IEnumerator WarmupCoroutine(int count, Transform parent = null, int instancesPerFrame = 1)
			=> ComponentPoolHelper.WarmupCoroutine(this, count, parent, instancesPerFrame);

		/// <inheritdoc />
		public TInstanceType Get(Transform parent, Vector3 position, Quaternion rotation, Vector3 localScale, Space space = Space.World)
		{
			while (true)
			{
				TInstanceType instance;
				if (_instances.Count < Capacity)
				{
					// We are not yet at capacity, add a new instance to the pool and return it.
					instance = ComponentPoolHelper.CreateInstance(Prefab, parent, position, rotation, localScale, space);
					_instances.Add(instance);
					return instance;
				}

				int count = _instances.Count;
				if (_currentIndex >= count)
					_currentIndex = 0;

				instance = _instances[_currentIndex];
				if (instance == null)
				{
					// Index is null, remove it and try again.
					_instances.RemoveUnorderedAt(_currentIndex);
					continue;
				}

				_currentIndex++;
				ComponentPoolHelper.PositionInstance(instance, parent, position, rotation, localScale, space);
				return instance;
			}
		}

		/// <inheritdoc />
		public void Pool(TInstanceType instance)
		{
			if (Contains(instance))
			{
				// Instance was pooled early. Just disable it.
				ComponentPoolHelper.DisableAndMoveToInstancePoolScene(instance);
				return;
			}

			if (_instances.Count >= Capacity)
			{
				// Instance cannot be pooled, we are at capacity.
				Object.Destroy(instance.gameObject);
				return;
			}

			_instances.Add(instance);
			ComponentPoolHelper.DisableAndMoveToInstancePoolScene(instance);
		}

		/// <inheritdoc />
		public bool Contains(TInstanceType instance)
		{
			// Checks if the collection contains the instance by checking from oldest to newest.
			int i = _currentIndex;
			for (; i < _instances.Count; i++)
			{
				if (instance == _instances[i])
					return true;
			}

			for (i = 0; i < _currentIndex; i++)
			{
				if (instance == _instances[i])
					return true;
			}

			return false;
		}

		/// <inheritdoc />
		public void TrimExcess()
		{
			if (_instances.Count <= Capacity) return;

			// Start from the start, removing any null indices so we hopefully have at least capacity worth of instances.
			for (var i = 0; i < _instances.Count; i++)
			{
				TInstanceType instance = _instances[i];
				if (instance == null)
				{
					_instances.RemoveUnorderedAt(i--);
					continue;
				}

				if (i >= Capacity)
					break;
			}

			// Remove the remaining instances past the capacity.
			for (int i = _instances.Count - 1; i >= Capacity; i--)
			{
				TInstanceType instance = _instances[i];
				_instances.RemoveAt(i);
				if (instance == null)
					continue;
				Object.Destroy(instance.gameObject);
			}

			_instances.Capacity = _capacity;
		}

		IEnumerator<TInstanceType> IEnumerable<TInstanceType>.GetEnumerator() => ((IEnumerable<TInstanceType>)_instances).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _instances.GetEnumerator();
	}
}