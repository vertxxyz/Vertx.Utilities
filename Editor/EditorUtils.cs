using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Vertx.Utilities.Editor
{
	public static class EditorUtils
	{
		#region Assets

		public static Object LoadAssetOfType(Type type, string query = null)
		{
			if (!TryGetGUIDs(out var guids, type, query))
				return null;
			foreach (string guid in guids)
			{
				var asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
				if (asset != null && type.IsInstanceOfType(asset))
					return asset;
			}

			return null;
		}

		public static T LoadAssetOfType<T>(string query = null) where T : Object
		{
			if (!TryGetGUIDs(out var guids, typeof(T), query))
				return null;
			foreach (string guid in guids)
			{
				var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
				if (asset != null)
					return asset;
			}

			return null;
		}

		public static T[] LoadAssetsOfType<T>(string query = null) where T : Object
		{
			if (!TryGetGUIDs(out var guids, typeof(T), query))
				return Array.Empty<T>();

			List<T> values = new List<T>();
			foreach (string guid in guids)
			{
				var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
				if (asset != null)
					values.Add(asset);
			}

			return values.ToArray();
		}

		private static bool TryGetGUIDs(out string[] guids, Type type, string query = null)
		{
			guids = AssetDatabase.FindAssets(query == null ? $"t:{type.FullName}" : $"t:{type.FullName} {query}");
			if (guids.Length == 0)
			{
				guids = AssetDatabase.FindAssets(query == null ? $"t:{type.Name}" : $"t:{type.Name} {query}");
				if (guids.Length == 0)
					return false;
			}

			return true;
		}

		#endregion

		#region Folders

		public static void ShowFolderContents(int folderInstanceId, bool revealAndFrameInFolderTree)
		{
			Type tProjectBrowser = Type.GetType("UnityEditor.ProjectBrowser,UnityEditor");
			MethodInfo showContentsMethod =
				tProjectBrowser.GetMethod("ShowFolderContents", BindingFlags.NonPublic | BindingFlags.Instance);
			EditorWindow browser = EditorWindow.GetWindow(tProjectBrowser);
			if (browser != null)
				showContentsMethod.Invoke(browser, new object[] {folderInstanceId, revealAndFrameInFolderTree});
		}

		public static int GetMainAssetInstanceID(string path)
		{
			object idObject = typeof(AssetDatabase).GetMethod("GetMainAssetInstanceID", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, new object[] {path});
			if (idObject != null) return (int) idObject;
			return -1;
		}

		public static void ShowFolder(DefaultAsset o)
		{
			if (o == null)
				return;

			string path = AssetDatabase.GetAssetPath(o);
			if (Path.GetFileName(path).Contains("."))
				return; //DefaultAsset is a file.
			ShowFolderContents(
				GetMainAssetInstanceID(AssetDatabase.GUIDToAssetPath(AssetDatabase.AssetPathToGUID(path))), true
			);
			EditorWindow.GetWindow(Type.GetType("UnityEditor.ProjectBrowser,UnityEditor")).Repaint();
		}
		
		public static string GetCurrentlyFocusedProjectFolder()
		{
			foreach (var obj in Selection.GetFiltered<Object>(SelectionMode.Assets))
			{
				var path = AssetDatabase.GetAssetPath(obj);
				if (string.IsNullOrEmpty(path))
					continue;
				if (Directory.Exists(path))
					return path;
				if (File.Exists(path))
					return Path.GetDirectoryName(path);
			}
			return "Assets";
		}

		#endregion

		#region Editor Extensions

		/// <summary>
		/// Returns instances of the types inherited from Type T
		/// </summary>
		/// <typeparam name="T">The type to query and return instances for.</typeparam>
		/// <returns>List of instances inherited from Type T</returns>
		public static List<T> GetEditorExtensionsOfType<T>()
		{
			IEnumerable<Type> derivedTypes = TypeCache.GetTypesDerivedFrom<T>();
			return derivedTypes.Select(t => (T) Activator.CreateInstance(t)).ToList();
		}

		/// <summary>
		/// Returns instances of the types inherited from Type T, casted to type TConverted
		/// </summary>
		/// <param name="type">Type query for inheritance</param>
		/// <typeparam name="TConverted">The type to cast new instances to.</typeparam>
		/// <returns>List of instances inherited from Type T</returns>
		public static List<TConverted> GetEditorExtensionsOfType<TConverted>(Type type)
		{
			IEnumerable<Type> derivedTypes = TypeCache.GetTypesDerivedFrom(type);
			return derivedTypes.Select(t => (TConverted) Activator.CreateInstance(t)).ToList();
		}

		#endregion

		#region Property Extensions

		public static int GetIndexFromArrayProperty(SerializedProperty property)
		{
			string propertyPath = property.propertyPath;
			int lastIndexOf = propertyPath.LastIndexOf('[');
			if(lastIndexOf < 0)
				throw GetException();
			lastIndexOf++;
			if(lastIndexOf == propertyPath.Length)
				throw GetException();
			if (int.TryParse(propertyPath.Substring(lastIndexOf, propertyPath.Length - lastIndexOf - 1), out var value))
				return value;
			throw GetException();
			Exception GetException () => new ArgumentException($"Property with path: \"{propertyPath}\" is not an array property.");
		}

		public static void LogAllProperties(this SerializedObject serializedObject)
		{
			StringBuilder stringBuilder = new StringBuilder(serializedObject.targetObject.ToString());
			stringBuilder.AppendLine(":");
			SerializedProperty rootProp = serializedObject.GetIterator();

			rootProp.Next(true);
			AppendProperty(stringBuilder, rootProp, null);

			Debug.Log(stringBuilder);
		}

		private const int safetySerializationDepth = 10;

		private static void AppendProperty
		(
			StringBuilder stringBuilder,
			SerializedProperty property,
			SerializedProperty endProperty,
			int substringPosition = 0,
			int depth = 0)
		{
			property = property.Copy();
			while (true)
			{
				if (depth > safetySerializationDepth) return;

				bool enterChildren = true;
				if (property.isArray)
				{
					//stringBuilder.AppendLine($"Is Array {property.propertyType} - {property.propertyPath}");
					switch (property.arrayElementType)
					{
						case "char":
						case "Vector3Curve":
						case "QuaternionCurve":
							LogPropertyWithSubstringSafety();
							break;
						default:
							LogPropertyWithSubstringSafety(true);
							depth++;
							AppendArrayProperty(property);
							depth--;
							break;
					}
					enterChildren = false;
				} else if (property.hasChildren) {
					//Do nothing
				}
				else
				{
					LogPropertyWithSubstringSafety();
				}
				
				if (EndProperty(enterChildren))
					break;
			}

			bool EndProperty(bool enterChildren) => !property.Next(enterChildren) || endProperty != null && SerializedProperty.EqualContents(property, endProperty);

			void AppendArrayProperty(SerializedProperty arrayProp)
			{
				if (depth > safetySerializationDepth) return;
			
				if (arrayProp.arraySize == 0)
					return;

				SerializedProperty temp = arrayProp.Copy();
				temp = temp.GetArrayElementAtIndex(0);
				SerializedProperty end;
				if (arrayProp.arraySize > 1)
					end = arrayProp.GetArrayElementAtIndex(1).Copy();
				else
				{
					end = temp.Copy();
					end.Next(false);
				}
				AppendProperty(stringBuilder, temp, end, temp.propertyPath.Length, depth);
			}

			void LogPropertyWithSubstringSafety(bool isArray = false)
			{
				stringBuilder.Append(' ', depth * 4);
				if (substringPosition >= property.propertyPath.Length)
				{
					//Safety/fallback
					stringBuilder.Append(property.propertyPath);
				}
				else
				{
					stringBuilder.Append(property.propertyPath.Substring(substringPosition));
				}

				if (isArray)
					stringBuilder.Append("[]");

				stringBuilder.Append(" (");
				stringBuilder.Append(property.type);
				stringBuilder.AppendLine(")");
			}
		}

		private static readonly Dictionary<string, (Type, FieldInfo)> baseTypeLookup = new Dictionary<string, (Type, FieldInfo)>();

		/// <summary>
		/// Gets the backing object from a serialized property.
		/// </summary>
		/// <param name="prop">The property query</param>
		/// <param name="parent">The parent of the returned object</param>
		/// <param name="fieldInfo">The fieldInfo associated with the property</param>
		/// <returns>The object associated with the SerializedProperty <para>prop</para></returns>
		public static object GetObjectFromProperty(SerializedProperty prop, out object parent, out FieldInfo fieldInfo)
		{
			// Separate the steps it takes to get to this property
			string p = Regex.Replace(prop.propertyPath, @".Array.data", string.Empty);
			string[] separatedPaths = p.Split('.');

			// Go down to the root of this serialized property
			object @object = prop.serializedObject.targetObject;
			parent = null;
			fieldInfo = null;
			Type type = prop.serializedObject.targetObject.GetType();
			// Walk down the path to get the target type
			foreach (var pathIterator in separatedPaths)
			{
				int index = -1;
				string path = pathIterator;
				if (path.EndsWith("]"))
				{
					int startIndex = path.IndexOf('[') + 1;
					int length = path.Length - startIndex - 1;
					index = int.Parse(path.Substring(startIndex, length));
					path = path.Substring(0, startIndex - 1);
				}

				fieldInfo = type.GetField(path, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				//Walk up the type tree to find the field in question
				if (fieldInfo == null)
				{
					do
					{
						type = type.BaseType;
						fieldInfo = type.GetField(path, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
					} while (fieldInfo == null && type != typeof(object));
				}

				if (fieldInfo == null)
					throw new FieldAccessException($"{type}.{prop.propertyPath} does not have a matching FieldInfo. This is likely because it is a native property.");

				type = fieldInfo.FieldType;
				parent = @object;
				@object = fieldInfo.GetValue(@object);

				if (type.IsArray)
				{
					if (index >= 0)
					{
						parent = @object;
						@object = ((Array) @object).GetValue(index);
					}
					else if (prop.propertyPath.EndsWith("Array.size"))
					{
						if (@object == null)
							return 0;
						parent = @object;
						@object = ((Array) @object).Length;
						return @object;
					}
					else
						return @object;

					type = @object?.GetType();
				}
				else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
				{
					if (index >= 0)
					{
						parent = @object;
						@object = ((IList) @object)[index];
					}
					else if (prop.propertyPath.EndsWith("Array.size"))
					{
						if (@object == null)
							return 0;
						parent = @object;
						@object = ((IList) @object).Count;
						return @object;
					}
					else
						return @object;

					type = @object?.GetType();
				}
			}

			return @object;
		}

		#endregion

		#region Scene

		public class BuildSceneScope : IEnumerator<Scene>, IEnumerable<Scene>
		{
			private readonly SceneSetup[] sceneManagerSetup;

			public BuildSceneScope()
			{
				sceneManagerSetup = EditorSceneManager.GetSceneManagerSetup();
				buildSceneCount = SceneManager.sceneCountInBuildSettings;
				currentScene = default;
				progressIncrement = 1 / (float)buildSceneCount;
			}

			public void Dispose()
			{
				EditorUtility.ClearProgressBar();
				if (sceneManagerSetup != null && sceneManagerSetup.Length > 0)
					EditorSceneManager.RestoreSceneManagerSetup(sceneManagerSetup);
			}

			private readonly float progressIncrement;

			public void DisplayProgressBar(string title, string info, float localProgress) 
				=> EditorUtility.DisplayProgressBar(title, info, (buildIndex + localProgress * progressIncrement) / buildSceneCount);

			private readonly int buildSceneCount;
			private int buildIndex = -1;
			private Scene currentScene;

			public bool MoveNext()
			{
				//Avoids going beyond the end of the collection.
				if (++buildIndex >= buildSceneCount)
					return false;

				string path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
				EditorSceneManager.OpenScene(path, buildIndex == 0 ? OpenSceneMode.Single : OpenSceneMode.Additive);
				currentScene = SceneManager.GetSceneByBuildIndex(buildIndex);
				return true;
			}

			public void Reset() => buildIndex = -1;

			public Scene Current => currentScene;

			object IEnumerator.Current => Current;
			public IEnumerator<Scene> GetEnumerator() => this;

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
		}

		public static IEnumerable<T> GetAllComponentsInScene<T>(Scene scene) where T : Component
		{
			GameObject[] roots = scene.GetRootGameObjects();
			foreach (GameObject root in roots)
			{
				Transform @base = root.transform;
				foreach (T component in OperateOnTransform(@base))
					yield return component;
			}

			IEnumerable<T> OperateOnTransform(Transform @base)
			{
				//Get components
				T[] components = @base.GetComponents<T>();
				foreach (T component in components)
				{
					if(component != null)
						yield return component;
				}

				foreach (Transform child in @base)
				{
					foreach (var component in OperateOnTransform(child))
						yield return component;
				}
			}
		}

		public static IEnumerable<GameObject> GetAllGameObjectsInScene(Scene scene)
		{
			GameObject[] roots = scene.GetRootGameObjects();
			foreach (GameObject root in roots)
			{
				Transform @base = root.transform;
				foreach (GameObject gameObject in GetGameObjectsIncludingRoot(@base))
					yield return gameObject;
			}
		}
		
		public static IEnumerable<GameObject> GetGameObjectsIncludingRoot(Transform @base)
		{
			//Get gameObject
			yield return @base.gameObject;
			foreach (Transform child in @base)
			{
				foreach (var gameObject in GetGameObjectsIncludingRoot(child))
					yield return gameObject;
			}
		}

		#endregion

		#region Logging Extensions

		/// <summary>
		/// Returns an appropriate full path to the object. This includes the scene if relevant.
		/// </summary>
		/// <param name="object">The Object to get a path to</param>
		/// <returns>Path to the Object</returns>
		public static string GetPathForObject(Object @object)
		{
			bool persistent = EditorUtility.IsPersistent(@object);
			string path = persistent ? $"{AssetDatabase.GetAssetPath(@object)}/" : string.Empty;
			switch (@object)
			{
				case Component component:
					{
						//The component already includes the base child in its ToString function, so we can use the parent.
						Transform transform = component.transform.parent;
						string tPath;
						if (transform != null)
						{
							tPath = AnimationUtility.CalculateTransformPath(transform, null);
							if (persistent)
							{
								//For prefabs, the path already includes the root. So we can remove it from the transform path.
								int indexOf = tPath.IndexOf('/');
								tPath = indexOf < 0 ? null : tPath.Substring(indexOf + 1);
							}
						}
						else
							tPath = null;

						var scene = component.gameObject.scene;
						if (scene.IsValid())
							path += $"({scene.path}) ";
						path += string.IsNullOrEmpty(tPath) ? component.ToString() : $"{tPath}/{component}";
					}
					break;
				case GameObject gameObject:
					{
						//The gameObject already includes the base child in its ToString function, so we can use the parent.
						Transform transform = gameObject.transform.parent;
						string tPath;
						if (transform != null)
						{
							tPath = AnimationUtility.CalculateTransformPath(transform, null);
							if (persistent)
							{
								//For prefabs, the path already includes the root. So we can remove it from the transform path.
								int indexOf = tPath.IndexOf('/');
								tPath = indexOf < 0 ? null : tPath.Substring(indexOf + 1);
							}
						}
						else
							tPath = null;

						var scene = gameObject.scene;
						if (scene.IsValid())
							path += $"({scene.path}) ";
						path += string.IsNullOrEmpty(tPath) ? gameObject.ToString() : $"{tPath}/{gameObject}";
					}
					break;
				default:
					path += @object.ToString();
					break;
			}

			return path;
		}

		#endregion
	}
}