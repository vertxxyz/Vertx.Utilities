#if UNITY_2020_1_OR_NEWER
// #define VERTX_ETV_EXCLUDE_OLD_SERIALIZATION

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace Vertx.Utilities
{
	/// <summary>
	/// This class exists to give the PropertyDrawer a class to bind to.
	/// </summary>
	[Serializable]
	public abstract class EnumToValueBase { }

	public class HideFirstEnumValue : Attribute { }

	/// <summary>
	/// A helper class to associate enum values to data, and display that appropriately in the inspector
	/// </summary>
	/// <typeparam name="T">The enum key</typeparam>
	/// <typeparam name="TValue">The data to associate with enum values</typeparam>
	[Serializable]
	public class EnumToValue<T, TValue> : EnumToValueBase,
		IReadOnlyDictionary<T, TValue>,
		ISerializationCallbackReceiver
		where T : Enum
	{
		[Serializable]
		private struct Pair
		{
			[SerializeField] private T key;
			[SerializeField] private TValue value;

			public Pair(T key, TValue value)
			{
				this.key = key;
				this.value = value;
			}

			public void Deconstruct(out T key, out TValue value)
			{
				key = this.key;
				value = this.value;
			}
		}

		private Dictionary<T, TValue> dictionary;
		[SerializeField] private Pair[] pairs;

#if !VERTX_ETV_EXCLUDE_OLD_SERIALIZATION
		// Old serialized representation.
		// This will be removed in the next major version.
		[SerializeField] protected T[] keys;
		[SerializeField] private TValue[] values;
#endif

		public TValue GetValue(T key) => this[key];
		public bool ContainsKey(T key) => dictionary.ContainsKey(key);

		public bool TryGetValue(T key, out TValue value) => dictionary.TryGetValue(key, out value);

		public TValue this[T key]
		{
			get
			{
				if (dictionary.TryGetValue(key, out var value))
					return value;
				Debug.LogError(
					$"Provided key \"{key}\" is out of range in {this}.\nValues will need to be serialized by using the inspector on this object.\nA default value has been returned."
				);
				return default;
			}
		}

		public IEnumerable<T> Keys => dictionary.Keys;
		public IEnumerable<TValue> Values => dictionary.Values;

		public int Count => dictionary.Count;

		public IEnumerator<KeyValuePair<T, TValue>> GetEnumerator() => dictionary.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => dictionary.GetEnumerator();

		public void OnBeforeSerialize()
		{
			//Don't do anything because we directly modify and re-serialize this via EnumToValueDrawer.
		}

		public void OnAfterDeserialize()
		{
			int pairsCount = pairs?.Length ?? 0;

#if !VERTX_ETV_EXCLUDE_OLD_SERIALIZATION
			if (pairsCount == 0)
			{
				// Handle old serialization versions.
				if (keys?.Length > 0)
				{
					// EnumToValueDictionary
					int count = values == null ? 0 : Mathf.Min(keys.Length, values.Length);
					if (count > 0)
					{
						dictionary = new Dictionary<T, TValue>();
						pairs = new Pair[count];
						for (int i = 0; i < count; i++)
						{
							// ReSharper disable twice PossibleNullReferenceException
							T key = keys[i];
							TValue value = values[i];
							pairs[i] = new Pair(key, value);
							if (dictionary.ContainsKey(key)) continue;
							dictionary.Add(key, value);
						}

						keys = null;
						values = null;
						Debug.Log(
							$"{this} was upgraded. If you face serialisation issues please revert these changes and roll back Utilities to version 2. {dictionary.Count} values were ported.\n" +
							"If you are repeatedly seeing this message, find the objects that use EnumToValue and dirty them manually before saving the project."
						);
#if UNITY_EDITOR
						EditorApplication.delayCall += () =>
						{
							EditorSceneManager.MarkAllScenesDirty();
							// Dirty all EnumDataDescriptions.
							string[] guids = AssetDatabase.FindAssets("t:EnumDataDescription`1");
							foreach (string guid in guids)
							{
								string path = AssetDatabase.GUIDToAssetPath(guid);
								var o = AssetDatabase.LoadAssetAtPath<Object>(path);
								EditorUtility.SetDirty(o);
							}
						};
#endif
						return;
					}
				}
				else if (values?.Length > 0)
				{
					// EnumToValue
					int count = values.Length;
					dictionary = new Dictionary<T, TValue>();
					pairs = new Pair[count];
					for (int i = 0; i < count; i++)
					{
						// ReSharper disable once PossibleInvalidCastException
						T key = (T)(object)i;
						TValue value = values[i];
						pairs[i] = new Pair(key, value);
						if (dictionary.ContainsKey(key)) continue;
						dictionary.Add(key, value);
					}

					keys = null;
					values = null;
					Debug.Log(
						$"{this} was upgraded. If you face serialisation issues please revert these changes and roll back Utilities to version 2.4.5. {dictionary.Count} values were ported."
					);
#if UNITY_EDITOR
					EditorApplication.delayCall += EditorSceneManager.MarkAllScenesDirty;
#endif
					return;
				}
			}
#endif

			{
				dictionary = new Dictionary<T, TValue>();
				for (int i = 0; i < pairsCount; i++)
				{
					// ReSharper disable PossibleNullReferenceException
					(T key, TValue value) = pairs[i];
					if (dictionary.ContainsKey(key)) continue;
					dictionary.Add(key, value);
				}
			}

#if !VERTX_ETV_EXCLUDE_OLD_SERIALIZATION
			keys = null;
			values = null;
#endif
		}
	}
}
#endif