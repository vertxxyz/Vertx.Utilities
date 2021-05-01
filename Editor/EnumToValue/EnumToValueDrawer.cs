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
		private bool initialised;

		private string[] enumNames;
		private float[] propertyHeights;
		private GUIContent[] tooltips;
		private bool multiLine;
		private bool dictionary;
		private bool hidesFirstEnum;
		private bool hasCustomPropertyDrawer;

		bool Initialise(SerializedProperty property)
		{
			if (initialised) return true;
			
			hidesFirstEnum = fieldInfo.GetCustomAttribute<HideFirstEnumValue>() != null;
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

			dictionary = baseType.GetGenericTypeDefinition() == typeof(EnumToValueDictionary<,>);

			SerializedProperty values = property.FindPropertyRelative("values");
			SerializedProperty hidesFirstEnumProp = property.FindPropertyRelative("hidesFirstEnum");

			if (dictionary)
			{
				//Remove any enum names that have duplicate indices.
				for (int i = 0; i < names.Length; i++)
				{
					int value = (int) enumValues.GetValue(i);
					string name = names[i];
					if (valuesToNames.ContainsKey(value))
						valuesToNames[value] = $"{valuesToNames[value]}/{name}";
					else
						valuesToNames.Add(value, name);
				}

				enumNames = valuesToNames.Values.ToArray();
				SerializedProperty keys = property.FindPropertyRelative("keys");

				//Get the values currently stored on the object and make sure they're set to the correct indices in the resized list.
				FieldInfo valuesField = baseType.GetField("values", BindingFlags.Instance | BindingFlags.NonPublic);
				FieldInfo keysField = baseType.GetField("keys", BindingFlags.Instance | BindingFlags.NonPublic);

				//Collect former values
				var dictionaryOldValues = new Dictionary<int, object>();
				object parent = EditorUtils.GetObjectFromProperty(property, out _, out _);
				Array valuesArray = (Array) valuesField.GetValue(parent);
				for (int i = 0; i < keys.arraySize; i++)
				{
					int key = keys.GetArrayElementAtIndex(i).intValue;
					if (dictionaryOldValues.ContainsKey(key)) continue;
					if(i >= valuesArray.Length) continue;
					dictionaryOldValues.Add(key, valuesArray.GetValue(i));
				}

				//Resize the arrays
				int length = valuesToNames.Count;
				keys.arraySize = length;
				values.arraySize = length;
				property.serializedObject.ApplyModifiedPropertiesWithoutUndo();

				//Apply the old dictionary values to the new arrays.
				Array valuesArrayFinal = (Array) valuesField.GetValue(parent);
				Array keysArrayFinal = (Array) keysField.GetValue(parent);
				int index = 0;
				foreach (KeyValuePair<int, string> valueToName in valuesToNames)
				{
					keysArrayFinal.SetValue(valueToName.Key, index);
					if (dictionaryOldValues.TryGetValue(valueToName.Key, out var @object))
						valuesArrayFinal.SetValue(@object, index);
					index++;
				}

				EditorUtility.SetDirty(property.serializedObject.targetObject);
				property.serializedObject.Update();
			}
			else
			{
				//Remove any enum names that have duplicate indices.
				int current = 0;
				foreach (string name in names)
				{
					int index = (int) Enum.Parse(enumType, name);
					if (index > current)
						return false;

					if (valuesToNames.ContainsKey(index))
					{
						valuesToNames[index] = $"{valuesToNames[index]}/{name}";
						continue;
					}

					valuesToNames.Add(index, name);
					current++;
				}

				enumNames = valuesToNames.Values.ToArray();
				values.arraySize = enumNames.Length;
			}
			
			property = values.GetArrayElementAtIndex(0);
			
			multiLine = property.hasChildren && property.propertyType == SerializedPropertyType.Generic;
			hasCustomPropertyDrawer = EditorUtils.HasCustomPropertyDrawer(property);

			tooltips = new GUIContent[valuesToNames.Count];
			int j = 0;
			foreach (KeyValuePair<int, string> valuesToName in valuesToNames)
				tooltips[j++] = new GUIContent(string.Empty, valuesToName.Key.ToString());

			propertyHeights = new float[valuesToNames.Count];

			hidesFirstEnumProp.boolValue = hidesFirstEnum;
			hidesFirstEnumProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();
			
			initialised = true;
			return true;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!Initialise(property))
			{
				EditorGUI.HelpBox(
					position,
					"EnumToValue does not support non-consecutive enum values! Use an EnumToValueDictionary instead.",
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
						var valuesToCollapseOrExpand = property.FindPropertyRelative("values");
						for (int i = 0; i < valuesToCollapseOrExpand.arraySize; i++)
						{
							SerializedProperty value = valuesToCollapseOrExpand.GetArrayElementAtIndex(i);
							value.isExpanded = property.isExpanded;
						}
					}
				}
			}

			if (!property.isExpanded)
				return;

			const float indentSize = 5;

			originalPosition.yMin = position.yMax;
			originalPosition.xMin += indentSize - 5;
			EditorGUIUtils.DrawOutline(originalPosition, 1);

			position.xMin += indentSize; //Indent
			position.xMax -= 4;
			position.y = position.yMax + EditorGUIUtility.standardVerticalSpacing * 2;

			SerializedProperty values = property.FindPropertyRelative("values");
			values.arraySize = enumNames.Length;
			float contentX = position.x + EditorGUIUtility.labelWidth;
			float contentWidth = position.width - EditorGUIUtility.labelWidth;
			for (int i = hidesFirstEnum ? 1 : 0; i < enumNames.Length; i++)
			{
				SerializedProperty value = values.GetArrayElementAtIndex(i);
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
					value.isExpanded = EditorGUI.Foldout(new Rect(r.x + 15, r.y, r.width, r.height), value.isExpanded, GUIContent.none, true);

					contentRect.y = labelRect.yMax + EditorGUIUtility.standardVerticalSpacing;
					if (!value.isExpanded)
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
			float heightWithSpacing = EditorGUIUtils.HeightWithSpacing;
			float totalHeight = heightWithSpacing;
			//Only drawing header if not expanded.
			if (!property.isExpanded)
				return totalHeight;

			//Do not draw this property if it fails to initialise. Instead draw a HelpBox.
			if (!Initialise(property)) return totalHeight * 2;

			if (multiLine)
			{
				SerializedProperty values = property.FindPropertyRelative("values");
				for (int i = hidesFirstEnum ? 1 : 0; i < enumNames.Length; i++)
				{
					SerializedProperty value = values.GetArrayElementAtIndex(i);
					totalHeight += heightWithSpacing;
					if (!value.isExpanded)
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
				//Non-multi-line properties are drawn inline without a dropdown.
				for (int i = hidesFirstEnum ? 1 : 0; i < enumNames.Length; i++)
					propertyHeights[i] = EditorGUIUtility.singleLineHeight;
				
				totalHeight += (hidesFirstEnum ? enumNames.Length - 1 : enumNames.Length) * heightWithSpacing;
			}

			return totalHeight
			       //Padding
			       + EditorGUIUtility.standardVerticalSpacing * 2;
		}
	}
}