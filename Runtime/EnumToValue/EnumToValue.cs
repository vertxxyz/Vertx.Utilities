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
	public abstract class EnumToValueBase
	{
		[SerializeField] protected bool hidesFirstEnum;
	}

	public class HideFirstEnumValue : Attribute { }

	
	/// <summary>
	/// A helper class to associate enum values to data, and display that appropriately in the inspector
	/// If your enum values are not consecutive, please use <see cref="EnumToValueDictionary{T,TValue}"/>.
	/// </summary>
	/// <typeparam name="T">The enum key</typeparam>
	/// <typeparam name="TValue">The data to associate with enum values</typeparam>
	[Serializable]
	public class EnumToValue<T, TValue> : EnumToValueBase, IEnumerable<(T key, TValue value)>
		where T : Enum
	{
		// ReSharper disable once Unity.RedundantSerializeFieldAttribute
		[SerializeField] private TValue[] values;

		protected TValue[] Values
		{
			get
			{
				if (values == null || values.Length == 0)
				{
					values = new TValue[Enum.GetValues(typeof(T)).Length];
					for (int i = 0; i < values.Length; i++)
						values[i] = default;
				}
				return values;
			}
		}

		public TValue GetValue(T key) => this[key];

		public TValue this[T key]
		{
			get
			{
				int index = (int) (object) key;
				if (index >= Values.Length)
				{
					Debug.LogError($"Provided key \"{key}\" is out of range in {this}.\nValues will need to be serialized by using the inspector on this object.\nA default value has been returned.");
					return default;
				}
				return Values[index];
			}
		}

		public int Count => Values.Length;

		private Array valuesArray = null;

		public int IndexOf(TValue value)
		{
			TValue[] values = Values;
			for (int i = hidesFirstEnum ? 1 : 0; i < values.Length; i++)
			{
				if (value.Equals(values[i]))
					return i;
			}

			return -1;
		}

		public IEnumerator<(T key, TValue value)> GetEnumerator()
		{
			TValue[] values = Values;
			Array array = valuesArray ?? (valuesArray = Enum.GetValues(typeof(T)));
			for (int i = hidesFirstEnum ? 1 : 0; i < values.Length; i++)
				yield return ((T) array.GetValue(i), values[i]);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}

	/// <summary>
	/// A helper class to associate enum values to data, and display that appropriately in the inspector
	/// </summary>
	/// <typeparam name="T">The enum key</typeparam>
	/// <typeparam name="TValue">The data to associate with enum values</typeparam>
	[Serializable]
	public class EnumToValueDictionary<T, TValue> :
		EnumToValueBase, IEnumerable<(T key, TValue value)>,
		ISerializationCallbackReceiver
		where T : Enum
	{
		// ReSharper disable once Unity.RedundantSerializeFieldAttribute
		[SerializeField] protected T[] keys = null;
		// ReSharper disable once Unity.RedundantSerializeFieldAttribute
		[SerializeField] protected TValue[] values = null;

		private Dictionary<T, TValue> dictionary;

		public TValue GetValue(T key) => dictionary[key];
		public TValue this[T key]
		{
			get
			{
				if (dictionary.TryGetValue(key, out var value))
					return value;
				Debug.LogError($"Provided key \"{key}\" is out of range in {this}.\nValues will need to be serialized by using the inspector on this object.\nA default value has been returned.");
				return default;
			}
		}

		public bool TryGetValue(T key, out TValue value) => dictionary.TryGetValue(key, out value);

		private int length;

		public IEnumerator<(T key, TValue value)> GetEnumerator()
		{
			foreach (KeyValuePair<T, TValue> keyValuePair in dictionary)
				yield return (keyValuePair.Key, keyValuePair.Value);
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


		public void OnBeforeSerialize()
		{
			//Don't do anything because we directly modify and re-serialize this via EnumToValueDrawer.
		}

		public void OnAfterDeserialize()
		{
			int count = keys == null || values == null ? 0 : Mathf.Min(keys.Length, values.Length);
			dictionary = new Dictionary<T, TValue>();
			for (int i = hidesFirstEnum ? 1 : 0; i < count; i++)
			{
				// ReSharper disable twice PossibleNullReferenceException
				T key = keys[i];
				if (dictionary.ContainsKey(key)) continue;
				dictionary.Add(key, values[i]);
			}
		}
	}
}