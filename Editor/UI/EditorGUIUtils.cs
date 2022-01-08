using System;
using UnityEngine;
using UnityEditor;

namespace Vertx.Utilities.Editor
{
	public static class EditorGUIUtils
	{
		#region Obsolete

		//[Obsolete("Please update packages that call this function. It has moved to EditorUtils")]
		public static void SetProjectBrowserSearch(string search)
			=> EditorUtils.SetProjectBrowserSearch(search);

		//[Obsolete("Please update packages that call this function. It has moved to EditorUtils")]
		public static EditorWindow GetProjectBrowserWindow(bool forceOpen = false)
			=> EditorUtils.GetProjectBrowserWindow(forceOpen);

		//[Obsolete("Please update packages that call this function. It has moved to EditorUtils")]
		public static void ShowFolderContents(int folderInstanceId, bool revealAndFrameInFolderTree)
			=> EditorUtils.ShowFolderContents(folderInstanceId, revealAndFrameInFolderTree);

		//[Obsolete("Please update packages that call this function. It has moved to EditorUtils")]
		public static void ShowFolder(DefaultAsset o)
			=> EditorUtils.ShowFolder(o);

		//[Obsolete("Please update packages that call this function. It has moved to EditorUtils")]
		public static string GetCurrentlyFocusedProjectFolder()
			=> EditorUtils.GetCurrentlyFocusedProjectFolder();

		#endregion

		#region Exponential Slider

		public static void ExponentialSlider(SerializedProperty property, float yMin, float yMax, params GUILayoutOption[] options) =>
			property.floatValue = ExponentialSlider(new GUIContent(property.displayName), property.floatValue, yMin, yMax, options);

		/// <summary>
		/// Exponential slider.
		/// </summary>
		/// <returns>The exponential value</returns>
		/// <param name="val">Value.</param>
		/// <param name="yMin">Ymin at x = 0.</param>
		/// <param name="yMax">Ymax at x = 1.</param>
		/// <param name="options"></param>
		public static float ExponentialSlider(float val, float yMin, float yMax, params GUILayoutOption[] options)
		{
			Rect controlRect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight, options);
			return ExponentialSlider(controlRect, val, yMin, yMax);
		}

