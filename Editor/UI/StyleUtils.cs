using UnityEditor;
using UnityEngine.UIElements;

namespace Vertx.Utilities.Editor
{
	public static class StyleUtils
	{
		/// <summary>
		/// It's easiest to serialize a StyleSheet into the MonoScript when using this for an EditorWindow.
		/// This method is helpful when that case is not available.
		/// </summary>
		/// <param name="name">The name of the style sheet without extension.</param>
		public static StyleSheet GetStyleSheet(string name)
		{
			string[] guids = AssetDatabase.FindAssets($"t:{nameof(StyleSheet)} {name}");
			if (guids.Length == 0)
				return null;
			var sheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(guids[0]));
			return sheet;
		}

		/// <summary>
		/// It's easiest to serialize a VisualTreeAsset into the MonoScript when using this for an EditorWindow.
		/// This method is helpful when that case is not available.
		/// </summary>
		/// <param name="name">The name of the uxml without extension.</param>
		public static VisualTreeAsset GetUXML(string name)
		{
			string[] guids = AssetDatabase.FindAssets($"t:{nameof(VisualTreeAsset)} {name}");
			if (guids.Length == 0)
				return null;
			var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
			return uxml;
		}

		/// <summary>
		/// It's easiest to serialize a StyleSheet and VisualTreeAsset into the MonoScript when using this for an EditorWindow.
		/// This method is helpful when that case is not available.
		/// </summary>
		/// <param name="name">The name of the style sheet and uxml without extension.</param>
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