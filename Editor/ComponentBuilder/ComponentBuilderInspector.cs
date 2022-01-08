using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CSharp;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Vertx.Utilities.ComponentBuilder;
using static Vertx.Utilities.Editor.CodeUtils;

namespace Vertx.Utilities.Editor
{
	[CustomEditor(typeof(ComponentBuilder))]
	public class ComponentBuilderInspector : UnityEditor.Editor
	{
		private SerializedProperty referencesProperty, namespaceProperty, nameProperty;

		private PropertyField nameField, namespaceField;
		private Button generateButton;

		private void OnEnable()
		{
			referencesProperty = serializedObject.FindProperty("references");
			namespaceProperty = serializedObject.FindProperty("namespaceName");
			nameProperty = serializedObject.FindProperty("componentName");
		}

		public override VisualElement CreateInspectorGUI()
		{
			VisualElement root = new VisualElement
			{
				style = { marginTop = 5 }
			};

			root.Add(new Label("Namespace")
			{
				style = { unityFontStyleAndWeight = FontStyle.Bold }
			});

			namespaceField = new PropertyField(namespaceProperty, string.Empty)
			{
				style = { marginBottom = 5, marginLeft = 0, marginRight = 0 }
			};
			namespaceField.RegisterValueChangeCallback(SetSanitisedProperty);
			root.Add(namespaceField);

			root.Add(new Label("Script Name")
			{
				style = { unityFontStyleAndWeight = FontStyle.Bold }
			});

			nameField = new PropertyField(nameProperty, string.Empty)
			{
				style = { marginBottom = 5, marginLeft = 0, marginRight = 0 }
			};
			nameField.RegisterValueChangeCallback(SetSanitisedProperty);
			root.Add(nameField);

			PropertyField referencesField = new PropertyField(referencesProperty, "Referenced Objects");
			referencesField.RegisterValueChangeCallback(_ => SetGenerateButtonEnabledState());
			root.Add(referencesField);

			generateButton = new Button(Generate)
			{
				text = "Generate New Component",
				style =
				{
					height = 26,
					marginLeft = 10,
					marginRight = 10
				}
			};
			generateButton.SetEnabled(false);
			root.Add(generateButton);

			return root;
		}

		public static string SanitiseIdentifier(string input)
		{
			// Remove anything other than letters, numbers, and underscores
			input = Regex.Replace(input, "[^a-zA-Z_0-9]", string.Empty);
			// Remove any numbers at the start
			return Regex.Replace(input, "^[0-9]+", string.Empty);
		}

		private void SetSanitisedProperty(SerializedPropertyChangeEvent serializedPropertyChangeEvent)
		{
			SerializedProperty property = serializedPropertyChangeEvent.changedProperty;
			string result = SanitiseIdentifier(property.stringValue);

			// Automatically capitalise.
			if (result.Length > 0 && char.IsLower(result[0]))
				result = $"{char.ToUpper(result[0])}{result.Substring(1)}";

			if (property.stringValue != result)
			{
				property.stringValue = result;
				property.serializedObject.ApplyModifiedProperties();
				((VisualElement)serializedPropertyChangeEvent.target).MarkDirtyRepaint();
			}

			SetGenerateButtonEnabledState();
		}

		private void SetGenerateButtonEnabledState()
		{
			if (nameProperty.stringValue.Length == 0 || namespaceProperty.stringValue.Length == 0)
			{
				generateButton.SetEnabled(false);
				return;
			}

			for (int i = 0; i < referencesProperty.arraySize; i++)
			{
				SerializedProperty referenceProperty = referencesProperty.GetArrayElementAtIndex(0);
				SerializedProperty objectProperty = referenceProperty.FindPropertyRelative(nameof(ReferenceGroup.Object));
				if (objectProperty.objectReferenceValue == null)
				{
					generateButton.SetEnabled(false);
					return;
				}

				SerializedProperty name = referenceProperty.FindPropertyRelative(nameof(ReferenceGroup.Name));
				if (name.stringValue.Length == 0)
				{
					generateButton.SetEnabled(false);
					return;
				}
			}

			generateButton.SetEnabled(true);
		}

