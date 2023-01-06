#if UNITY_2020_1_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vertx.Utilities
{
	/// <summary>
	/// This class exists to give the PropertyDrawer a class to bind to.
	/// </summary>
	[Serializable]
	public abstract class EnumToValueBase { }

	[AttributeUsage(AttributeTargets.Field)]
	public class HideFirstEnumValueAttribute : Attribute { }

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
			// Don't do anything because we directly modify and re-serialize this via EnumToValueDrawer.
		}

		public void OnAfterDeserialize()
		{
			int pairsCount = pairs?.Length ?? 0;

			dictionary = new Dictionary<T, TValue>();
			for (int i = 0; i < pairsCount; i++)
			{
				// ReSharper disable PossibleNullReferenceException
				(T key, TValue value) = pairs[i];
				if (dictionary.ContainsKey(key)) continue;
				dictionary.Add(key, value);
			}
		}
	}
}
#endif