		public static float ExponentialSlider(GUIContent label, float val, float yMin, float yMax, params GUILayoutOption[] options)
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.PrefixLabel(label);
				val = ExponentialSlider(val, yMin, yMax, options);
			}

			return val;
		}

		public static float ExponentialSlider(Rect position, float val, float yMin, float yMax)
		{
			val = Mathf.Clamp(val, yMin, yMax);
			yMin -= 1;
			float x = Mathf.Log(val - yMin) / Mathf.Log(yMax - yMin);
			x = EditorGUI.Slider(position, x, 0, 1);
			float y = Mathf.Pow(yMax - yMin, x) + yMin;
			position.x = position.x + position.width - 50;
			position.width = 50;
			GUI.Box(position, GUIContent.none);

			GUI.SetNextControlName("vertxFloatField");
			y = Mathf.Clamp(EditorGUI.FloatField(position, y), yMin, yMax);
			if (position.Contains(Event.current.mousePosition))
				GUI.FocusControl("vertxFloatField");

			return y;
		}

		#endregion

		public static bool ButtonOverPreviousControl()
		{
			Rect r = GUILayoutUtility.GetLastRect();
			return GUI.Button(r, GUIContent.none, GUIStyle.none);
		}

		#region Header

		public static bool DrawHeader(
			GUIContent label,
			SerializedProperty activeField = null,
			float widthCutoff = 0,
			bool noBoldOrIndent = false
		)
		{
			bool ret;
			if (activeField != null)
			{
				activeField.serializedObject.Update();
				bool v = activeField.boolValue;
				ret = DrawHeader(label, ref v, false, widthCutoff, noBoldOrIndent);
				activeField.boolValue = v;
				activeField.serializedObject.ApplyModifiedProperties();
			}
			else
			{
				bool v = true;
				ret = DrawHeader(label, ref v, true, widthCutoff, noBoldOrIndent);
			}

			return ret;
		}

		public static bool DrawHeader(
			GUIContent label,
			ref bool active,
			bool hideToggle = false,
			float widthCutoff = 0,
			bool noBoldOrIndent = false,
			float headerXOffset = 0
		)
		{
			Rect r = GUILayoutUtility.GetRect(1, 17);
			return DrawHeader(r,
				label,
				ref active,
				hideToggle,
				widthCutoff,
				noBoldOrIndent,
				headerXOffset: headerXOffset
			);
		}

		public static bool DrawHeader(Rect contentRect,
			GUIContent label,
			ref bool active,
			bool hideToggle = false,
			float widthCutoff = 0,
			bool noBoldOrIndent = false,
			float backgroundXMin = 0,
			float headerXOffset = 0)
		{
			Rect labelRect = contentRect;
			if (!noBoldOrIndent)
				labelRect.xMin += 16f;
			labelRect.xMin += headerXOffset;
			labelRect.xMax -= 20f;
			Rect toggleRect = contentRect;
			if (!noBoldOrIndent)
				toggleRect.xMin = EditorGUI.indentLevel * 15;
			toggleRect.xMin += headerXOffset;
			toggleRect.y += 2f;
			toggleRect.width = 13f;
			toggleRect.height = 13f;
			contentRect.xMin = backgroundXMin;
			EditorGUI.DrawRect(contentRect, HeaderColor);
			using (new EditorGUI.DisabledScope(!active))
				EditorGUI.LabelField(labelRect, label, noBoldOrIndent ? EditorStyles.label : EditorStyles.boldLabel);
			if (!hideToggle)
			{
				active = GUI.Toggle(toggleRect, active, GUIContent.none, SmallTickbox);
				labelRect.xMin = toggleRect.xMax;
			}
			else
				labelRect.xMin = 0;

			Event current = Event.current;
			if (current.type != EventType.MouseDown)
				return false;
			labelRect.width -= widthCutoff;
			if (!labelRect.Contains(current.mousePosition))
				return false;
			if (current.button != 0)
				return false;
			current.Use();
			return true;
		}

		public static bool DrawHeaderWithFoldout(
			GUIContent label,
			bool expanded,
			float widthCutoff = 0,
			bool opensOnDragUpdated = false,
			float headerXOffset = 0
		)
		{
			bool v = true;
			bool ret = DrawHeader(label, ref v, true, widthCutoff, headerXOffset: headerXOffset);
			Rect foldoutRect = GUILayoutUtility.GetLastRect();
			foldoutRect.xMin += headerXOffset;
			if (Foldout(foldoutRect, expanded, opensOnDragUpdated))
				return true;
			return ret;
		}

		private static bool Foldout(
			Rect r,
			bool expanded,
			bool opensOnDragUpdated = false
		)
		{
			switch (Event.current.type)
			{
				case EventType.DragUpdated:
					if (opensOnDragUpdated && !expanded)
					{
						if (r.Contains(Event.current.mousePosition))
						{
							Event.current.Use();
							return true;
						}
					}

					break;
				case EventType.Repaint:
					//Only draw the Foldout - don't use it as a button or get focus
					r.x += 3;
					r.x += EditorGUI.indentLevel * 15;
					r.y += 1f;
					EditorStyles.foldout.Draw(r, GUIContent.none, -1, expanded);
					break;
			}

			return false;
		}

		public static bool DrawHeaderWithFoldout(
			Rect rect,
			GUIContent label,
			bool expanded,
			float widthCutoff = 0,
			bool noBoldOrIndent = false,
			float backgroundXMin = 0,
			float headerXOffset = 0
		)
		{
			bool v = true; //Can't wait for c# 7
			bool ret = DrawHeader(
				rect,
				label,
				ref v,
				true,
				widthCutoff,
				noBoldOrIndent,
				backgroundXMin,
				headerXOffset
			);
			rect.Indent(headerXOffset);
			if (noBoldOrIndent)
				rect.xMin -= 16;
			if (Foldout(rect, expanded))
				return true;
			return ret;
		}

		public static void DrawSplitter(
			bool inverse = false,
			bool alignXMinToZero = true
		)
		{
			Rect rect = GUILayoutUtility.GetRect(1f, 1f);
			if (alignXMinToZero)
				rect.xMin = 0.0f;
			if (Event.current.type != EventType.Repaint)
				return;
			Color c = inverse ? InverseSplitterColor : SplitterColor;
			c.a = GUI.color.a;
			EditorGUI.DrawRect(rect, c);
		}

		public static Color SplitterColorPro = new Color(0.12f, 0.12f, 0.12f, 1.333f);
		public static Color SplitterColorNonPro = new Color(0.6f, 0.6f, 0.6f, 1.333f);
		public static Color SplitterColor => EditorGUIUtility.isProSkin ? SplitterColorPro : SplitterColorNonPro;
		public static Color InverseSplitterColor => !EditorGUIUtility.isProSkin ? SplitterColorPro : SplitterColorNonPro;
		public static Color HeaderColor => !EditorGUIUtility.isProSkin ? new Color(1, 1, 1, 0.2f) : new Color(0.1f, 0.1f, 0.1f, 0.2f);

		private static GUIStyle _smallTickbox;
		public static GUIStyle SmallTickbox => _smallTickbox ?? (_smallTickbox = new GUIStyle("ShurikenCheckMark"));

		#endregion

		#region Outline

		public class OutlineScope : IDisposable
		{
			private readonly EditorGUILayout.VerticalScope scope;

			private static GUIStyle _smallMargins;

			private static GUIStyle SmallMargins => _smallMargins ?? (_smallMargins = new GUIStyle(EditorStyles.inspectorDefaultMargins)
			{
				padding = new RectOffset(4, 4, 2, 2),
			});

			public OutlineScope(bool drawBackground = true, bool largeMargins = true)
			{
				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
				scope = new EditorGUILayout.VerticalScope(largeMargins ? EditorStyles.inspectorDefaultMargins : SmallMargins);
				Rect rect = scope.rect;
				if (drawBackground)
				{
					if (Event.current.type == EventType.Repaint)
					{
						Color orgColor = GUI.color;
						GUI.color = BackgroundColor;
						GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
						GUI.color = orgColor;
					}
				}

				DrawOutline(new Rect(rect.x, rect.y - 1, rect.width, rect.height + 1), 1);
			}

			public void Dispose()
			{
				GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
				scope.Dispose();
			}
		}

		private static GUIStyle _headerBackground;
		private static GUIStyle HeaderBackground => _headerBackground ?? (_headerBackground = "RL Header");

		private static GUIStyle _boxBackground;
		private static GUIStyle BoxBackground => _boxBackground ?? (_boxBackground = "RL Background");

		private static GUIStyle _smallPadding;

		private static GUIStyle SmallPadding => _smallPadding ?? (_smallPadding = new GUIStyle
		{
			padding = new RectOffset(6, 4, 2, 4)
		});

		public static void DrawHeaderWithBackground(GUIContent label)
		{
			Rect rect = EditorGUILayout.GetControlRect(false, HeightWithSpacing);
			if (Event.current.type == EventType.Repaint)
				HeaderBackground.Draw(rect, GUIContent.none, 0);
			rect.Indent(5);
			GUI.Label(rect, label);
		}

		public static void DrawHeaderWithBackground(Rect position, GUIContent label)
		{
			if (Event.current.type == EventType.Repaint)
				HeaderBackground.Draw(position, GUIContent.none, 0);
			position.Indent(5);
			GUI.Label(position, label);
		}

		public class ContainerScope : IDisposable
		{
			private readonly int bottomMargin;
			private readonly int bottomPadding;
			private readonly EditorGUILayout.VerticalScope scope;

			public ContainerScope(GUIContent headerLabel, int bottomMargin = 8, int bottomPadding = 5)
			{
				this.bottomMargin = bottomMargin;
				this.bottomPadding = bottomPadding;
				DrawHeaderWithBackground(headerLabel);
				scope = new EditorGUILayout.VerticalScope(SmallPadding);
				Rect rect = scope.rect;
				rect.yMin -= 2;

				DrawBoxBackground(rect);
			}

			public void Dispose()
			{
				GUILayout.Space(bottomPadding);
				scope.Dispose();
				GUILayout.Space(bottomMargin);
			}
		}

		public static void DrawBoxBackground(Rect rect)
		{
			if (Event.current.type == EventType.Repaint)
				BoxBackground.Draw(rect, GUIContent.none, 0);
		}

		public static void DrawOutline(Rect rect, float size)
		{
			if (Event.current.type != EventType.Repaint)
				return;

			Color orgColor = GUI.color;
			GUI.color *= OutlineColor;
			GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, size), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.x, rect.yMax - size, rect.width, size), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.x, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);
			GUI.DrawTexture(new Rect(rect.xMax - size, rect.y + 1, size, rect.height - 2 * size), EditorGUIUtility.whiteTexture);

			GUI.color = orgColor;
		}

		private static Color OutlineColor => EditorGUIUtility.isProSkin ? new Color(0.12f, 0.12f, 0.12f, 1.333f) : new Color(0.6f, 0.6f, 0.6f, 1.333f);
		private static Color BackgroundColor => EditorGUIUtility.isProSkin ? new Color(0.2f, 0.2f, 0.2f) : new Color(0.9f, 0.9f, 0.9f);

		#endregion

		#region Rect Extensions

		public static void NextGUIRect(this ref Rect rect) => rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;

		public static Rect GetNextGUIRect(this Rect rect)
		{
			rect.y = rect.yMax + EditorGUIUtility.standardVerticalSpacing;
			return rect;
		}

		public static void Indent(this ref Rect rect) => rect = EditorGUI.IndentedRect(rect);
		public static void Indent(this ref Rect rect, float amount) => rect.xMin += amount;

		public static Rect GetIndentedRect(this ref Rect rect) => EditorGUI.IndentedRect(rect);

		public static Rect GetIndentedRect(this ref Rect rect, float amount)
		{
			Rect r = rect;
			r.Indent(amount);
			return r;
		}

		#endregion

		#region ReorderableList

		private static GUIStyle preButton;
		private static GUIStyle footerBackground;
		private static GUIStyle PreButton => preButton ?? (preButton = "RL FooterButton");
		private static GUIStyle FooterBackground => footerBackground ?? (footerBackground = "RL Footer");
		private static GUIContent iconToolbarPlusMore;
		public static GUIContent IconToolbarPlusMore => iconToolbarPlusMore ?? (iconToolbarPlusMore = EditorGUIUtility.TrIconContent("Toolbar Plus More", "Choose to add to list"));
		private static GUIContent iconToolbarPlus;
		public static GUIContent IconToolbarPlus => iconToolbarPlus ?? (iconToolbarPlus = EditorGUIUtility.TrIconContent("Toolbar Plus", "Add to list"));

		/// <summary>
		/// Draws an alternate add button if run after a reorderable list's DoLayoutList function
		/// </summary>
		/// <param name="lastRect">The position of the list</param>
		/// <param name="buttonRect">The position of this button</param>
		/// <param name="displayDropdownPlus">Whether or not the + should have a dropdown styling</param>
		/// <returns>Whether the button was pressed or not</returns>
		public static bool ReorderableListAddButton(out Rect lastRect, out Rect buttonRect, bool displayDropdownPlus = true)
		{
			lastRect = GUILayoutUtility.GetLastRect();
			buttonRect = new Rect(lastRect.x + (lastRect.width - 75), lastRect.yMax - 20, 33, 20);
			GUI.Label(buttonRect, GUIContent.none, FooterBackground);
			buttonRect.height -= 2;
			buttonRect.x += 2;
			buttonRect.width -= 4;
			return GUI.Button(buttonRect, displayDropdownPlus ? IconToolbarPlusMore : IconToolbarPlus, PreButton);
		}

		/// <summary>
		/// Draws a Clear button in the top right of a ReorderableList's header when run in the header callback.
		/// </summary>
		/// <param name="rect">The total header rect</param>
		/// <param name="property">The list property</param>
		public static void ReorderableListHeaderClearButton(Rect rect, SerializedProperty property)
		{
			rect.height = EditorGUIUtility.singleLineHeight;
			float intendedX = rect.xMax - 50;
			float x = Mathf.Max(rect.xMin, intendedX);
			float width = 56 + (x - intendedX);
			if (!GUI.Button(new Rect(x, rect.y, width, rect.height), "Clear", EditorStyles.toolbarButton))
				return;
			property.arraySize = 0;
			property.serializedObject.ApplyModifiedProperties();
		}

		#endregion

		#region Styles

		private static GUIStyle centeredMiniLabel;

		public static GUIStyle CenteredMiniLabel => centeredMiniLabel ?? (centeredMiniLabel = new GUIStyle(EditorStyles.miniLabel)
		{
			alignment = TextAnchor.MiddleCenter
		});

		private static GUIStyle centeredBoldMiniLabel;

		public static GUIStyle CenteredBoldMiniLabel => centeredBoldMiniLabel ?? (centeredBoldMiniLabel = new GUIStyle(EditorStyles.miniBoldLabel)
		{
			alignment = TextAnchor.MiddleCenter
		});

		private static GUIStyle centeredBoldLabel;

		public static GUIStyle CenteredBoldLabel => centeredBoldLabel ?? (centeredBoldLabel = new GUIStyle(EditorStyles.boldLabel)
		{
			alignment = TextAnchor.MiddleCenter
		});

		public static float HeightWithSpacing => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

		#endregion

		public class ZeroIndentScope : IDisposable
		{
			public readonly int PreviousIndent;

			public ZeroIndentScope()
			{
				PreviousIndent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;
			}

			public void Dispose() => EditorGUI.indentLevel = PreviousIndent;
		}
	}
}