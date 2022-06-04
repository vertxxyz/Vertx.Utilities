using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Vertx.Utilities
{
	public abstract class ComponentPoolBase<TInstanceType> : IEnumerable<TInstanceType> where TInstanceType : Component
	{
		public int Capacity { get; set; }
		public TInstanceType Prefab => _prefab;
		private readonly IPoolCollection<TInstanceType> _instances;
		private readonly TInstanceType _prefab;

		protected ComponentPoolBase(IPoolCollection<TInstanceType> collection, TInstanceType prefab, int capacity = 20)
		{
			_instances = collection;
			_prefab = prefab;
			Capacity = capacity;
		}

		/// <summary>
		/// Returns the amount of pooled instances associated with a prefab key.
		/// </summary>
		/// <returns>The amount of pooled instances associated with the key.</returns>
		public int GetCurrentlyPooledCount() => _instances.Count;

		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="Prefab"/> pooled.
		/// </summary>
		/// <param name="count">The amount to ensure is pooled.</param>
		/// <param name="parent">Optional parent.</param>
		public void Warmup(int count, Transform parent = null)
		{
			for (int i = _instances.Count; i < count; i++)
			{
				var instance = Object.Instantiate(_prefab, parent);
				instance.name = _prefab.name;
				instance.gameObject.SetActive(false);
				InstancePool.MoveToInstancePoolScene(instance);
				_instances.Push(instance);
			}
		}

		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="prefab"/> pooled.
		/// </summary>
		/// <param name="count">The amount to ensure is pooled.</param>
		/// <param name="parent">Optional parent.</param>
		/// <param name="instancesPerFrame">The amount of instances created per frame.</param>
		public IEnumerator WarmupCoroutine(int count, Transform parent = null, int instancesPerFrame = 1)
		{
			while (_instances.Count < count)
			{
				int amount = Mathf.Clamp(instancesPerFrame, 0, count - _instances.Count);
				for (int i = 0; i < amount; i++)
				{
					var instance = Object.Instantiate(_prefab, parent);
					instance.name = _prefab.name;
					instance.gameObject.SetActive(false);
					InstancePool.MoveToInstancePoolScene(instance);
					_instances.Push(instance);
				}

				yield return null;
			}
		}

		/// <summary>
		/// Retrieves a positioned instance from the pool.
		/// </summary>
		/// <param name="parent">The parent to parent instances under.</param>
		/// <param name="position">Position of the instance</param>
		/// <param name="rotation">Rotation of the instance</param>
		/// <param name="localScale">Local Scale of the instance</param>
		/// <param name="space">Which space the position and rotation is applied in</param>
		/// <returns>An instance retrieved from the pool.</returns>
		public TInstanceType Get(Transform parent, Vector3 position, Quaternion rotation, Vector3 localScale, Space space = Space.World)
		{
			if (_instances.TryPop(out TInstanceType instance))
			{
				// Activate and re-parent
				GameObject poppedInstanceGameObject = instance.gameObject;
				poppedInstanceGameObject.SetActive(true);
				Transform t = instance.transform;
				if (t.parent != parent)
					t.SetParent(parent);
				else
					SceneManager.MoveGameObjectToScene(poppedInstanceGameObject, SceneManager.GetActiveScene());

				//Position
				switch (space)
				{
					case Space.World:
						t.SetPositionAndRotation(position, rotation);
						t.localScale = localScale;
						break;
					case Space.Self:
						t.localPosition = position;
						t.localRotation = rotation;
						t.localScale = localScale;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(space), space, null);
				}

				return instance;
			}

			return ComponentPoolGroup<TInstanceType>.CreateInstance(_prefab, parent, position, rotation, localScale, space);
		}

		/// <summary>
		/// Returns a Component instance to the pool.
		/// </summary>
		/// <param name="instance">The instance to return to the pool.</param>
		public void Pool(TInstanceType instance)
		{
			if (!_instances.Push(instance))
			{
#if UNITY_EDITOR
				Debug.LogWarning($"Item {instance} is requested to be pooled for a second time. The request has been ignored.");
#endif
			}

			// Disable the object and push it to the HashSet.
			instance.gameObject.SetActive(false);
			InstancePool.MoveToInstancePoolScene(instance);
		}
		
		/// <summary>
		/// Queries whether the pool contains a specific instance of a prefab.
		/// </summary>
		/// <param name="instance">The instance we are querying.</param>
		/// <returns>True if the pool contains the queried instance.</returns>
		public bool IsPooled(TInstanceType instance) => _instances.Contains(instance);
		
		/// <summary>
        /// Destroys extra instances beyond the set capacity.
        /// </summary>
        internal void TrimExcess(HashSet<TInstanceType> temp)
        {
        	if (_instances.Count <= Capacity) return;
            _instances.TrimExcess(Capacity, temp);
        }

        /// <summary>
        /// Destroys extra instances beyond the set capacity.
        /// </summary>
		public void TrimExcess()
		{
			if (_instances.Count <= Capacity) return;

#if UNITY_2021_1_OR_NEWER
			using (UnityEngine.Pool.HashSetPool<TInstanceType>.Get(out HashSet<TInstanceType> temp))
#else
			HashSet<TInstanceType> temp = new HashSet<TInstanceType>();
#endif
			_instances.TrimExcess(Capacity, temp);
		}
		
		IEnumerator<TInstanceType> IEnumerable<TInstanceType>.GetEnumerator() => _instances.GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => _instances.GetEnumerator();
	}
}