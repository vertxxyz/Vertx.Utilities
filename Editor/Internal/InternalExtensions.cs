using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable ArrangeObjectCreationWhenTypeEvident

namespace Vertx.Utilities.Editor.Internal
{
	/// <summary>
	/// Do not use this class. Access Vertx.Utilities.Editor.EditorUtils instead.
	/// </summary>
	public static partial class InternalExtensions
	{
		public readonly struct GameViewSizeInternal
		{
			public enum Type
			{
				AspectRatio,
				FixedResolution
			}

			private readonly string _baseText;
			private readonly Type _sizeType;
			private readonly int _width;
			private readonly int _height;

			public GameViewSizeInternal(string baseText, Type sizeType, int width, int height)
			{
				_baseText = baseText;
				_sizeType = sizeType;
				_width = width;
				_height = height;
			}

			internal GameViewSize ToGameViewSize() =>
				new GameViewSize(
					_sizeType == Type.AspectRatio ? GameViewSizeType.AspectRatio : GameViewSizeType.FixedResolution,
					_width,
					_height,
					_baseText
				);
		}

		private static IEnumerable<GameViewSize> GetCustomGameViewSizesForCurrentGroup()
		{
			GameViewSizeGroup currentGroup = GameViewSizes.instance.currentGroup;
			if (currentGroup == null)
				yield break;
			int totalCount = currentGroup.GetTotalCount();
			int builtInCount = currentGroup.GetBuiltinCount();

			for (int i = builtInCount; i < totalCount; i++)
				yield return currentGroup.GetGameViewSize(i);
		}

		public static bool TryAddCustomGameViewSizeForCurrentGroup(GameViewSizeInternal size)
		{
			var gameViewSize = size.ToGameViewSize();
			foreach (GameViewSize sizeInternal in GetCustomGameViewSizesForCurrentGroup())
			{
				if (ValueEquals(gameViewSize, sizeInternal))
					return false;
			}

			GameViewSizeGroup currentGroup = GameViewSizes.instance.currentGroup;
			if (currentGroup == null)
				return false;
			currentGroup.AddCustomSize(gameViewSize);
			return true;

			bool ValueEquals(GameViewSize a, GameViewSize b) =>
				a.baseText == b.baseText &&
				a.sizeType == b.sizeType &&
				a.width == b.width &&
				a.height == b.height;
		}

		public static void ShowFolderContents(int folderInstanceId)
		{
			MethodInfo showContentsMethod =
				typeof(ProjectBrowser).GetMethod("ShowFolderContents", BindingFlags.NonPublic | BindingFlags.Instance);
			ProjectBrowser browser = GetProjectBrowserWindowInternal();
			if (browser != null)
				showContentsMethod.Invoke(browser, new object[] { folderInstanceId, true });
		}

		public static void SetProjectBrowserSearch(string search) => EditorWindow.GetWindow<ProjectBrowser>().SetSearch(search);

		public static EditorWindow GetProjectBrowserWindow(bool forceOpen = false) => GetProjectBrowserWindowInternal(forceOpen);

		private static ProjectBrowser GetProjectBrowserWindowInternal(bool forceOpen = false)
		{
			ProjectBrowser projectBrowser = EditorWindow.GetWindow<ProjectBrowser>();
			if (projectBrowser != null)
				return projectBrowser;
			if (!forceOpen)
				return null;
			EditorApplication.ExecuteMenuItem("Window/General/Project");
			return EditorWindow.GetWindow<ProjectBrowser>();
		}

		public static EditorWindow GetSceneViewHierarchyWindow(bool forceOpen = false) => GetSceneViewHierarchyWindowInternal(forceOpen);

		private static SceneHierarchyWindow GetSceneViewHierarchyWindowInternal(bool forceOpen = false)
		{
			SceneHierarchyWindow hierarchyWindow = EditorWindow.GetWindow<SceneHierarchyWindow>();
			if (hierarchyWindow != null)
				return hierarchyWindow;
			if (!forceOpen)
				return null;
			EditorApplication.ExecuteMenuItem("Window/General/Hierarchy");
			return EditorWindow.GetWindow<SceneHierarchyWindow>();
		}

		public static int GetMainAssetInstanceID(string path) => AssetDatabase.GetMainAssetInstanceID(path);

		public static void SetSceneViewHierarchySearch(string search)
		{
			SceneHierarchyWindow window = GetSceneViewHierarchyWindowInternal();
			if (window == null) return;
			window.SetSearchFilter(search, SearchableEditorWindow.SearchMode.All, false);
		}

		public static FieldInfo GetFieldInfoFromProperty(SerializedProperty property, out Type type) => ScriptAttributeUtility.GetFieldInfoFromProperty(property, out type);

		public static bool HasCustomPropertyDrawer(SerializedProperty property) => ScriptAttributeUtility.GetHandler(property).hasPropertyDrawer;

#if !UNITY_2022_2_OR_NEWER
		public static Gradient GetGradientValue(SerializedProperty property) => property.gradientValue;
#endif

		public static void AddScrollViewToSelectedObject() =>
			Type.GetType("UnityEditor.UI.MenuOptions,UnityEditor.UI")
				.GetMethod("AddScrollView", BindingFlags.Static | BindingFlags.Public)
				.Invoke(null, new object[] { new MenuCommand(Selection.activeObject) });
	}
}