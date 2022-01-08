using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Vertx.Utilities.Editor
{
	[CustomPropertyDrawer(typeof(ComponentBuilder.ReferenceGroup))]
	public class ComponentReferenceGroupDrawer : PropertyDrawer
	{
		private class ObjectSelectionElement : IAdvancedDropdownItem
		{
			public string Name { get; }
			public string Path => string.Empty;
			public Texture2D Icon { get; }
			public Object Object { get; }

			public ObjectSelectionElement(string name, Object o)
			{
				Name = name;
				Object = o;
				Icon = GetIconFromObjectType(o.GetType());
			}

			static Texture2D GetIconFromObjectType(Type t)
			{
				Texture2D icon = EditorGUIUtility.ObjectContent(null, t).image as Texture2D;
				if (icon == null && t.IsSubclassOf(typeof(Component)))
					icon = EditorGUIUtility.FindTexture("cs Script Icon");
				return icon;
			}
		}

		public override VisualElement CreatePropertyGUI(SerializedProperty property)
		{
			VisualElement root = new VisualElement();

			SerializedProperty objectProperty = property.FindPropertyRelative(nameof(ComponentBuilder.ReferenceGroup.Object));
			PropertyField objectField = new PropertyField(objectProperty, string.Empty);
			objectField.RegisterValueChangeCallback(evt =>
			{
				var componentProperty = evt.changedProperty;
				switch (componentProperty.objectReferenceValue)
				{
					case ComponentBuilder _:
						componentProperty.objectReferenceValue = null;
						break;
					case GameObject gameObject:
						List<ObjectSelectionElement> elements = new List<ObjectSelectionElement> { new ObjectSelectionElement("GameObject", gameObject) };
						var components = gameObject.GetComponents<Component>();
						foreach (Component component in components)
						{
							if (component == null) continue; // This can be the case if the script has been deleted.
							if ((component.hideFlags & HideFlags.HideInHierarchy) != 0) continue;
							if (component is ComponentBuilder) continue;
							elements.Add(new ObjectSelectionElement(component.GetType().Name, component));
						}

						var dropdown = new AdvancedDropdownUtils.AdvancedDropdownWithCallbacks<ObjectSelectionElement>(
							new AdvancedDropdownState(),
							"Select Reference",
							default,
							elements,
							element =>
							{
								componentProperty.objectReferenceValue = element.Object;
								componentProperty.serializedObject.ApplyModifiedProperties();
							}
						);
						dropdown.Show(objectField.worldBound);
						break;
				}
			});
			root.Add(objectField);

			SerializedProperty referenceTypeProperty = property.FindPropertyRelative(nameof(ComponentBuilder.ReferenceGroup.ReferenceType));
			PropertyField referenceField = new PropertyField(referenceTypeProperty, string.Empty);
			root.Add(referenceField);

			SerializedProperty nameProperty = property.FindPropertyRelative(nameof(ComponentBuilder.ReferenceGroup.Name));
			PropertyField nameField = new PropertyField(nameProperty, "Name");
			nameField.RegisterValueChangeCallback(evt =>
			{
				evt.changedProperty.stringValue = ComponentBuilderInspector.SanitiseIdentifier(evt.changedProperty.stringValue);
				evt.changedProperty.serializedObject.ApplyModifiedProperties();
			});
			root.Add(nameField);

			return root;
		}
	}
}