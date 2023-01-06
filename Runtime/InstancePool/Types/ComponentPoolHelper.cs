using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Vertx.Utilities
{
	internal static class ComponentPoolHelper
	{
		public static void Warmup<TInstanceType>(
			IComponentPool<TInstanceType> pool,
			int count,
			Transform parent = null
		) where TInstanceType : Component
		{
			count = Math.Min(count, pool.Capacity);
			TInstanceType prefab = pool.Prefab;
			for (int i = pool.Count; i < count; i++)
			{
				var instance = Object.Instantiate(prefab, parent);
				instance.name = prefab.name;
				instance.gameObject.SetActive(false);
				InstancePool.MoveToInstancePoolScene(instance);
				pool.Pool(instance);
			}
		}

		public static IEnumerator WarmupCoroutine<TInstanceType>(
			IComponentPool<TInstanceType> pool,
			int count,
			Transform parent = null,
			int instancesPerFrame = 1
		) where TInstanceType : Component
		{
			count = Math.Min(count, pool.Capacity);
			TInstanceType prefab = pool.Prefab;
			while (pool.Count < count)
			{
				int amount = Mathf.Clamp(instancesPerFrame, 0, count - pool.Count);
				for (int i = 0; i < amount; i++)
				{
					var instance = Object.Instantiate(prefab, parent);
					instance.name = prefab.name;
					instance.gameObject.SetActive(false);
					InstancePool.MoveToInstancePoolScene(instance);
					pool.Pool(instance);
				}

				yield return null;
			}
		}

		public static TInstanceType CreateInstance<TInstanceType>(
			TInstanceType prefab,
			Transform parent,
			Vector3 position,
			Quaternion rotation,
			Vector3 localScale,
			Space space = Space.World
		) where TInstanceType : Component
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

		public static void PositionInstance<TInstanceType>(
			TInstanceType instance,
			Transform parent,
			Vector3 position,
			Quaternion rotation,
			Vector3 localScale,
			Space space = Space.World
		) where TInstanceType : Component
		{
			// Activate and re-parent
			GameObject poppedInstanceGameObject = instance.gameObject;
			poppedInstanceGameObject.SetActive(true);
			Transform t = instance.transform;
			if (t.parent != parent)
				t.SetParent(parent);
			else
				SceneManager.MoveGameObjectToScene(poppedInstanceGameObject, SceneManager.GetActiveScene());

			// Position
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
		}


		public static void DisableAndMoveToInstancePoolScene<TInstanceType>(TInstanceType instance) where TInstanceType : Component
		{
			// Disable the object and move it to the instance pool scene.
			instance.gameObject.SetActive(false);
			InstancePool.MoveToInstancePoolScene(instance);
		}
	}
}