using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Vertx.Utilities.Editor
{
	[CustomPropertyDrawer(typeof(EnumToValueBase), true)]
	public class EnumToValueDrawer : PropertyDrawer
	{
		private const string
			pairsName = "pairs",
			keyName = "key",
			valueName = "value";

		private readonly Dictionary<string, Data> propertyData = new Dictionary<string, Data>();

		private class Data
		{
			public string[] EnumNames;
			public float[] PropertyHeights;
			public GUIContent[] Tooltips;
			public bool MultiLine;
			public bool HidesFirstEnum;
			public bool HasCustomPropertyDrawer;

			public void Deconstruct(
				out string[] enumNames,
				out float[] propertyHeights,
				out GUIContent[] tooltips,
				out bool multiLine,
				out bool hidesFirstEnum,
				out bool hasCustomPropertyDrawer
			)
			{
				enumNames = EnumNames;
				propertyHeights = PropertyHeights;
				tooltips = Tooltips;
				multiLine = MultiLine;
				hidesFirstEnum = HidesFirstEnum;
				hasCustomPropertyDrawer = HasCustomPropertyDrawer;
			}
		}

		bool Initialise(SerializedProperty property, out Data data)
		{
			string path = property.propertyPath;
			if (propertyData.TryGetValue(path, out data))
			{
				return true;
			}

			data = new Data
			{
				HidesFirstEnum = fieldInfo.GetCustomAttribute<HideFirstEnumValue>() != null
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

			data.EnumNames = valuesToNames.Values.ToArray();
			string[] enumNames = data.EnumNames;

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

			SerializedProperty values = property.FindPropertyRelative("values");
			SerializedProperty keys = property.FindPropertyRelative("keys");
			if (values.arraySize > 0 || keys.arraySize > 0)
			{
				values.arraySize = 0;
				keys.arraySize = 0;
				property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
			}

			property = pairs.GetArrayElementAtIndex(0).FindPropertyRelative(valueName);

			data.MultiLine = property.hasChildren && property.propertyType == SerializedPropertyType.Generic;
			data.HasCustomPropertyDrawer = EditorUtils.HasCustomPropertyDrawer(property);

			data.Tooltips = new GUIContent[valuesToNames.Count];
			int j = 0;
			foreach (KeyValuePair<int, string> valuesToName in valuesToNames)
				data.Tooltips[j++] = new GUIContent(string.Empty, valuesToName.Key.ToString());

			data.PropertyHeights = new float[valuesToNames.Count];
			propertyData.Add(path, data);
			return true;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!Initialise(property, out Data data))
			{
				EditorGUI.HelpBox(
					position,
					"There were issues initialising this drawer!",
					MessageType.Error
				);
				return;
			}

			Rect originalPosition = position;
			position.height = EditorGUIUtility.singleLineHeight;
			using (new EditorGUI.PropertyScope(position, GUIContent.none, property))
			{
				if (EditorGUIUtils.DrawHeaderWithFoldout(position, label, property.isExpanded, noBoldOrIndent: true))
				{
					property.isExpanded = !property.isExpanded;
					if (Event.current.alt)
					{
						var pairsToCollapseOrExpand = property.FindPropertyRelative(pairsName);
						for (int i = 0; i < pairsToCollapseOrExpand.arraySize; i++)
						{
							SerializedProperty pair = pairsToCollapseOrExpand.GetArrayElementAtIndex(i);
							pair.isExpanded = property.isExpanded;
						}
					}
				}
			}

			if (!property.isExpanded)
				return;

			(
				string[] enumNames,
				float[] propertyHeights,
				GUIContent[] tooltips,
				bool multiLine,
				bool hidesFirstEnum,
				bool hasCustomPropertyDrawer
			) = data;

			const float indentSize = 5;

			originalPosition.yMin = position.yMax;
			originalPosition.xMin += indentSize - 5;
			EditorGUIUtils.DrawOutline(originalPosition, 1);

			position.xMin += indentSize; //Indent
			position.xMax -= 4;
			position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing * 2;

			SerializedProperty pairs = property.FindPropertyRelative(pairsName);
			pairs.arraySize = enumNames.Length;
			float contentX = position.x + EditorGUIUtility.labelWidth;
			float contentWidth = position.width - EditorGUIUtility.labelWidth;
			for (int i = hidesFirstEnum ? 1 : 0; i < enumNames.Length; i++)
			{
				SerializedProperty pair = pairs.GetArrayElementAtIndex(i);
				SerializedProperty value = pair.FindPropertyRelative(valueName);
				Rect labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth - 4, position.height);
				Rect contentRect = new Rect(contentX, position.y, contentWidth, position.height);
				if (multiLine)
				{
					Rect r = new Rect(position.x - 5, contentRect.y - 2, position.width + 8, contentRect.height + 2);
					EditorGUI.DrawRect(r, EditorGUIUtils.HeaderColor);

					labelRect.xMin += 12;

					using (new EditorGUI.PropertyScope(r, GUIContent.none, value))
					{
						GUI.enabled = false;
						EditorGUI.Popup(labelRect, i, enumNames);
						GUI.enabled = true;
					}

					EditorGUI.LabelField(labelRect, tooltips[i]);

					//Foldout
					pair.isExpanded = EditorGUI.Foldout(new Rect(r.x + 15, r.y, r.width, r.height), pair.isExpanded, GUIContent.none, true);

					contentRect.y = labelRect.yMax + EditorGUIUtility.standardVerticalSpacing;
					if (!pair.isExpanded)
					{
						EditorGUI.DrawRect(new Rect(r.x, labelRect.yMax, r.width, 1), EditorGUIUtils.SplitterColor);
						position.y = contentRect.y;
						continue;
					}

					using (new EditorGUI.PropertyScope(new Rect(position.x, contentRect.y, position.width, contentRect.height), GUIContent.none, value))
					{
						contentRect.xMin = labelRect.x;
						if (hasCustomPropertyDrawer)
						{
							EditorGUI.PropertyField(contentRect, value, true);
							contentRect.y += propertyHeights[i];
						}
						else
						{
							SerializedProperty end = value.GetEndProperty();
							bool enterChildren = true;
							while (value.NextVisible(enterChildren) && !SerializedProperty.EqualContents(value, end))
							{
								contentRect.height = EditorGUI.GetPropertyHeight(value, true);
								using (new EditorGUI.PropertyScope(new Rect(position.x, contentRect.y, position.width, contentRect.height), GUIContent.none, value))
								{
									contentRect.xMin = labelRect.x;
									EditorGUI.PropertyField(contentRect, value, true);
								}

								contentRect.NextGUIRect();
								enterChildren = false;
							}
						}

						contentRect.y += EditorGUIUtility.standardVerticalSpacing;
					}
				}
				else
				{
					GUI.enabled = false;
					EditorGUI.Popup(labelRect, i, enumNames);
					GUI.enabled = true;
					EditorGUI.LabelField(labelRect, tooltips[i]);

					Color lastColor = GUI.color;
					if (value.propertyType == SerializedPropertyType.ObjectReference)
					{
						if (value.objectReferenceValue == null)
							GUI.color *= new Color(1f, 0.46f, 0.51f);
					}

					using (new EditorGUI.PropertyScope(new Rect(position.x, contentRect.y, position.width, contentRect.height), GUIContent.none, value))
						EditorGUI.PropertyField(contentRect, value, GUIContent.none, true);
					contentRect.y += propertyHeights[i] + EditorGUIUtility.standardVerticalSpacing;
					GUI.color = lastColor;
				}

				position.y = contentRect.yMin;
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// Only drawing header if not expanded.
			if (!property.isExpanded)
				return EditorGUIUtility.singleLineHeight;

			float heightWithSpacing = EditorGUIUtils.HeightWithSpacing;
			float totalHeight = heightWithSpacing;

			// Do not draw this property if it fails to initialise. Instead draw a HelpBox.
			if (!Initialise(property, out Data data)) return totalHeight * 2;

			(
				string[] enumNames,
				float[] propertyHeights,
				_,
				bool multiLine,
				bool hidesFirstEnum,
				bool hasCustomPropertyDrawer
			) = data;

			if (multiLine)
			{
				SerializedProperty pairs = property.FindPropertyRelative(pairsName);
				for (int i = hidesFirstEnum ? 1 : 0; i < enumNames.Length; i++)
				{
					SerializedProperty pair = pairs.GetArrayElementAtIndex(i);
					SerializedProperty value = pair.FindPropertyRelative(valueName);
					totalHeight += heightWithSpacing;
					if (!pair.isExpanded)
						continue;

					value.isExpanded = true;
					float propertyHeight = EditorGUI.GetPropertyHeight(value, true) + EditorGUIUtility.standardVerticalSpacing;
					if (!hasCustomPropertyDrawer)
						propertyHeight -= heightWithSpacing;
					propertyHeights[i] = propertyHeight;
					totalHeight += propertyHeight + EditorGUIUtility.standardVerticalSpacing;
				}
			}
			else
			{
				// Non-multi-line properties are drawn inline without a dropdown.
				for (int i = hidesFirstEnum ? 1 : 0; i < enumNames.Length; i++)
					propertyHeights[i] = EditorGUIUtility.singleLineHeight;

				totalHeight += (hidesFirstEnum ? enumNames.Length - 1 : enumNames.Length) * heightWithSpacing;
			}

			return totalHeight
			       // Padding
			       + EditorGUIUtility.standardVerticalSpacing * 2;
		}
	}
}