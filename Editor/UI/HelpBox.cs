#if !UNITY_2020_1_OR_NEWER
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Vertx.Utilities.Editor
{
	public class HelpBox : VisualElement
	{
		public const string uSSClassName = "helpBox";
		public const string uSSLabelClassName = "helpBoxLabel";
		public const string infoUssClassName = "consoleInfo";
		public const string warningUssClassName = "consoleWarning";
		public const string errorUssClassName = "consoleError";

		public enum MessageType
		{
			None,
			Info,
			Warning,
			Error
		}

		// ReSharper disable once UnusedType.Global
		public new class UxmlFactory : UxmlFactory<HelpBox, UxmlTraits> { }

		public new class UxmlTraits : VisualElement.UxmlTraits
		{
			readonly UxmlStringAttributeDescription text = new UxmlStringAttributeDescription {name = "text"};
			readonly UxmlEnumAttributeDescription<MessageType> messageType = new UxmlEnumAttributeDescription<MessageType> {name = "messageType"};
			
			public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
			{
				get
				{
					yield break;
					/*yield return new UxmlChildElementDescription(typeof(Label));
					yield return new UxmlChildElementDescription(typeof(Image));*/
				}
			}

			public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
			{
				base.Init(ve, bag, cc);
				var helpBox = (HelpBox) ve;
				helpBox.AddIcon(messageType.GetValueFromBag(bag, cc));
				helpBox.AddLabel(text.GetValueFromBag(bag, cc));
			}
		}

		public HelpBox()
		{
			styleSheets.Add(StyleUtils.GetStyleSheet("HelpBox"));
			AddToClassList(uSSClassName);
		}

		public HelpBox(string labelText, MessageType messageType = MessageType.None) : this()
		{
			AddIcon(messageType);
			AddLabel(labelText);
		}

		private bool AddIcon(MessageType messageType)
		{
			if (!TryGetClassName(messageType, out var className))
				return false;
			AddIconWithClass(className);
			return true;
		}

		private static bool TryGetClassName(MessageType messageType, out string className)
		{
			switch (messageType)
			{
				case MessageType.None:
					className = null;
					return false;
				case MessageType.Info:
					className = infoUssClassName;
					return true;
				case MessageType.Warning:
					className = warningUssClassName;
					return true;
				case MessageType.Error:
					className = errorUssClassName;
					return true;
				default:
					throw new ArgumentOutOfRangeException(nameof(messageType), messageType, null);
			}
		}

		private void AddIconWithClass(string ussClass)
		{
			VisualElement image = new VisualElement();
			image.AddToClassList(ussClass);
			Add(image);
		}

		private bool AddLabel(string labelText)
		{
			if (string.IsNullOrEmpty(labelText))
				return false;
			Label l = new Label(labelText);
			l.AddToClassList(uSSLabelClassName);
			Add(l);
			return true;
		}
	}
}
#endif