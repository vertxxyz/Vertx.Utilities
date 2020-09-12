using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Vertx.Utilities;
using Vertx.Utilities.Editor;

namespace Vertx.Utilities.Editor
{
	[CustomPropertyDrawer(typeof(EnumToValueBase), true)]
	public class EnumToValueDrawer : PropertyDrawer
	{
		private bool initialised;

		private string[] enumNames;
		private float[] propertyHeights;
		private GUIContent[] tooltips;
		private float totalPropertyHeight;
		private bool multiLine;
		private bool dictionary;
		private bool hidesFirstEnum;

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
					{
						Debug.LogWarning("EnumToValue does not support non-consecutive enum values use an EnumToValueDictionary instead." +
						                 $"{enumType} in {property.serializedObject.targetObject}");
						return false;
					}

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

			tooltips = new GUIContent[valuesToNames.Count];
			int j = 0;
			foreach (KeyValuePair<int, string> valuesToName in valuesToNames)
				tooltips[j++] = new GUIContent(string.Empty, valuesToName.Key.ToString());

			List<float> heights = new List<float>();
			totalPropertyHeight = 0;

			property = values.GetArrayElementAtIndex(0);
			if (property.hasChildren && property.propertyType == SerializedPropertyType.Generic)
			{
				multiLine = true;
				SerializedProperty end = property.GetEndProperty();
				bool enterChildren = true;
				while (property.NextVisible(enterChildren) && !SerializedProperty.EqualContents(property, end))
				{
					float height = EditorGUI.GetPropertyHeight(property, false);
					heights.Add(height);
					totalPropertyHeight += height + EditorGUIUtility.standardVerticalSpacing;
					enterChildren = false;
				}
			}
			else
			{
				multiLine = false;
				float height = EditorGUI.GetPropertyHeight(property, false);
				heights.Add(height);
				totalPropertyHeight = height + EditorGUIUtility.standardVerticalSpacing;
			}

			propertyHeights = heights.ToArray();

			hidesFirstEnumProp.boolValue = hidesFirstEnum;
			hidesFirstEnumProp.serializedObject.ApplyModifiedPropertiesWithoutUndo();

			initialised = true;
			return true;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!Initialise(property)) return;

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

			originalPosition.yMin = position.yMax;
			originalPosition.xMin += 10;
			EditorGUIUtils.DrawOutline(originalPosition, 1);

			position.xMin += 15; //Indent
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

					SerializedProperty end = value.GetEndProperty();
					int index = 0;
					bool enterChildren = true;
					while (value.NextVisible(enterChildren) && !SerializedProperty.EqualContents(value, end))
					{
						float height = propertyHeights[index++];
						contentRect.height = height;
						using (new EditorGUI.PropertyScope(new Rect(position.x, contentRect.y, position.width, contentRect.height), GUIContent.none, value))
						{
							float labelRectX = labelRect.x + 15;
							GUI.Label(new Rect(labelRectX, contentRect.y, contentRect.x - labelRectX - 2, contentRect.height), value.displayName);
							EditorGUI.PropertyField(contentRect, value, GUIContent.none, false);
						}

						contentRect.y += height + EditorGUIUtility.standardVerticalSpacing;
						enterChildren = false;
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
						EditorGUI.PropertyField(contentRect, value, GUIContent.none);
					contentRect.y += propertyHeights[0] + EditorGUIUtility.standardVerticalSpacing;
					GUI.color = lastColor;
				}

				position.y = contentRect.yMin;
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (!property.isExpanded)
				return EditorGUIUtility.singleLineHeight;
			if (!Initialise(property)) return 0;
			int length = hidesFirstEnum ? enumNames.Length - 1 : enumNames.Length;
			int expandedLength = length;
			if (multiLine)
			{
				SerializedProperty values = property.FindPropertyRelative("values");
				for (int i = hidesFirstEnum ? 1 : 0; i < enumNames.Length; i++)
				{
					SerializedProperty value = values.GetArrayElementAtIndex(i);
					if (!value.isExpanded) expandedLength--;
				}
			}

			return
				//Heading
				EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing +
				//Multi-line skip enum heading
				(multiLine ? (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing) * length : 0) +
				//Property height
				totalPropertyHeight * (multiLine ? expandedLength : length)
				//Padding
				+ EditorGUIUtility.standardVerticalSpacing * 2;
		}
	}
}