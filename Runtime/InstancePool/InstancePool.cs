﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ASSERTIONS
using UnityEngine.Assertions;
#endif
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
// ReSharper disable MemberCanBePrivate.Global

namespace Vertx.Utilities
{
	public static partial class InstancePool
	{
		public const string InstancePoolSceneName = "Instance Pool";
		private static Scene s_instancePoolScene;

		/// <summary>
		/// Gets the runtime scene InstancePool moves pooled instances to.
		/// This scene is created if needed.
		/// </summary>
		/// <returns>The Scene used by the InstancePool.</returns>
		public static Scene GetInstancePoolScene()
		{
			if (s_instancePoolScene.IsValid() && s_instancePoolScene.isLoaded)
				return s_instancePoolScene;

			s_instancePoolScene = GetNewScene();
			return s_instancePoolScene;

			Scene GetNewScene()
			{
				s_instancePoolScene = SceneManager.GetSceneByName(InstancePoolSceneName);
				return s_instancePoolScene.IsValid() ? s_instancePoolScene : SceneManager.CreateScene(InstancePoolSceneName, new CreateSceneParameters(LocalPhysicsMode.None));
			}
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStaticInstances() => RemovePools();

		/// <summary>
		/// Removes references to all static pools.
		/// This does not remove the instance pool scene, and any object that is currently pooled can and will leak into that scene unless handled manually.
		/// </summary>
		public static void RemovePools(Action<Component> handlePooledInstance = null)
		{
			foreach (IComponentPool pool in s_instancePools)
				pool.RemovePools(handlePooledInstance);
			// Never clear the instance pools, these are initialised from a static constructor,
			// If they are removed from the pool then we will lose tracking of them entirely until domain reload runs.
			// s_instancePools.Clear();
		}

		internal static readonly HashSet<IComponentPool> s_instancePools = new HashSet<IComponentPool>();

		/// <summary>
		/// Destroys extra instances beyond the capacities set (or defaulted to.)
		/// </summary>
		/// <param name="defaultCapacity">The default maximum amount of instances kept when <see cref="TrimExcess"/> is called</param>
		public static void TrimExcess(int defaultCapacity = 20)
		{
			foreach (IComponentPool pool in s_instancePools)
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
		void TrimExcess(int? capacityOverride);
		void RemovePools(Action<Component> handlePooledInstance = null);
	}

	public static partial class InstancePool<TInstanceType> where TInstanceType : Component
	{
		private static readonly ComponentPoolGroup<TInstanceType> s_componentPool = new ComponentPoolGroup<TInstanceType>();

		static InstancePool() => InstancePool.s_instancePools.Add(s_componentPool);
	}

	/// <summary>
	/// A pool for Component instances.
	/// </summary>
	/// <typeparam name="TInstanceType">The Component Type associated with the pool</typeparam>
	internal class ComponentPoolGroup<TInstanceType> : IComponentPool where TInstanceType : Component
	{
#if UNITY_ASSERTIONS
		private readonly string _getAssertPrefabMessage;
		private readonly string _poolAssertPrefabMessage;
		private readonly string _poolAssertInstanceMessage;
#endif
		
		public ComponentPoolGroup()
		{
#if UNITY_ASSERTIONS
			_getAssertPrefabMessage = $"Prefab passed to InstancePool<{typeof(TInstanceType).Name}>{nameof(Get)} was null";
			_poolAssertPrefabMessage = $"Prefab passed to InstancePool<{typeof(TInstanceType).Name}>{nameof(Pool)} was null";
			_poolAssertInstanceMessage = $"Instance passed to InstancePool<{typeof(TInstanceType).Name}>{nameof(Pool)} was null";
#endif
		}
		
		/// <summary>
		/// Dictionary of prefab components to HashSets of pooled instances.
		/// </summary>
		private readonly Dictionary<TInstanceType, ComponentPoolBase<TInstanceType>> _poolGroup = new Dictionary<TInstanceType, ComponentPoolBase<TInstanceType>>();

		/// <summary>
		/// Returns the amount of pooled instances associated with a prefab key.
		/// </summary>
		/// <param name="key">The prefab key.</param>
		/// <returns>The amount of pooled instances associated with the key.</returns>
		public int GetCurrentlyPooledCount(TInstanceType key) => !_poolGroup.TryGetValue(key, out ComponentPoolBase<TInstanceType> set) ? 0 : set.GetCurrentlyPooledCount();


		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="prefab"/> pooled.
		/// </summary>
		/// <param name="prefab">The prefab key to instance.</param>
		/// <param name="count">The amount to ensure is pooled.</param>
		/// <param name="parent">Optional parent.</param>
		public void Warmup(TInstanceType prefab, int count, Transform parent = null)
		{
			if (!_poolGroup.TryGetValue(prefab, out var pool))
				_poolGroup.Add(prefab, pool = new ExpandablePool<TInstanceType>(prefab));
			pool.Warmup(count, parent);
		}

		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="prefab"/> pooled.
		/// </summary>
		/// <param name="prefab">The prefab key to instance.</param>
		/// <param name="count">The amount to ensure is pooled.</param>
		/// <param name="parent">Optional parent.</param>
		/// <param name="instancesPerFrame">The amount of instances created per frame.</param>
		public IEnumerator WarmupCoroutine(TInstanceType prefab, int count, Transform parent = null, int instancesPerFrame = 1)
		{
			if (!_poolGroup.TryGetValue(prefab, out ComponentPoolBase<TInstanceType> pool))
				_poolGroup.Add(prefab, pool = new ExpandablePool<TInstanceType>(prefab));
			return pool.WarmupCoroutine(count, parent, instancesPerFrame);
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
			return Get(prefab, parent, prefabTransform.localPosition, prefabTransform.localRotation);
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
			=> Get(prefab, parent, position, rotation, prefab.transform.localScale, space);

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
#if UNITY_ASSERTIONS
			Assert.IsNotNull(prefab, _getAssertPrefabMessage);
#endif

			// Use the pool if we have one already
			if (_poolGroup.TryGetValue(prefab, out ComponentPoolBase<TInstanceType> pool))
				return pool.Get(parent, position, rotation, localScale, space);

			return CreateInstance(prefab, parent, position, rotation, localScale, space);
		}

		internal static TInstanceType CreateInstance(TInstanceType prefab, Transform parent, Vector3 position, Quaternion rotation, Vector3 localScale, Space space = Space.World)
		{
			TInstanceType instance;
			// Otherwise return a new instance.
			// Only when an instance is returned do we need to create a pool.
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
#if UNITY_ASSERTIONS
			Assert.IsNotNull(prefab, _poolAssertPrefabMessage);
			Assert.IsNotNull(instance, _poolAssertInstanceMessage);
#endif

			// Create a pool if we don't have one already.
			if (!_poolGroup.TryGetValue(prefab, out ComponentPoolBase<TInstanceType> pool))
			{
				pool = new ExpandablePool<TInstanceType>(prefab);
				_poolGroup.Add(prefab, pool);
			}

			pool.Pool(instance);
		}

		/// <summary>
		/// Queries whether the pool contains a specific instance of a prefab.
		/// </summary>
		/// <param name="prefab">The prefab key referring to the pool.</param>
		/// <param name="instance">The instance we are querying.</param>
		/// <returns>True if the pool contains the queried instance.</returns>
		public bool IsPooled(TInstanceType prefab, TInstanceType instance) => _poolGroup.TryGetValue(prefab, out ComponentPoolBase<TInstanceType> pool) && pool.IsPooled(instance);

		/// <summary>
		/// When working with temporary pools this will remove all of them associated with a component type from the system.<br/>
		/// This will not remove the instances that are currently pooled. Un-pool all instances before calling this function.
		/// </summary>
		public void RemovePools() => ((IComponentPool)this).RemovePools();

		/// <summary>
		/// When working with temporary pools this will remove all of them associated with a component type from the system.<br/>
		/// This will not remove the instances that are currently pooled. Un-pool all instances before calling this function.
		/// </summary>
		/// /// <param name="handlePooledInstance">A callback for dealing with instances that are in the pool.</param>
		public void RemovePools(Action<TInstanceType> handlePooledInstance)
		{
			foreach (KeyValuePair<TInstanceType, ComponentPoolBase<TInstanceType>> pair in _poolGroup)
			{
				foreach (TInstanceType instance in pair.Value)
					handlePooledInstance(instance);
			}

			((IComponentPool)this).RemovePools();
		}

		/// <summary>
		/// When working with temporary pools this will remove them from the system.<br/>
		/// This will not remove the instances that are currently pooled. Un-pool all instances before calling this function.
		/// </summary>
		/// <param name="prefab">The prefab key referring to the pool.</param>
		public void RemovePool(TInstanceType prefab) => _poolGroup.Remove(prefab);

		/// <summary>
		/// When working with temporary pools this will remove them from the system..<br/>
		/// Handle the currently pooled instances with the <see cref="handlePooledInstance"/> parameter.
		/// </summary>
		/// <param name="prefab">The prefab key referring to the pool.</param>
		/// <param name="handlePooledInstance">A callback for dealing with instances that are in the pool.</param>
		public void RemovePool(TInstanceType prefab, Action<TInstanceType> handlePooledInstance)
		{
			if (!_poolGroup.TryGetValue(prefab, out ComponentPoolBase<TInstanceType> pool))
				return;
			RemovePool(prefab);
			foreach (TInstanceType instance in pool)
				handlePooledInstance(instance);
		}

		/// <summary>
		/// When working with temporary pools this will remove them from the system..<br/>
		/// Handle the currently pooled instances with the <see cref="handlePooledInstance"/> parameter.
		/// </summary>
		/// <param name="handlePooledInstance">A callback for dealing with instances that are in the pool.</param>
		void IComponentPool.RemovePools(Action<Component> handlePooledInstance)
		{
			if (handlePooledInstance != null)
			{
				foreach (KeyValuePair<TInstanceType, ComponentPoolBase<TInstanceType>> pair in _poolGroup)
				{
					foreach (TInstanceType instance in pair.Value)
						handlePooledInstance(instance);
				}
			}

			// Reset collections to their initial state.
			_poolGroup.Clear();
			_poolGroup.TrimExcess();
		}

		#region Capacity

		/// <summary>
		/// Sets the capacity used by <see cref="TrimExcess"/> for all instances shared between the type <see cref="TInstanceType"/>
		/// </summary>
		/// <param name="capacity">The maximum amount of instances kept when <see cref="TrimExcess"/> is called.</param>
		public void SetCapacities(int capacity)
		{
			foreach (var pair in _poolGroup)
				_poolGroup[pair.Key].Capacity = capacity;
		}

		/// <summary>
		/// Sets the capacity used by <see cref="TrimExcess"/>
		/// </summary>
		/// <param name="prefab">The prefab used as a key within the pool.</param>
		/// <param name="capacity">The maximum amount of instances kept when <see cref="TrimExcess"/> is called.</param>
		public void SetCapacity(TInstanceType prefab, int capacity)
		{
			if (!_poolGroup.TryGetValue(prefab, out ComponentPoolBase<TInstanceType> pool))
			{
				pool = new ExpandablePool<TInstanceType>(prefab);
				_poolGroup.Add(prefab, pool);
			}
			pool.Capacity = capacity;
		}

		/// <summary>
		/// Destroys extra instances beyond the capacities set.<br/>
		/// The default capacity is 20 if <see cref="SetCapacity"/> or <see cref="SetCapacities"/> was not set.
		/// <param name="capacityOverride">Optional capacity override.</param>
		/// </summary>
		public void TrimExcess(int? capacityOverride = null)
		{
#if UNITY_2021_1_OR_NEWER
			using (UnityEngine.Pool.HashSetPool<TInstanceType>.Get(out var temp))
#else
			HashSet<TInstanceType> temp = new HashSet<TInstanceType>();
#endif
			foreach (var pair in _poolGroup)
			{
				var pool = pair.Value;
				if (capacityOverride.HasValue)
					pool.Capacity = capacityOverride.Value;
				pool.TrimExcess(temp);
				temp.Clear();
			}
		}

		/// <summary>
		/// Destroys extra instances beyond the capacities set.<br/>
		/// The default capacity is 20 if <see cref="SetCapacity"/> or <see cref="SetCapacities"/> was not set.
		/// </summary>
		/// <param name="prefab">The prefab used as a key within the pool.</param>
		/// <param name="capacity">Optional capacity override.</param>
		public void TrimExcess(TInstanceType prefab, int? capacity = null)
		{
			if (!_poolGroup.TryGetValue(prefab, out ComponentPoolBase<TInstanceType> pool))
				return;
			if (capacity.HasValue)
				pool.Capacity = capacity.Value;
			pool.TrimExcess();
		}

		#endregion
	}
}