﻿using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Vertx.Utilities.Editor.Internal;

namespace Vertx.Utilities.Editor
{
	[CustomEditor(typeof(PooledListView))]
	public class PooledListViewInspector : UnityEditor.Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			VisualElement root = new VisualElement();
			root.Add(new PropertyField(serializedObject.FindProperty("m_Content")));

			root.Add(new PropertyField(serializedObject.FindProperty("snapping")));
			root.Add(new PropertyField(serializedObject.FindProperty("m_MovementType")));
			//pad inwards
			root.Add(IndentedProperty(serializedObject.FindProperty("m_Elasticity"), "Elasticity"));

			root.Add(new PropertyField(serializedObject.FindProperty("m_Inertia")));
			//pad inwards
			root.Add(IndentedProperty(serializedObject.FindProperty("m_DecelerationRate"), "Deceleration Rate"));

			root.Add(new PropertyField(serializedObject.FindProperty("m_Viewport"))
			{
				style =
				{
					marginTop = 5
				}
			});
			root.Add(new PropertyField(serializedObject.FindProperty("m_VerticalScrollbar")));
			//pad inwards
			root.Add(IndentedProperty(serializedObject.FindProperty("m_VerticalScrollbarVisibility"), "Visibility"));
			root.Add(IndentedProperty(serializedObject.FindProperty("m_VerticalScrollbarSpacing"), "Spacing"));

			root.Add(new PropertyField(serializedObject.FindProperty("prefab"))
			{
				style =
				{
					marginTop = 5
				}
			});
			root.Add(new PropertyField(serializedObject.FindProperty("elementHeight")));

			root.Add(IndentedProperty(serializedObject.FindProperty("selectOnUp"), "Select On Up"));
			root.Add(IndentedProperty(serializedObject.FindProperty("selectOnDown"), "Select On Down"));
			root.Add(IndentedProperty(serializedObject.FindProperty("selectOnLeft"), "Select On Left"));
			root.Add(IndentedProperty(serializedObject.FindProperty("selectOnRight"), "Select On Right"));

			root.Add(new PropertyField(serializedObject.FindProperty("m_OnValueChanged"))
			{
				style =
				{
					marginTop = 10
				}
			});
			root.Add(new PropertyField(serializedObject.FindProperty("bindItem")));

			return root;
		}

		private static PropertyField IndentedProperty(SerializedProperty property, string label)
		{
			var pF = new PropertyField(property, label);
			pF.RegisterCallback<GeometryChangedEvent, PropertyField>((_, field) =>
			{
				var l = field.Q<Label>();
				l.style.marginLeft = 16;
				l.style.marginRight = -16;
			}, pF);
			return pF;
		}

		[MenuItem("GameObject/UI/Pooled List View", false, 2063)]
		public static void AddPooledListView(MenuCommand menuCommand)
		{
			InternalExtensions.AddScrollViewToSelectedObject();
			//The created GameObject is set to the current selection, so we can retrieve it via there.
			GameObject scrollView = Selection.activeGameObject;

			//Rename the GameObject
			scrollView.name = "List View";
			using (var sO = new SerializedObject(scrollView.GetComponent<ScrollRect>()))
			{
				//Replace the script
				var pooledListViewScript = EditorUtils.LoadAssetOfType<MonoScript>(nameof(PooledListView));
				sO.FindProperty("m_Script").objectReferenceValue = pooledListViewScript;

				//Remove the horizontal scroll bar
				var horizontalScrollbar = sO.FindProperty("m_HorizontalScrollbar");
				var scrollBar = (Scrollbar)horizontalScrollbar.objectReferenceValue;
				DestroyImmediate(scrollBar.gameObject, true);
				horizontalScrollbar.objectReferenceValue = null;

				//Fix the scroll bar so that it takes up the total vertical space of the viewport.
				var verticalScrollbar = (Scrollbar)sO.FindProperty("m_VerticalScrollbar").objectReferenceValue;
				((RectTransform)verticalScrollbar.transform).offsetMin = new Vector2(-20, 0);

				sO.ApplyModifiedPropertiesWithoutUndo();
			}
		}
	}
}