using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Vertx.Utilities
{
	public static partial class InstancePool
	{
		private const string instancePoolSceneName = "Instance Pool";
		private static Scene instancePoolScene;

		private static Scene GetInstancePoolScene()
		{
			if (instancePoolScene.IsValid() && instancePoolScene.isLoaded)
				return instancePoolScene;
			
			instancePoolScene = GetNewScene();
			return instancePoolScene;

			Scene GetNewScene()
			{
				instancePoolScene = SceneManager.GetSceneByName(instancePoolSceneName);
				return instancePoolScene.IsValid() ? instancePoolScene : SceneManager.CreateScene(instancePoolSceneName, new CreateSceneParameters(LocalPhysicsMode.None));
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		static void ResetStaticInstances()
		{
			foreach (IComponentPool pool in instancePools)
				pool.RemoveAllPrefabPools();
			instancePools.Clear();
		}

		internal static readonly HashSet<IComponentPool> instancePools = new HashSet<IComponentPool>();

		/// <summary>
		/// Destroys extra instances beyond the capacities set (or defaulted to.)
		/// </summary>
		/// <param name="defaultCapacity">The default maximum amount of instances kept when <see cref="TrimExcess"/> is called</param>
		public static void TrimExcess(int defaultCapacity = 20)
		{
			foreach (IComponentPool pool in instancePools)
				pool.TrimExcess(defaultCapacity);
		}
		
		/// <summary>
		/// Moves a GameObject instance to the Instance Pool scene. This will not pool the object.
		/// </summary>
		/// <param name="instance">A Component attached to the GameObject that will be moved.</param>
		public static void MoveToInstancePoolScene(Component instance) => MoveToInstancePoolScene(instance.gameObject);

		/// <summary>
		/// Moves a GameObject instance to the Instance Pool scene. This will not pool the object.
		/// </summary>
		/// <param name="instance">The GameObject that will be moved.</param>
		public static void MoveToInstancePoolScene(GameObject instance)
		{
			instance.transform.SetParent(null);
			SceneManager.MoveGameObjectToScene(instance, GetInstancePoolScene());
		}
	}
	
	internal interface IComponentPool
	{
		void TrimExcess(int defaultCapacity);
		void RemoveAllPrefabPools();
	}

	public static partial class InstancePool<TInstanceType> where TInstanceType : Component
	{
		private static readonly ComponentPool<TInstanceType> componentPool = new ComponentPool<TInstanceType>();

		static InstancePool() => InstancePool.instancePools.Add(componentPool);
	}

	/// <summary>
	/// A pool for Component instances.
	/// </summary>
	/// <typeparam name="TInstanceType">The Component Type associated with the pool</typeparam>
	internal class ComponentPool<TInstanceType> : IComponentPool where TInstanceType : Component
	{
		/// <summary>
		/// Dictionary of prefab components to HashSets of pooled instances.
		/// </summary>
		private readonly Dictionary<TInstanceType, HashSet<TInstanceType>> pool = new Dictionary<TInstanceType, HashSet<TInstanceType>>();

		/// <summary>
		/// Returns the amount of pooled instances associated with a prefab key.
		/// </summary>
		/// <param name="key">The prefab key.</param>
		/// <returns>The amount of pooled instances associated with the key.</returns>
		public int GetCurrentlyPooledCount(TInstanceType key) => !pool.TryGetValue(key, out var set) ? 0 : set.Count;


		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="prefab"/> pooled.
		/// </summary>
		/// <param name="prefab">The prefab key to instance.</param>
		/// <param name="count">The amount to ensure is pooled.</param>
		/// <param name="parent">Optional parent</param>
		public void Warmup(TInstanceType prefab, int count, Transform parent = null)
		{
			if (!pool.TryGetValue(prefab, out var hashSet))
				pool.Add(prefab, hashSet = new HashSet<TInstanceType>());
			for (int i = hashSet.Count; i < count; i++)
			{
				var instance = Object.Instantiate(prefab, parent);
				instance.name = prefab.name;
				instance.gameObject.SetActive(false);
				InstancePool.MoveToInstancePoolScene(instance);
				hashSet.Add(instance);
			}
		}

		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="prefab"/> pooled.
		/// </summary>
		/// <param name="prefab">The prefab key to instance.</param>
		/// <param name="count">The amount to ensure is pooled.</param>
		/// <param name="parent">Optional parent</param>
		/// <param name="instancesPerFrame">The amount of instances created per frame</param>
		public IEnumerator WarmupCoroutine(TInstanceType prefab, int count, Transform parent = null, int instancesPerFrame = 1)
		{
			if (!pool.TryGetValue(prefab, out var hashSet))
				pool.Add(prefab, hashSet = new HashSet<TInstanceType>());
			while (hashSet.Count < count)
			{
				int amount = Mathf.Clamp(instancesPerFrame, 0, count - hashSet.Count);
				for (int i = 0; i < amount; i++)
				{
					var instance = Object.Instantiate(prefab, parent);
					instance.name = prefab.name;
					instance.gameObject.SetActive(false);
					InstancePool.MoveToInstancePoolScene(instance);
					hashSet.Add(instance);
				}
				yield return null;
			}
		}

		/// <summary>
		/// Retrieves an instance from the pool, positioned at the origin.
		/// </summary>
		/// <param name="prefab">The prefab key to retrieve instances of.</param>
		/// <param name="parent">The parent to parent instances under.</param>
		/// <returns>An instance retrieved from the pool.</returns>
		public TInstanceType Get(TInstanceType prefab, Transform parent = null)
		{
			Transform prefabTransform = prefab.transform;
			var position = prefabTransform.localPosition;
			var rotation = prefabTransform.localRotation;
			return Get(prefab, parent, position, rotation);
		}

		/// <summary>
		/// Retrieves a positioned instance from the pool.
		/// </summary>
		/// <param name="prefab">The prefab key to retrieve instances of.</param>
		/// <param name="parent">The parent to parent instances under.</param>
		/// <param name="position">Position of the instance</param>
		/// <param name="rotation">Rotation of the instance</param>
		/// <param name="space">Which space the position and rotation is applied in</param>
		/// <returns>An instance retrieved from the pool.</returns>
		public TInstanceType Get(TInstanceType prefab, Transform parent, Vector3 position, Quaternion rotation, Space space = Space.World)
		{
			var scale = prefab.transform.localScale;
			return Get(prefab, parent, position, rotation, scale, space);
		}

		/// <summary>
		/// Retrieves a positioned instance from the pool.
		/// </summary>
		/// <param name="prefab">The prefab key to retrieve instances of.</param>
		/// <param name="parent">The parent to parent instances under.</param>
		/// <param name="position">Position of the instance</param>
		/// <param name="rotation">Rotation of the instance</param>
		/// <param name="localScale">Local Scale of the instance</param>
		/// <param name="space">Which space the position and rotation is applied in</param>
		/// <returns>An instance retrieved from the pool.</returns>
		public TInstanceType Get(TInstanceType prefab, Transform parent, Vector3 position, Quaternion rotation, Vector3 localScale, Space space = Space.World)
		{
			Assert.IsNotNull(prefab, $"Prefab passed to InstancePool<{typeof(TInstanceType).Name}>{nameof(Get)} was null");

			// Use the pool if we have one already
			if (pool.TryGetValue(prefab, out var hashSet))
			{
				if (hashSet.Count > 0)
				{
					TInstanceType poppedInstance = null;
					bool found = false;
					bool hasNull = false;
					foreach (TInstanceType i in hashSet)
					{
						found = i != null;
						if (found)
						{
							poppedInstance = i;
							break;
						}

						hasNull = true;
					}

					if (hasNull)
						hashSet.RemoveWhere(i => i == null);

					if (found)
					{
						hashSet.Remove(poppedInstance);
						
						// Activate and re-parent
						poppedInstance.gameObject.SetActive(true);
						Transform t = poppedInstance.transform;
						if (t.parent != parent)
							t.SetParent(parent);

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

						return poppedInstance;
					}
				}
			}

			// Otherwise return a new instance.
			// Only when an instance is returned do we need to create a pool.
			TInstanceType instance;
			switch (space)
			{
				case Space.World:
				{
					instance = Object.Instantiate(prefab, position, rotation, parent);
					Transform t = instance.transform;
					t.localScale = localScale;
					break;
				}
				case Space.Self:
				{
					instance = Object.Instantiate(prefab, parent);
					Transform t = instance.transform;
					t.localPosition = position;
					t.localRotation = rotation;
					t.localScale = localScale;
					break;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(space), space, null);
			}

			instance.name = prefab.name;
			return instance;
		}

		/// <summary>
		/// Returns a Component instance to the pool.
		/// </summary>
		/// <param name="prefab">The prefab key used when the instance was retrieved via <see cref="Get(TInstanceType,UnityEngine.Transform)"/></param>
		/// <param name="instance">The instance to return to the pool.</param>
		public void Pool(TInstanceType prefab, TInstanceType instance)
		{
			Assert.IsNotNull(prefab, $"Prefab passed to InstancePool<{typeof(TInstanceType).Name}>{nameof(Pool)} was null");
			Assert.IsNotNull(instance, $"Instance passed to InstancePool<{typeof(TInstanceType).Name}>{nameof(Pool)} was null");
			
			// Create a pool if we don't have one already.
			if (!pool.TryGetValue(prefab, out var hashSet))
			{
				hashSet = new HashSet<TInstanceType>();
				pool.Add(prefab, hashSet);
				hashSet.Add(instance);
			}
			else
			{
				if (hashSet.Contains(instance))
				{
					#if UNITY_EDITOR
					Debug.LogWarning($"Item {instance} is requested to be pooled for a second time. The request has been ignored.");
					#endif
				}
				else
				{
					hashSet.Add(instance);
				}
			}

			// Disable the object and push it to the HashSet.
			instance.gameObject.SetActive(false);
			InstancePool.MoveToInstancePoolScene(instance);
		}

		/// <summary>
		/// Queries whether the pool contains a specific instance of a prefab.
		/// </summary>
		/// <param name="prefab">The prefab key referring to the pool.</param>
		/// <param name="instance">The instance we are querying.</param>
		/// <returns>True if the pool contains the queried instance.</returns>
		public bool IsPooled(TInstanceType prefab, TInstanceType instance) => pool.TryGetValue(prefab, out var hashSet) && hashSet.Contains(instance);

		/// <summary>
		/// If you are temporarily working with pools for prefabs you can remove them from the system by calling this function.
		/// This will not remove the instances that are currently pooled. Un-pool all instances before calling this function.
		/// </summary>
		/// <param name="prefab">The prefab key referring to the pool.</param>
		public void RemovePrefabPool(TInstanceType prefab)
		{
			if (pool.ContainsKey(prefab))
				pool.Remove(prefab);
		}

		
		void IComponentPool.RemoveAllPrefabPools()
		{
			pool.Clear();
		}

		#region Capacity
		
		private readonly Dictionary<TInstanceType, int> capacities = new Dictionary<TInstanceType, int>();

		/// <summary>
		/// Sets the capacity used by <see cref="TrimExcess"/> for all instances shared between the type <see cref="TInstanceType"/>
		/// </summary>
		/// <param name="capacity">The maximum amount of instances kept when <see cref="TrimExcess"/> is called.</param>
		public void SetCapacities(int capacity)
		{
			foreach (var pair in pool)
				capacities[pair.Key] = capacity;
		}

		/// <summary>
		/// Sets the capacity used by <see cref="TrimExcess"/>
		/// </summary>
		/// <param name="prefab">The prefab used as a key within the pool.</param>
		/// <param name="capacity">The maximum amount of instances kept when <see cref="TrimExcess"/> is called.</param>
		public void SetCapacity(TInstanceType prefab, int capacity) 
			=> capacities[prefab] = capacity;

		/// <summary>
		/// Destroys extra instances beyond the capacities set (or defaulted to.)
		/// </summary>
		/// <param name="defaultCapacity">The default maximum amount of instances kept when <see cref="TrimExcess"/> is called
		/// if <see cref="SetCapacity"/> or <see cref="SetCapacities"/> was not set.</param>
		public void TrimExcess(int defaultCapacity = 20)
		{
			HashSet<TInstanceType> temp = new HashSet<TInstanceType>();
			foreach (var pair in pool)
			{
				if (!capacities.TryGetValue(pair.Key, out int capacity))
					capacity = defaultCapacity;
				
				HashSet<TInstanceType> instances = pair.Value;
				if(instances.Count <= capacity) continue;
				
				temp.Clear();
				int c = 0;
				foreach (var instance in instances)
				{
					if (c++ < capacity)
						temp.Add(instance);
					else
						Object.Destroy(instance.gameObject);
				}
				instances.IntersectWith(temp);
			}
		}

		/// <summary>
		/// Destroys extra instances beyond the capacities set (or defaulted to.)
		/// </summary>
		/// <param name="prefab">The prefab used as a key within the pool.</param>
		/// <param name="defaultCapacity">The default maximum amount of instances kept when <see cref="TrimExcess"/> is called
		/// if <see cref="SetCapacity"/> or <see cref="SetCapacities"/> was not set.</param>
		public void TrimExcess(TInstanceType prefab, int defaultCapacity = 20)
		{
			if (!pool.TryGetValue(prefab, out var instances))
				return;
			
			if (!capacities.TryGetValue(prefab, out int capacity))
				capacity = defaultCapacity;
			
			if(instances.Count <= capacity) return;
			
			HashSet<TInstanceType> temp = new HashSet<TInstanceType>();
			int c = 0;
			foreach (var instance in instances)
			{
				if (c++ < capacity)
					temp.Add(instance);
				else
					Object.Destroy(instance.gameObject);
			}
			instances.IntersectWith(temp);
		}

		#endregion
	}
}