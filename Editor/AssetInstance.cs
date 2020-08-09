using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Vertx.Utilities.Editor
{
	/// <summary>
	/// A Generic class for creating singleton assets.
	/// </summary>
	/// <typeparam name="T">The Type of the class you wish to make a Singleton for (likely the same class you're extending with).</typeparam>
	public abstract class AssetInstance<T> : ScriptableObject where T : AssetInstance<T>
	{
		public enum SearchFilter
		{
			All,
			Assets,
			Packages
		}

		private static T _Instance;

		/// <summary>
		/// Singleton asset instance of the provided type T.
		/// </summary>
		public static T Instance
		{
			get
			{
				if (_Instance != null)
					return _Instance;

				//Load the T instance
				if ((_Instance = EditorUtils.LoadAssetOfType<T>()) != null)
					return _Instance;
				//or create it when not found
				string typeName = typeof(T).Name;
				Debug.Log(typeName + " Asset was initialised, its location is not fixed.");
				T instance = CreateInstance<T>();
				_Instance = CreateInstanceInProject(typeName, instance.ResourcesLocation, instance);
				string niceName = _Instance.NicifiedTypeName;
				if (!string.IsNullOrEmpty(niceName))
					AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(_Instance), $"{niceName}.asset");
				return _Instance;
			}
		}

		static string[] GetSearchDirs(SearchFilter searchAssets)
		{
			string[] searchDirs;
			switch (searchAssets)
			{
				case SearchFilter.All:
					searchDirs = new[] {"Assets", "Packages"};
					break;
				case SearchFilter.Assets:
					searchDirs = new[] {"Assets"};
					break;
				case SearchFilter.Packages:
					searchDirs = new[] {"Packages"};
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(searchAssets), searchAssets, null);
			}

			return searchDirs;
		}

		/// <summary>
		/// Creates a ScriptableObject Asset in the specified location
		/// </summary>
		/// <param name="fileName">the file name for the Asset</param>
		/// <param name="folder">the folder to create the asset in</param>
		/// <param name="instance">Optional already-created instance of the asset (it will be saved to the project folder)</param>
		/// <typeparam name="T">The Type of asset</typeparam>
		/// <returns>the newly created Asset instance</returns>
		public static T CreateInstanceInProject(string fileName, string folder, T instance = null)
		{
			if (!fileName.Contains("."))
				fileName += ".asset";
			if (!Directory.Exists(folder))
				AssetDatabase.CreateFolder(Path.GetDirectoryName(folder), Path.GetFileName(folder));

			string path = $"{folder}/{fileName}";
			if (instance == null)
				instance = CreateInstance<T>();
			AssetDatabase.CreateAsset(instance, path);
			AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
			return instance;
		}

		/// <summary>
		/// The name used when creating the asset instance.
		/// </summary>
		protected virtual string NicifiedTypeName => null;
		/// <summary>
		/// The location where the asset instance is created.
		/// </summary>
		protected virtual string ResourcesLocation => "Assets";
	}
}