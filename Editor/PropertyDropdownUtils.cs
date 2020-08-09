using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Vertx.Utilities.Editor
{
	public interface IPropertyDropdownItem
	{
		string Name { get; }
		string Path { get; }
	}

	public static class PropertyDropdownUtils
	{
		private class PropertyDropdownItem<T>
			where T : class, IPropertyDropdownItem
		{
			public string Name { get; }
			public T Item { get; }

			private Dictionary<string, PropertyDropdownItem<T>> children;
			public Dictionary<string, PropertyDropdownItem<T>> Children => children;

			public PropertyDropdownItem(string name)
			{
				Name = name;
				Item = null;
			}

			public PropertyDropdownItem(T item)
			{
				Name = item.Name;
				Item = item;
			}

			public PropertyDropdownItem<T> AddChild(string localPath, T child)
			{
				if (children == null)
					children = new Dictionary<string, PropertyDropdownItem<T>>();
				
				//Add as a child if we have reached the bottom of the path.
				if (string.IsNullOrEmpty(localPath))
					return AddChildDirectly(child) ? this : null;
				
				int indexOfSeparator = localPath.IndexOfAny(separators);

				//----------------------------------------------------------

				string thisPathName;
				string nextPathName;
				if (indexOfSeparator == -1 || indexOfSeparator == localPath.Length - 1)
				{
					thisPathName = localPath;
					nextPathName = null;
				}
				else
				{
					thisPathName = localPath.Substring(0, indexOfSeparator);
					nextPathName = localPath.Substring(indexOfSeparator + 1);
				}

				if (!children.TryGetValue(thisPathName, out var nextChild))
					children.Add(thisPathName, nextChild = new PropertyDropdownItem<T>(thisPathName));
				return nextChild.AddChild(nextPathName, child);
			}

			public bool AddChildDirectly(T child)
			{
				if (children.ContainsKey(child.Name))
				{
					Debug.LogError($"{child} key already exists in children ({children[child.Name]}).");
					return false;
				}

				children.Add(child.Name, new PropertyDropdownItem<T>(child));
				return true;
			}
		}

		private static readonly char[] separators = {'/', '\\'};

		public static (Dictionary<int, T>, AdvancedDropdownItem) GetStructure<T>(IEnumerable<T> items, string rootName, Func<T, bool> enabledFunc = null)
			where T : class, IPropertyDropdownItem
		{
			PropertyDropdownItem<T> rootItem = GenerateItems(items, rootName);
			AdvancedDropdownItem root = ConvertToItems(rootItem, out var lookup, enabledFunc);
			return (lookup, root);
		}

		private static PropertyDropdownItem<T> GenerateItems<T>(IEnumerable<T> items, string rootName)
			where T : class, IPropertyDropdownItem
		{
			//Collect all the items into their respective paths.
			Dictionary<string, List<T>> pathsToItems = new Dictionary<string, List<T>>();

			foreach (T item in items)
			{
				if (!pathsToItems.TryGetValue(item.Path, out List<T> list))
				{
					list = new List<T>();
					pathsToItems.Add(item.Path, list);
				}

				list.Add(item);
			}

			PropertyDropdownItem<T> root = new PropertyDropdownItem<T>(rootName);
			foreach (var pathToItems in pathsToItems)
			{
				PropertyDropdownItem<T> current = null;
				for (int i = 0; i < pathToItems.Value.Count; i++)
				{
					if (i == 0 || current == null)
						current = root.AddChild(pathToItems.Key, pathToItems.Value[i]);
					else
						current.AddChildDirectly(pathToItems.Value[i]);
				}
			}

			return root;
		}

		private static AdvancedDropdownItem ConvertToItems<T>(PropertyDropdownItem<T> rootItem, out Dictionary<int, T> lookup, Func<T, bool> enabledFunc)
			where T : class, IPropertyDropdownItem
		{
			lookup = new Dictionary<int, T>();
			AdvancedDropdownItem root = new AdvancedDropdownItem(rootItem.Name);

			AddChildren(root, rootItem, lookup);

			void AddChildren(AdvancedDropdownItem toTarget, PropertyDropdownItem<T> toGather, Dictionary<int, T> localLookup)
			{
				foreach (KeyValuePair<string, PropertyDropdownItem<T>> children in toGather.Children)
				{
					PropertyDropdownItem<T> item = children.Value;
					if (TryAddEndChild(item))
						continue;
					AdvancedDropdownItem child;
					toTarget.AddChild(child = new AdvancedDropdownItem(item.Name));
					AddChildren(child, item, localLookup);
				}
				
				bool TryAddEndChild(PropertyDropdownItem<T> item)
				{
					if (item.Children != null)
						return false;
					if (item.Item == null)
						return false;
					AdvancedDropdownItem child;
					toTarget.AddChild(child = new AdvancedDropdownItem(item.Name)
					{
						id = $"{item.Item.Path}/{item.Item.Name}".GetHashCode(),
						enabled = enabledFunc?.Invoke(item.Item) ?? true
					});
					localLookup.Add(child.id, item.Item);
					return true;
				}
			}

			return root;
		}
	}
}