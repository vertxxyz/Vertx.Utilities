using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

#if UNITY_2020_1_OR_NEWER

namespace Vertx.Utilities.Editor
{
	public partial class EnumToValueDrawer
	{
		private const string UssStyle = "vertx-enum-to-value";
		private const string UssDropdownStyle = UssStyle + "__header-dropdown";
		private const string UssDropdownBackgroundStyle = UssDropdownStyle + "-background";
		private const string UssPropertyDropdownStyle = UssStyle + "__property";
		private const string UssPropertySingleLineStyle = UssPropertyDropdownStyle + "--single-line";
		private const string UssBoxStyle = "vertx-box";
		private const string UssBoxOutlinedStyle = "vertx-box--outlined";
		
		private static StyleSheet _styleSheet;
		
		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			if (!Initialise(property, out Data data, Mode.UIToolkit))
			{
				return new HelpBox(
					"There were issues initialising this drawer!",
					HelpBoxMessageType.Error
				);
			}
			
			if (_styleSheet == null)
				_styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.vertx.utilities/Editor/Assets/EnumToValueStyles.uss");

			var foldout = new Foldout
			{
				value = property.isExpanded,
				text = property.displayName
			};
			AddBackgroundToFoldout(foldout);
			foldout.styleSheets.Add(_styleSheet);
			foldout.AddToClassList(UssDropdownStyle);
			foldout.RegisterValueChangedCallback(evt => property.isExpanded = evt.newValue);

			var box = new VisualElement();
			box.AddToClassList(UssBoxStyle);
			box.AddToClassList(UssBoxOutlinedStyle);
			foldout.Add(box);
			
			(
				GUIContent[] enumNames,
				_,
				bool multiLine,
				bool hidesFirstEnum,
				bool hasCustomPropertyDrawer
			) = data;
			
			SerializedProperty pairs = property.FindPropertyRelative(pairsName);
			pairs.arraySize = enumNames.Length;
			for (int i = hidesFirstEnum ? 1 : 0; i < enumNames.Length; i++)
			{
				SerializedProperty pair = pairs.GetArrayElementAtIndex(i);
				SerializedProperty value = pair.FindPropertyRelative(valueName);
				if (multiLine)
				{
					var propertyFoldout = new Foldout
					{
						value = pair.isExpanded,
						text = enumNames[i].text
					};
					propertyFoldout.Q<Label>().tooltip = enumNames[i].tooltip;
					AddBackgroundToFoldout(propertyFoldout);
					propertyFoldout.RegisterValueChangedCallback(evt => pair.isExpanded = evt.newValue);
					propertyFoldout.AddToClassList(UssPropertyDropdownStyle);

					if (hasCustomPropertyDrawer)
					{
						propertyFoldout.Add(new PropertyField(value, null));
					}
					else
					{
						SerializedProperty end = value.GetEndProperty();
						bool enterChildren = true;
						while (value.NextVisible(enterChildren) && !SerializedProperty.EqualContents(value, end))
						{
							propertyFoldout.Add(new PropertyField(value));
							enterChildren = false;
						}
					}
					
					box.Add(propertyFoldout);
				}
				else
				{
					PropertyField propertyField = new PropertyField(value, enumNames[i].text)
					{
						tooltip = enumNames[i].tooltip
					};
					propertyField.AddToClassList(UssPropertySingleLineStyle);
					propertyField.AddToClassList(BaseField<int>.alignedFieldUssClassName);
					box.Add(propertyField);
				}
			}
			return foldout;

			void AddBackgroundToFoldout(Foldout root)
			{
				var foldoutBackground = new VisualElement { pickingMode = PickingMode.Ignore };
				foldoutBackground.AddToClassList(UssDropdownBackgroundStyle);
				root.Q<Toggle>().hierarchy.Insert(0, foldoutBackground);
			}
		}
	}
}
#endif