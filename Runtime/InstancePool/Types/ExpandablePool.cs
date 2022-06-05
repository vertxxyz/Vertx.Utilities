using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vertx.Utilities
{
	/// <summary>
	/// A pool that will expand to contain all instances pooled into it.
	/// Only instances that are freed to the pool can be returned by <see cref="ExpandablePool{TInstanceType}.Get"/>.
	/// </summary>
	/// <typeparam name="TInstanceType">The type of the root component on the prefab we want to pool.</typeparam>
	public class ExpandablePool<TInstanceType> : IComponentPool<TInstanceType> where TInstanceType : Component
	{
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

		public int Count => _instances.Count;

		public TInstanceType Prefab => _prefab;

		private readonly HashSet<TInstanceType> _instances;
		private readonly TInstanceType _prefab;
		private int _capacity;

		/// <summary>
		/// Create a pool that will expand to contain all instances pooled into it.
		/// </summary>
		/// <param name="prefab">The prefab used to create instances when <see cref="ExpandablePool{TInstanceType}.Get"/> is called.</param>
		/// <param name="capacity">The capacity used when <see cref="ExpandablePool{TInstanceType}.TrimExcess"/> is called.</param>
		public ExpandablePool(TInstanceType prefab, int capacity = 20)
		{
			Capacity = capacity;
			_prefab = prefab;
			_instances = new HashSet<TInstanceType>();
		}

		public void Warmup(int count, Transform parent = null) => ComponentPoolHelper.Warmup(this, count, parent);

		public IEnumerator WarmupCoroutine(int count, Transform parent = null, int instancesPerFrame = 1)
			=> ComponentPoolHelper.WarmupCoroutine(this, count, parent, instancesPerFrame);

		public TInstanceType Get(Transform parent, Vector3 position, Quaternion rotation, Vector3 localScale, Space space = Space.World)
		{
			if (!TryPop(out TInstanceType instance))
				return ComponentPoolHelper.CreateInstance(Prefab, parent, position, rotation, localScale, space);
			ComponentPoolHelper.PositionInstance(instance, parent, position, rotation, localScale, space);
			return instance;
		}

		private bool TryPop(out TInstanceType instance)
		{
			instance = null;
			if (_instances.Count == 0)
				return false;

			bool found = false;
			bool hasNull = false;
			foreach (TInstanceType i in _instances)
			{
				found = i != null;
				if (found)
				{
					instance = i;
					break;
				}

				hasNull = true;
			}

			if (hasNull)
				_instances.RemoveWhere(i => i == null);
			if (found)
				_instances.Remove(instance);
			return found;
		}

		public void Pool(TInstanceType instance)
		{
			if (!_instances.Add(instance))
			{
#if UNITY_EDITOR
				Debug.LogWarning($"Item {instance} is requested to be pooled for a second time. The request has been ignored.");
#endif
			}

			ComponentPoolHelper.DisableAndMoveToInstancePoolScene(instance);
		}

		public bool Contains(TInstanceType instance) => _instances.Contains(instance);

		public void TrimExcess(HashSet<TInstanceType> temp)
		{
			if (_instances.Count <= Capacity) return;
			int c = 0;
			foreach (var instance in _instances)
			{
				if (instance == null) continue;
				if (c++ < Capacity)
					temp.Add(instance);
				else
					Object.Destroy(instance.gameObject);
			}

			_instances.IntersectWith(temp);
		}

		public void TrimExcess()
		{
			if (_instances.Count <= Capacity) return;
#if UNITY_2021_1_OR_NEWER
			using (UnityEngine.Pool.HashSetPool<TInstanceType>.Get(out HashSet<TInstanceType> temp))
#else
			HashSet<TInstanceType> temp = new HashSet<TInstanceType>();
#endif
				TrimExcess(temp);
		}

		IEnumerator<TInstanceType> IEnumerable<TInstanceType>.GetEnumerator() => ((IEnumerable<TInstanceType>)_instances).GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => _instances.GetEnumerator();
	}
}