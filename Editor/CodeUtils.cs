using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vertx.Utilities.Editor
{
	public static class CodeUtils
	{
		public static bool GetAndReplaceTextAtFilePath(string templatePath, Dictionary<string, string> mapping, out string text)
		{
			if (!File.Exists(templatePath))
			{
				Debug.LogError($"File no longer exists at {templatePath}");
				text = null;
				return false;
			}

			string content = File.ReadAllText(templatePath);
			foreach (KeyValuePair<string, string> child in mapping)
				content = content.Replace(child.Key, child.Value);
			text = content;
			return true;
		} 
		
		public static bool SaveAndWriteFileDialog(string fileName, string content, string extension = "cs")
		{
			string path = EditorUtility.SaveFilePanel("Save Remapped Template", Application.dataPath, fileName, extension);
			if (string.IsNullOrEmpty(path))
				return false;

			File.WriteAllText(path, content);
			AssetDatabase.Refresh();
			return true;
		}
	}
}