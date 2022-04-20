using System;
using System.Collections.Generic;
using UnityEditor;
// ReSharper disable once RedundantUsingDirective
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Vertx.Utilities.Editor
{
	/// <summary>
	/// A UIToolkit GenericMenu dropdown button.
	/// </summary>
	public class DropdownButton : BaseField<string>
	{
		private readonly Func<IEnumerable<string>> populateDropdown;
		private readonly Func<string, bool> onSelect;
		private readonly TextElement textElement;

		private VisualElement internalVisualInput;
		private VisualElement VisualInput => internalVisualInput = internalVisualInput ?? this.Q<VisualElement>(null, inputUssClassName);

		public DropdownButton(string displayValue, Func<IEnumerable<string>> populateDropdown, Func<string, bool> onSelect)
			: this(null, displayValue, populateDropdown, onSelect) { }

		public DropdownButton(string label, string displayValue, Func<IEnumerable<string>> populateDropdown, Func<string, bool> onSelect) : base(label, null)
		{
			this.populateDropdown = populateDropdown;
			this.onSelect = onSelect;
			AddToClassList(BasePopupField<string, string>.ussClassName);
			labelElement.AddToClassList(BasePopupField<string, string>.labelUssClassName);
			TextElement popupTextElement = new TextElement { pickingMode = PickingMode.Ignore };
			textElement = popupTextElement;
			textElement.AddToClassList(BasePopupField<string, string>.textUssClassName);
			VisualInput.AddToClassList(BasePopupField<string, string>.inputUssClassName);
			textElement.text = displayValue;
			VisualInput.Add(textElement);
			var arrowElement = new VisualElement();
			arrowElement.AddToClassList(BasePopupField<string, string>.arrowUssClassName);
			arrowElement.pickingMode = PickingMode.Ignore;
			VisualInput.Add(arrowElement);
			RegisterCallback<PointerDownEvent>(ClickEvent);
		}

		private void ClickEvent(PointerDownEvent evt)
		{
			ShowMenu();
			evt.StopPropagation();
		}

		private void ShowMenu()
		{
			GenericMenu menu = new GenericMenu();
			foreach (var val in populateDropdown.Invoke())
			{
				menu.AddItem(new GUIContent(val), false, () =>
				{
					if (onSelect.Invoke(val))
						textElement.text = val;
				});
			}

			menu.DropDown(worldBound);
		}
	}
}