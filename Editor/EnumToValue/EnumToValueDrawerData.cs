#if UNITY_2020_1_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vertx.Utilities.Editor
{
	[CustomPropertyDrawer(typeof(EnumToValueBase), true)]
	public partial class EnumToValueDrawer : PropertyDrawer
	{
		private const string
			pairsName = "pairs",
			keyName = "key",
			valueName = "value";

		private readonly Dictionary<string, Data> propertyData = new Dictionary<string, Data>();

		private class Data
		{
			public GUIContent[] EnumNames;
			public float[] PropertyHeights;
			public bool MultiLine;
			public bool HidesFirstEnum;
			public bool HasCustomPropertyDrawer;

			public void Deconstruct(
				out GUIContent[] enumNames,
				out float[] propertyHeights,
				out bool multiLine,
				out bool hidesFirstEnum,
				out bool hasCustomPropertyDrawer
			)
			{
				enumNames = EnumNames;
				propertyHeights = PropertyHeights;
				multiLine = MultiLine;
				hidesFirstEnum = HidesFirstEnum;
				hasCustomPropertyDrawer = HasCustomPropertyDrawer;
			}
		}

		private enum Mode
		{
			Imgui,
			UIToolkit
		}
		
		bool Initialise(SerializedProperty property, out Data data, Mode mode)
		{
			string path = property.propertyPath;
			if (propertyData.TryGetValue(path, out data))
			{
				return true;
			}

			data = new Data
			{
				HidesFirstEnum = fieldInfo.GetCustomAttribute<HideFirstEnumValueAttribute>() != null
			};

			Type baseType = fieldInfo.FieldType;

			Type enumToValueType = typeof(EnumToValueBase);
			Type objectType = typeof(object);
			while (baseType.BaseType != enumToValueType && baseType.BaseType != objectType)
				baseType = baseType.BaseType;

			Type[] genericArguments = baseType.GetGenericArguments();
			Type enumType = genericArguments[0];

			string[] names = Enum.GetNames(enumType);
			Array enumValues = Enum.GetValues(enumType);

			Dictionary<int, string> valuesToNames = new Dictionary<int, string>();
			SerializedProperty pairs = property.FindPropertyRelative(pairsName);

			// Remove any enum names that have duplicate indices.
			for (int i = 0; i < names.Length; i++)
			{
				int value = (int)enumValues.GetValue(i);
				string name = names[i];
				if (valuesToNames.ContainsKey(value))
					valuesToNames[value] = $"{valuesToNames[value]}/{name}";
				else
					valuesToNames.Add(value, name);
			}

			data.EnumNames = valuesToNames.Values.Select(s => new GUIContent(s)).ToArray();
			GUIContent[] enumNames = data.EnumNames;

			if (enumNames.Length == 0) return false;
			if (pairs.arraySize != valuesToNames.Count)
			{
				int pairIndex = 0;
				foreach (KeyValuePair<int, string> p in valuesToNames)
				{
					var key = p.Key;
					if (pairIndex >= pairs.arraySize)
					{
						// Add pair if no pair was found.
						SerializedProperty newPair = pairs.GetArrayElementAtIndex(pairs.arraySize++);
						newPair.FindPropertyRelative(keyName).intValue = key;
						pairIndex++;
						continue;
					}

					SerializedProperty pair = pairs.GetArrayElementAtIndex(pairIndex);
					int pairKey = pair.FindPropertyRelative(keyName).intValue;

					while (pairKey < key)
					{
						// Keys have been removed, remove pairs until that's no longer the case
						pairs.DeleteArrayElementAtIndex(pairIndex);
						if (pairIndex == pairs.arraySize)
						{
							SerializedProperty newPair = pairs.GetArrayElementAtIndex(pairs.arraySize++);
							newPair.FindPropertyRelative(keyName).intValue = key;
							pairKey = key;
							pairIndex++;
							break;
						}

						pair = pairs.GetArrayElementAtIndex(pairIndex);
						pairKey = pair.FindPropertyRelative(keyName).intValue;
					}

					if (pairKey == key)
					{
						// Pair is matched.
						pairIndex++;
					}
					else if (pairKey > key)
					{
						// If there is not going to be a matching pair, insert one.
						pairs.InsertArrayElementAtIndex(pairIndex);
						SerializedProperty newPair = pairs.GetArrayElementAtIndex(pairIndex);
						newPair.FindPropertyRelative(keyName).intValue = key;
						pairIndex++;
					}
				}

				// Remove remaining elements.
				pairs.arraySize = valuesToNames.Count;
				property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
			}

			property = pairs.GetArrayElementAtIndex(0).FindPropertyRelative(valueName);

			data.MultiLine = property.hasChildren && property.propertyType == SerializedPropertyType.Generic;
			data.HasCustomPropertyDrawer = EditorUtils.HasCustomPropertyDrawer(property);

			int j = 0;
			foreach (KeyValuePair<int, string> valuesToName in valuesToNames)
				data.EnumNames[j++].tooltip = valuesToName.Key.ToString();

			if (mode == Mode.Imgui)
				data.PropertyHeights = new float[valuesToNames.Count];
			propertyData.Add(path, data);
			return true;
		}
	}
}
#endif