using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Vertx.Utilities
{
	public class AdvancedDropdownOfSubtypes<T> : AdvancedDropdownOfSubtypes
	{
		public AdvancedDropdownOfSubtypes(AdvancedDropdownState state, Action<Type> onTypeSelectedCallback) : base(state, onTypeSelectedCallback, typeof(T)) { }
	}

	public class AdvancedDropdownOfSubtypes : AdvancedDropdown
	{
		private readonly Type _rootType;
		private readonly Func<Type, bool> _performConstraint;
		private readonly Action<Type> _onTypeSelected;

		private class TypeDropdownItem : AdvancedDropdownItem
		{
			public Type Type;

			public TypeDropdownItem(Type type) : base(ObjectNames.NicifyVariableName(type.Name)) => Type = type;
		}

		public AdvancedDropdownOfSubtypes(
			AdvancedDropdownState state,
			Action<Type> onTypeSelectedCallback,
			Type rootType,
			Func<Type, bool> performConstraint = null
		) : base(state)
		{
			Vector2 customMinimumSize = minimumSize;
			customMinimumSize.y = 250;
			minimumSize = customMinimumSize;
			_onTypeSelected = onTypeSelectedCallback;
			_rootType = rootType;
			_performConstraint = performConstraint;
		}


		protected override AdvancedDropdownItem BuildRoot()
		{
			AdvancedDropdownItem root = new AdvancedDropdownItem("Types");

			// Only constrain when required.
			List<Type> allTypes = _performConstraint != null
				? TypeCache.GetTypesDerivedFrom(_rootType).Where(_performConstraint).ToList()
				: TypeCache.GetTypesDerivedFrom(_rootType).ToList();
			
			List<string> allNameSpaces = allTypes.Select(t => ToNamespace(t.Namespace)).Distinct().ToList();
			Dictionary<Type, TypeDropdownItem> typesToItems = new Dictionary<Type, TypeDropdownItem>();
			Dictionary<string, AdvancedDropdownItem> namespaceToRootItem = new Dictionary<string, AdvancedDropdownItem>();

			// Add namespace roots as required.
			if (allNameSpaces.Count == 1)
				namespaceToRootItem.Add(allNameSpaces[0], root);
			else
			{
				allNameSpaces.Sort(StringComparer.OrdinalIgnoreCase);
				foreach (string nameSpace in allNameSpaces)
				{
					AdvancedDropdownItem namespaceItem = new AdvancedDropdownItem(nameSpace);
					root.AddChild(namespaceItem);
					namespaceToRootItem.Add(nameSpace, namespaceItem);
				}
			}

			foreach (Type type in allTypes)
				Append(typesToItems, namespaceToRootItem[ToNamespace(type.Namespace)], type);

			return root;
		}

		private static string ToNamespace(string value) => string.IsNullOrEmpty(value) ? "Root Namespace" : value;

		private void Append(Dictionary<Type, TypeDropdownItem> typesToItems, AdvancedDropdownItem root, Type type)
		{
			TypeDropdownItem item = null;
			do
			{
				var newItem = new TypeDropdownItem(type);
				if (item != null)
					newItem.AddChild(item);
				
				// If this item has already been added to the list it means another subtype has created its tree. Just add this item as a child.
				if (typesToItems.TryGetValue(type, out TypeDropdownItem me))
				{
					me.AddChild(newItem);
					me.Type = null;
					return;
				}

				typesToItems.Add(type, item = newItem);
				type = type.BaseType;
				if (type == null || type == _rootType)
					break;
				if (!typesToItems.TryGetValue(type, out TypeDropdownItem parent))
					continue;
				// There is already a type in the hierarchy that represents this item.
				if (parent.Type != null)
				{
					// Move the parent to become a child
					// (nullifying its type is our way of defining it as moved,
					// AdvancedDropdown item helpfully provides no info, and I don't want to allocate more.)
					parent.AddChild(new TypeDropdownItem(parent.Type));
					parent.Type = null;
				}

				parent.AddChild(item);
				return;
			} while (true);

			root.AddChild(item);
		}

		protected override void ItemSelected(AdvancedDropdownItem item)
		{
			if (item is TypeDropdownItem typeDropdownItem)
				_onTypeSelected(typeDropdownItem.Type);
		}
	}
}