		private void Generate()
		{
			// Check whether the type already exists.
			if (Type.GetType($"{namespaceProperty.stringValue}.{nameProperty.stringValue}") != null)
			{
				Debug.LogError($"{namespaceProperty.stringValue}.{nameProperty.stringValue} already exists.");
				return;
			}

			StringBuilder stringBuilder = new StringBuilder(512);
			IEnumerable<string> namespaces = GetNamespaces(out Dictionary<Type, string> typeNameLookup);
			foreach (string @namespace in namespaces)
			{
				stringBuilder.Append("using ");
				stringBuilder.Append(@namespace);
				stringBuilder.AppendLine(";");
			}

			stringBuilder.AppendLine();
			stringBuilder.Append("namespace ");
			stringBuilder.AppendLine(namespaceProperty.stringValue);
			stringBuilder.AppendLine("{");
			using (TabbedScope namespaceScope = new TabbedScope(stringBuilder, 1))
			{
				namespaceScope.AppendWithTabs("public class ");
				namespaceScope.Append(nameProperty.stringValue);
				namespaceScope.AppendLine(" : MonoBehaviour");
				namespaceScope.AppendWithTabsAndLine("{");
				using (TabbedScope classScope = new TabbedScope(namespaceScope))
				{
					bool hasResetAssignment = false; // Whether or not a Reset method should be created that uses GetComponent assignment.
					for (int i = 0; i < referencesProperty.arraySize; i++)
					{
						SerializedProperty referenceProperty = referencesProperty.GetArrayElementAtIndex(0);
						SerializedProperty objectProperty = referenceProperty.FindPropertyRelative(nameof(ReferenceGroup.Object));
						if (!hasResetAssignment)
						{
							if (PropertyIsReferenceOnThisGameObject(objectProperty))
								hasResetAssignment = true;
						}

						SerializedProperty referenceType = referenceProperty.FindPropertyRelative(nameof(ReferenceGroup.ReferenceType));
						SerializedProperty name = referenceProperty.FindPropertyRelative(nameof(ReferenceGroup.Name));
						AppendMember(classScope, name.stringValue, (ReferenceType)referenceType.intValue, typeNameLookup[objectProperty.objectReferenceValue.GetType()]);
					}

					if (hasResetAssignment)
					{
						classScope.AppendWithTabsAndLine("private void Reset()");
						classScope.AppendWithTabsAndLine("{");
						using (TabbedScope resetScope = new TabbedScope(classScope))
						{
							for (int i = 0; i < referencesProperty.arraySize; i++)
							{
								SerializedProperty referenceProperty = referencesProperty.GetArrayElementAtIndex(0);
								SerializedProperty objectProperty = referenceProperty.FindPropertyRelative(nameof(ReferenceGroup.Object));
								if (!PropertyIsReferenceOnThisGameObject(objectProperty)) continue;
								SerializedProperty name = referenceProperty.FindPropertyRelative(nameof(ReferenceGroup.Name));
								resetScope.AppendWithTabs(name.stringValue);
								resetScope.Append(" = GetComponent<");
								resetScope.Append(typeNameLookup[objectProperty.objectReferenceValue.GetType()]);
								resetScope.AppendLine(">();");
							}
						}

						classScope.AppendWithTabsAndLine("}");
					}
				}

				// Close class scope
				namespaceScope.AppendWithTabsAndLine("}");
			}

			// Close Namespace
			stringBuilder.AppendLine("}");

			bool PropertyIsReferenceOnThisGameObject(SerializedProperty objectProperty) =>
				objectProperty.objectReferenceValue is Component component &&
				component.gameObject == ((ComponentBuilder)serializedObject.targetObject).gameObject;

			bool saved = SaveAndWriteFileDialog(nameProperty.stringValue, stringBuilder.ToString(), title: "Save new component");
			if (!saved) return;
			Debug.Log(Type.GetType($"{namespaceProperty.stringValue}.{nameProperty.stringValue}"));
		}

		private IEnumerable<string> GetNamespaces(out Dictionary<Type, string> typeNameLookup)
		{
			typeNameLookup = new Dictionary<Type, string>();
			using var codeProvider = new CSharpCodeProvider();
			SortedSet<string> namespaces = new SortedSet<string> { "UnityEngine" };
			for (int i = 0; i < referencesProperty.arraySize; i++)
			{
				SerializedProperty referenceProperty = referencesProperty.GetArrayElementAtIndex(0);
				SerializedProperty objectProperty = referenceProperty.FindPropertyRelative(nameof(ReferenceGroup.Object));
				Type type = objectProperty.objectReferenceValue.GetType();
				if (!typeNameLookup.ContainsKey(type))
					typeNameLookup.Add(type, codeProvider.GetTypeOutput(new CodeTypeReference(type)));
				if (type.Namespace == null) continue;
				namespaces.Add(type.Namespace);
			}

			return namespaces;
		}

		private static void AppendMember(TabbedScope scope, string name, ReferenceType type, string typeName)
		{
			switch (type)
			{
				case ReferenceType.PrivateField:
					// [SerializeField] private TYPE name;
					AppendPrivateMember();
					AppendType();
					scope.Append(" ");
					AppendName();
					scope.Append(";");
					break;
				case ReferenceType.PublicField:
					// public TYPE Name;
					AppendPublicMember();
					AppendType();
					scope.Append(" ");
					AppendName();
					scope.Append(";");
					break;
				case ReferenceType.PrivateFieldWithGetProperty:
					// public Type Name => name;
					AppendPublicMember();
					AppendType();
					scope.Append(" => ");
					// TODO append name
					scope.Append(";");

					// [SerializeField] private TYPE name;
					AppendPrivateMember();
					AppendType();
					// TODO append name
					scope.Append(";");
					break;
				case ReferenceType.PropertyWithPrivateSet:
					// [field: SerializeField] 
					AppendSerializedBackingField();
					// public TYPE Name { get; private set; }
					AppendPublicMember();
					AppendType();
					scope.Append(" ");
					AppendName();
					scope.AppendLine(" { get; private set; }");
					break;
				case ReferenceType.PropertyWithPublicSet:
					// [field: SerializeField] 
					AppendSerializedBackingField();
					// public TYPE Name { get; set; }
					AppendPublicMember();
					AppendType();
					scope.Append(" ");
					AppendName();
					scope.AppendLine(" { get; set; }");
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			void AppendPrivateMember() => scope.AppendWithTabs("[SerializeField] private ");
			void AppendSerializedBackingField() => scope.AppendWithTabsAndLine("[field: SerializeField]");
			void AppendPublicMember() => scope.AppendWithTabs("public ");
			void AppendType() => scope.Append(typeName);
			void AppendName() => scope.Append(name);
		}
	}
}