using UnityEditor;
using UnityEngine.UIElements;

namespace Vertx.Utilities.Editor
{
	public static class StyleUtils
	{
		#region Vertx Style

		private static StyleSheet vertxStyleSheetShared;
		private static StyleSheet VertxStyleSheetShared => vertxStyleSheetShared == null ? vertxStyleSheetShared = GetStyleSheet("VertxStyleShared") : vertxStyleSheetShared;
		
		private static StyleSheet vertxStyleSheetLight;
		private static StyleSheet VertxStyleSheetLight => vertxStyleSheetLight == null ? vertxStyleSheetLight = GetStyleSheet("VertxStyleLight") : vertxStyleSheetLight;
		
		private static StyleSheet vertxStyleSheetDark;
		private static StyleSheet VertxStyleSheetDark => vertxStyleSheetDark == null ? vertxStyleSheetDark = GetStyleSheet("VertxStyle") : vertxStyleSheetDark;

		public static void AddVertxStyleSheets(VisualElement visualElement)
		{
			StyleSheet styleSheetShared = VertxStyleSheetShared;
			visualElement.styleSheets.Add(styleSheetShared);
			StyleSheet styleSheet = EditorGUIUtility.isProSkin ? VertxStyleSheetDark : VertxStyleSheetLight;
			visualElement.styleSheets.Add(styleSheet);
		}

		#endregion

		public static StyleSheet GetStyleSheet(string name)
		{
			string[] guids = AssetDatabase.FindAssets($"t:{nameof(StyleSheet)} {name}");
			if (guids.Length == 0)
				return null;
			var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
			return sheet;
		}

		public static VisualTreeAsset GetUXML(string name)
		{
			string[] guids = AssetDatabase.FindAssets($"t:{nameof(VisualTreeAsset)} {name}");
			if (guids.Length == 0)
				return null;
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
			return uxml;
		}

		public static (StyleSheet, VisualTreeAsset) GetStyleSheetAndUXML(string name)
		{
			string[] guids = AssetDatabase.FindAssets(name);
			if (guids.Length == 0)
				return (null, null);
			(StyleSheet, VisualTreeAsset) values = default;
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (path.EndsWith($"/{name}.uss"))
					values.Item1 = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
				else if (path.EndsWith($"/{name}.uxml"))
					values.Item2 = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
			}

			return values;
		}
	}
}