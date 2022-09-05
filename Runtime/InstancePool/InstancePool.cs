using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ASSERTIONS
using UnityEngine.Assertions;
#endif
using UnityEngine.SceneManagement;

// ReSharper disable MemberCanBePrivate.Global

namespace Vertx.Utilities
{
	public static partial class InstancePool
	{
		public const string InstancePoolSceneName = "Instance Pool";
		public const int DefaultPoolCapacity = 20;

		/// <summary>
		/// The default pool is <see cref="ExpandablePool{TInstanceType}"/>. If safety checks are disabled it is <see cref="ExpandablePoolUnchecked{TInstanceType}"/>.
		/// </summary>
		public static bool DefaultPoolHasSafetyChecks { get; set; } = true;

		private static Scene s_instancePoolScene;
		internal static readonly HashSet<IComponentPoolGroup> s_instancePools = new HashSet<IComponentPoolGroup>();

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
			foreach (IComponentPoolGroup pool in s_instancePools)
				pool.RemovePools(handlePooledInstance);
			// Never clear the instance pools, these are initialised from a static constructor,
			// If they are removed from the pool then we will lose tracking of them entirely until domain reload runs.
			// s_instancePools.Clear();
		}

		/// <summary>
		/// Destroys extra instances beyond the capacities set (or defaulted to.)
		/// </summary>
		/// <param name="forcedCapacity">The default maximum amount of instances kept when <see cref="TrimExcess"/> is called is defined per-pool.<br/>
		/// If provided, this will override those values.</param>
		public static void TrimExcess(int? forcedCapacity = null)
		{
			foreach (IComponentPoolGroup pool in s_instancePools)
				pool.TrimExcess(forcedCapacity);
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

	internal interface IComponentPoolGroup
	{
		void TrimExcess(int? capacityOverride);
		void RemovePools(Action<Component> handlePooledInstance = null);

		IDictionary PoolGroup { get; }
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
	internal class ComponentPoolGroup<TInstanceType> : IComponentPoolGroup where TInstanceType : Component
	{
#if UNITY_ASSERTIONS
		private readonly string _getAssertPrefabMessage;
		private readonly string _poolAssertPrefabMessage;
		private readonly string _poolAssertInstanceMessage;
#endif

		public IDictionary PoolGroup => _poolGroup;

		/// <summary>
		/// Dictionary of prefab components to pools.
		/// </summary>
		private readonly Dictionary<TInstanceType, IComponentPool<TInstanceType>> _poolGroup = new Dictionary<TInstanceType, IComponentPool<TInstanceType>>();

		public ComponentPoolGroup()
		{
#if UNITY_ASSERTIONS
			_getAssertPrefabMessage = $"Prefab passed to InstancePool<{typeof(TInstanceType).Name}>{nameof(Get)} was null.";
			_poolAssertPrefabMessage = $"Prefab passed to InstancePool<{typeof(TInstanceType).Name}>{nameof(Pool)} was null.";
			_poolAssertInstanceMessage = $"Instance passed to InstancePool<{typeof(TInstanceType).Name}>{nameof(Pool)} was null.";
#endif
		}

		private IComponentPool<TInstanceType> GetOrCreatePool(TInstanceType prefab)
		{
			if (_poolGroup.TryGetValue(prefab, out IComponentPool<TInstanceType> pool))
				return pool;

			// Create a pool if we don't have one already.
			_poolGroup.Add(
				prefab,
				pool = InstancePool.DefaultPoolHasSafetyChecks
					? new ExpandablePool<TInstanceType>(prefab)
					: (IComponentPool<TInstanceType>) // 2019.4 needs this
					new ExpandablePoolUnchecked<TInstanceType>(prefab)
			);
			return pool;
		}

		/// <summary>
		/// Checks whether InstancePool contains a matching pool.
		/// </summary>
		/// <param name="prefab">The prefab key.</param>
		/// <returns>True if InstancePool contains a pool matching the prefab key.</returns>
		public bool HasPool(TInstanceType prefab) => _poolGroup.ContainsKey(prefab);

		/// <summary>
		/// Returns the amount of pooled instances associated with a prefab key.
		/// </summary>
		/// <param name="key">The prefab key.</param>
		/// <returns>The amount of pooled instances associated with the key.</returns>
		public int GetCurrentlyPooledCount(TInstanceType key) => !_poolGroup.TryGetValue(key, out IComponentPool<TInstanceType> pool) ? 0 : pool.Count;

		/// <summary>
		/// Overrides the default <see cref="ExpandablePool{TInstanceType}"/> used by InstancePool with another instance.<br/>
		/// If there is a pre-existing pool, instances are moved to the new pool.
		/// </summary>
		/// <param name="pool">The new pool to switch to.</param>
		public void Override(IComponentPool<TInstanceType> pool)
		{
			if (pool.Prefab == null)
			{
				Debug.LogError($"Provided pool was not initialised with a prefab. {nameof(InstancePool)}.{nameof(Override)} has been ignored.");
				return;
			}

			if (_poolGroup.TryGetValue(pool.Prefab, out IComponentPool<TInstanceType> currentPool))
			{
				// Move instances from the old pool to the new one.
				foreach (TInstanceType instance in currentPool)
				{
					if (instance == null) continue;
					pool.Pool(instance);
				}
			}

			_poolGroup[pool.Prefab] = pool;
		}

		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="prefab"/> pooled.
		/// </summary>
		/// <param name="prefab">The prefab key to instance.</param>
		/// <param name="count">The amount to ensure is pooled.</param>
		/// <param name="parent">Optional parent.</param>
		public void Warmup(TInstanceType prefab, int count, Transform parent = null)
			=> GetOrCreatePool(prefab).Warmup(count, parent);

		/// <summary>
		/// Ensures the pool has <see cref="count"/> number of instances of <see cref="prefab"/> pooled.
		/// </summary>
		/// <param name="prefab">The prefab key to instance.</param>
		/// <param name="count">The amount to ensure is pooled.</param>
		/// <param name="parent">Optional parent.</param>
		/// <param name="instancesPerFrame">The amount of instances created per frame.</param>
		public IEnumerator WarmupCoroutine(TInstanceType prefab, int count, Transform parent = null, int instancesPerFrame = 1)
			=> GetOrCreatePool(prefab).WarmupCoroutine(count, parent, instancesPerFrame);

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
		/// <param name="position">Position of the instance.</param>
		/// <param name="rotation">Rotation of the instance.</param>
		/// <param name="localScale">Local Scale of the instance.</param>
		/// <param name="space">Which space the position and rotation is applied in.</param>
		/// <returns>An instance retrieved from the pool.</returns>
		public TInstanceType Get(TInstanceType prefab, Transform parent, Vector3 position, Quaternion rotation, Vector3 localScale, Space space = Space.World)
		{
#if UNITY_ASSERTIONS
			Assert.IsNotNull(prefab, _getAssertPrefabMessage);
#endif
			return GetOrCreatePool(prefab).Get(parent, position, rotation, localScale, space);
		}

		/// <summary>
		/// Returns a Component instance to the pool.
		/// </summary>
		/// <param name="prefab">The prefab key used when the instance was retrieved via <see cref="Get(TInstanceType,UnityEngine.Transform)"/>.</param>
		/// <param name="instance">The instance to return to the pool.</param>
		public void Pool(TInstanceType prefab, TInstanceType instance)
		{
#if UNITY_ASSERTIONS
			Assert.IsNotNull(prefab, _poolAssertPrefabMessage);
			Assert.IsNotNull(instance, _poolAssertInstanceMessage);
#endif
			GetOrCreatePool(prefab).Pool(instance);
		}

		/// <summary>
		/// Queries whether the pool contains a specific instance of a prefab.
		/// </summary>
		/// <param name="prefab">The prefab key referring to the pool.</param>
		/// <param name="instance">The instance we are querying.</param>
		/// <returns>True if the pool contains the queried instance.</returns>
		public bool IsPooled(TInstanceType prefab, TInstanceType instance)
			=> _poolGroup.TryGetValue(prefab, out IComponentPool<TInstanceType> pool) && pool.Contains(instance);

		/// <summary>
		/// When working with temporary pools this will remove all of them associated with a component type from the system.<br/>
		/// This will not remove the instances that are currently pooled. Un-pool all instances before calling this function.
		/// </summary>
		public void RemovePools() => ((IComponentPoolGroup)this).RemovePools();

		/// <summary>
		/// When working with temporary pools this will remove all of them associated with a component type from the system.<br/>
		/// This will not remove the instances that are currently pooled. Un-pool all instances before calling this function.
		/// </summary>
		/// /// <param name="handlePooledInstance">A callback for dealing with instances that are in the pool.</param>
		public void RemovePools(Action<TInstanceType> handlePooledInstance)
		{
			foreach (KeyValuePair<TInstanceType, IComponentPool<TInstanceType>> pair in _poolGroup)
			{
				foreach (TInstanceType instance in pair.Value)
					handlePooledInstance(instance);
			}

			((IComponentPoolGroup)this).RemovePools();
		}

		/// <summary>
		/// When working with temporary pools this will remove them from the system.<br/>
		/// This will not remove the instances that are currently pooled. Un-pool all instances before calling this function.
		/// </summary>
		/// <param name="prefab">The prefab key referring to the pool.</param>
		/// <returns>The pool that was removed. Null otherwise.</returns>
		public IComponentPool<TInstanceType> RemovePool(TInstanceType prefab)
		{
			if (!_poolGroup.TryGetValue(prefab, out IComponentPool<TInstanceType> pool))
				return null;
			_poolGroup.Remove(prefab);
			return pool;
		}

		/// <summary>
		/// When working with temporary pools this will remove them from the system..<br/>
		/// Handle the currently pooled instances with the <see cref="handlePooledInstance"/> parameter.
		/// </summary>
		/// <param name="prefab">The prefab key referring to the pool.</param>
		/// <param name="handlePooledInstance">A callback for dealing with instances that are in the pool.</param>
		/// /// <returns>The pool that was removed. Null otherwise.</returns>
		public IComponentPool<TInstanceType> RemovePool(TInstanceType prefab, Action<TInstanceType> handlePooledInstance)
		{
			var pool = RemovePool(prefab);
			if (pool == null)
				return null;
			foreach (TInstanceType instance in pool)
				handlePooledInstance(instance);
			return pool;
		}

		/// <summary>
		/// When working with temporary pools this will remove them from the system..<br/>
		/// Handle the currently pooled instances with the <see cref="handlePooledInstance"/> parameter.
		/// </summary>
		/// <param name="handlePooledInstance">A callback for dealing with instances that are in the pool.</param>
		void IComponentPoolGroup.RemovePools(Action<Component> handlePooledInstance)
		{
			if (handlePooledInstance != null)
			{
				foreach (KeyValuePair<TInstanceType, IComponentPool<TInstanceType>> pair in _poolGroup)
				{
					foreach (TInstanceType instance in pair.Value)
						handlePooledInstance(instance);
				}
			}

			// Reset collections to their initial state.
			_poolGroup.Clear();
#if UNITY_2022_1_OR_NEWER
			// Set the internal dictionary to have no entries. If you're calling this method, lets truly clear all the data.
			_poolGroup.TrimExcess();
#endif
		}

		#region Capacity

		/// <summary>
		/// Sets the capacity used by <see cref="TrimExcess(System.Nullable{int})"/> for all instances shared between the type <see cref="TInstanceType"/>
		/// </summary>
		/// <param name="capacity">The maximum amount of instances kept when <see cref="TrimExcess(System.Nullable{int})"/> is called.</param>
		public void SetCapacities(int capacity)
		{
			foreach (var pair in _poolGroup)
				_poolGroup[pair.Key].Capacity = capacity;
		}

		/// <summary>
		/// Sets the capacity used by <see cref="TrimExcess(System.Nullable{int})"/>
		/// </summary>
		/// <param name="prefab">The prefab used as a key within the pool.</param>
		/// <param name="capacity">The maximum amount of instances kept when <see cref="TrimExcess(System.Nullable{int})"/> is called.</param>
		public void SetCapacity(TInstanceType prefab, int capacity) => GetOrCreatePool(prefab).Capacity = capacity;

		/// <summary>
		/// Destroys extra instances beyond the capacities set.<br/>
		/// The default capacity is 20 if <see cref="SetCapacity"/> or <see cref="SetCapacities"/> was not set.
		/// <param name="capacityOverride">Optional capacity override.</param>
		/// </summary>
		public void TrimExcess(int? capacityOverride = null)
		{
			foreach (var pair in _poolGroup)
			{
				var pool = pair.Value;
				if (capacityOverride.HasValue)
					pool.Capacity = capacityOverride.Value;
				pool.TrimExcess();
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
			if (!_poolGroup.TryGetValue(prefab, out IComponentPool<TInstanceType> pool))
				return;
			if (capacity.HasValue)
				pool.Capacity = capacity.Value;
			pool.TrimExcess();
		}

		#endregion
	}
}