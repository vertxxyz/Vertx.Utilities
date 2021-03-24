using System;
using System.Collections.Generic;
using RiftWreckers;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Vertx.Utilities.Editor
{
	[CustomPropertyDrawer(typeof(InterfaceProviderAttribute))]
	public class InterfaceProviderDrawer : PropertyDrawer
	{
		private static readonly int managedRefStringLength = "managedReference<".Length;

		private struct ProviderDetails
		{
			public GUIContent AddLabel, ChangeLabel;
		}

		private static readonly Dictionary<string, ProviderDetails> detailsLookup = new Dictionary<string, ProviderDetails>();
		private static readonly Dictionary<string, GUIContent> typeLabelLookup = new Dictionary<string, GUIContent>();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.propertyType != SerializedPropertyType.ManagedReference)
			{
				EditorGUI.HelpBox(position, "Property is not a SerializeReference field.", MessageType.Error);
				return;
			}

			string propName = ObjectNames.NicifyVariableName(property.name);
			if (!detailsLookup.TryGetValue(propName, out var details))
			{
				detailsLookup.Add(propName, details = new ProviderDetails
				{
					AddLabel = new GUIContent($"Add {propName}"),
					ChangeLabel = new GUIContent($"Change {propName}")
				});
			}

			if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
			{
				if (GUI.Button(position, details.AddLabel))
					ShowPropertyDropdown();
			}
			else
			{
				Rect fullPos = position;
				position.height = EditorGUIUtility.singleLineHeight;
				if (GUI.Button(position, details.ChangeLabel))
					ShowPropertyDropdown();
				fullPos.y += EditorGUIUtils.HeightWithSpacing;

				if (!typeLabelLookup.TryGetValue(property.type, out var typeLabel))
				{
					typeLabelLookup.Add(
						property.type,
						typeLabel = new GUIContent(
							ObjectNames.NicifyVariableName(
								property.type.Substring(managedRefStringLength, property.type.Length - managedRefStringLength - 1)
							)
						)
					);
				}

				EditorGUI.PropertyField(
					fullPos,
					property,
					typeLabel,
					true
				);
			}

			void ShowPropertyDropdown()
			{
				AdvancedDropdown dropdown = AdvancedDropdownUtils.CreateAdvancedDropdownFromType(
					((InterfaceProviderAttribute) attribute).Type,
					propName,
					element =>
					{
						property.managedReferenceValue = Activator.CreateInstance(element.Type);
						property.serializedObject.ApplyModifiedProperties();
					}
				);
				dropdown.Show(position);
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (string.IsNullOrEmpty(property.managedReferenceFullTypename))
				return EditorGUIUtility.singleLineHeight;
			return EditorGUI.GetPropertyHeight(property) + EditorGUIUtils.HeightWithSpacing;
		}
	}
}