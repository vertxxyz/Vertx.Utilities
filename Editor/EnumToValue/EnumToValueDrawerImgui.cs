#if UNITY_2020_1_OR_NEWER
using UnityEditor;
using UnityEngine;

namespace Vertx.Utilities.Editor
{
	public partial class EnumToValueDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!Initialise(property, out Data data, Mode.Imgui))
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
				GUIContent[] enumNames,
				float[] propertyHeights,
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
						EditorGUI.LabelField(labelRect, enumNames[i]);

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
					EditorGUI.LabelField(labelRect, enumNames[i]);

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
			if (!Initialise(property, out Data data, Mode.Imgui)) return totalHeight * 2;

			(
				GUIContent[] enumNames,
				float[] propertyHeights,
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
#endif