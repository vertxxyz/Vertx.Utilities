using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Vertx.Utilities.Editor
{
	public static partial class EditorUtils
	{
		public static int GetIndexFromArrayProperty(SerializedProperty property)
		{
			string propertyPath = property.propertyPath;
			int lastIndexOf = propertyPath.LastIndexOf('[');
			if (lastIndexOf < 0)
				throw GetException();
			lastIndexOf++;
			if (lastIndexOf == propertyPath.Length)
				throw GetException();
			if (int.TryParse(propertyPath.Substring(lastIndexOf, propertyPath.Length - lastIndexOf - 1), out var value))
				return value;
			throw GetException();
			Exception GetException() => new ArgumentException($"Property with path: \"{propertyPath}\" is not an array property.");
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

		public static void LogAllProperties(this SerializedProperty property)
		{
			StringBuilder stringBuilder = new StringBuilder(property.propertyPath);
			stringBuilder.AppendLine(":");
			SerializedProperty rootProp = property.Copy();
			AppendProperty(stringBuilder, rootProp, rootProp.GetEndProperty(true), property.propertyPath.Length, 1);
			Debug.Log(stringBuilder);
		}

		public static SerializedProperty FindBackingProperty(this SerializedProperty property, string propertyName) =>
			property.FindPropertyRelative($"<{propertyName}>k__BackingField");

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
				}
				else if (property.hasChildren)
				{
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
		private static object[] fieldInfoArray;

		public static FieldInfo GetFieldInfoFromProperty(SerializedProperty property, out Type type)
		{
			if (fieldInfoArray == null)
				fieldInfoArray = new object[2];
			fieldInfoArray[0] = property;
			fieldInfoArray[1] = null;
			FieldInfo fieldInfo = (FieldInfo)GetFieldInfoFromPropertyMethod.Invoke(null, fieldInfoArray);
			type = (Type)fieldInfoArray[1];
			return fieldInfo;
		}

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
						@object = ((Array)@object).GetValue(index);
					}
					else if (prop.propertyPath.EndsWith("Array.size"))
					{
						if (@object == null)
							return 0;
						parent = @object;
						@object = ((Array)@object).Length;
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
						@object = ((IList)@object)[index];
					}
					else if (prop.propertyPath.EndsWith("Array.size"))
					{
						if (@object == null)
							return 0;
						parent = @object;
						@object = ((IList)@object).Count;
						return @object;
					}
					else
						return @object;

					type = @object?.GetType();
				}
			}

			return @object;
		}

		public static object GetPropertyValue(this SerializedProperty property)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer:
					return property.intValue;
				case SerializedPropertyType.Boolean:
					return property.boolValue;
				case SerializedPropertyType.Float:
					return property.floatValue;
				case SerializedPropertyType.String:
					return property.stringValue;
				case SerializedPropertyType.ObjectReference:
					return property.objectReferenceValue;
				case SerializedPropertyType.LayerMask:
					return property.intValue;
				case SerializedPropertyType.Enum:
					return property.intValue;
				case SerializedPropertyType.ArraySize:
					return property.arraySize;
				case SerializedPropertyType.Character:
					return property.stringValue;
				case SerializedPropertyType.Color:
					return property.colorValue;
				case SerializedPropertyType.Vector2:
					return property.vector2Value;
				case SerializedPropertyType.Vector3:
					return property.vector3Value;
				case SerializedPropertyType.Vector4:
					return property.vector4Value;
				case SerializedPropertyType.AnimationCurve:
					return property.animationCurveValue;
				case SerializedPropertyType.Quaternion:
					return property.quaternionValue;
				case SerializedPropertyType.FixedBufferSize:
					return property.fixedBufferSize;
				case SerializedPropertyType.Vector2Int:
					return property.vector2IntValue;
				case SerializedPropertyType.Vector3Int:
					return property.vector3IntValue;
				case SerializedPropertyType.Rect:
					return property.rectValue;
				case SerializedPropertyType.RectInt:
					return property.rectIntValue;
				case SerializedPropertyType.Bounds:
					return property.boundsValue;
				case SerializedPropertyType.BoundsInt:
					return property.boundsIntValue;
				case SerializedPropertyType.ExposedReference:
					return property.exposedReferenceValue;
				case SerializedPropertyType.Gradient:
					return GetGradientValue(property);
				case SerializedPropertyType.Generic:
					return GetObjectFromProperty(property, out _, out _);
#if UNITY_2019_3_OR_NEWER
				case SerializedPropertyType.ManagedReference:
#endif
				default:
					throw new NotSupportedException($"{nameof(GetPropertyValue)} does not support values of type {property.propertyType}");
			}
		}

		public static string ToFullString(this SerializedProperty property)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.Float:
					return property.floatValue.ToString(CultureInfo.InvariantCulture);
				case SerializedPropertyType.Integer:
					return property.intValue.ToString();
				case SerializedPropertyType.Boolean:
					return property.boolValue.ToString();
				case SerializedPropertyType.String:
					return property.stringValue;
				case SerializedPropertyType.ObjectReference:
					return property.objectReferenceValue == null ? string.Empty : property.objectReferenceValue.name;
				case SerializedPropertyType.LayerMask:
					return property.enumDisplayNames[property.intValue];
				case SerializedPropertyType.Enum:
					return property.enumDisplayNames[property.intValue];
				case SerializedPropertyType.ArraySize:
					return property.arraySize.ToString();
				case SerializedPropertyType.Character:
					return property.stringValue;
				case SerializedPropertyType.Color:
					return property.colorValue.ToString();
				case SerializedPropertyType.Vector2:
					return property.vector2Value.ToString("F7");
				case SerializedPropertyType.Vector3:
					return property.vector3Value.ToString("F7");
				case SerializedPropertyType.Vector4:
					return property.vector4Value.ToString("F7");
				case SerializedPropertyType.Quaternion:
					return property.quaternionValue.eulerAngles.ToString("F7");
				case SerializedPropertyType.FixedBufferSize:
					return property.fixedBufferSize.ToString();
				case SerializedPropertyType.Vector2Int:
					return property.vector2IntValue.ToString();
				case SerializedPropertyType.Vector3Int:
					return property.vector3IntValue.ToString();
				case SerializedPropertyType.Rect:
					return property.rectValue.ToString();
				case SerializedPropertyType.RectInt:
					return property.rectIntValue.ToString();
				case SerializedPropertyType.Bounds:
					return property.boundsValue.ToString();
				case SerializedPropertyType.BoundsInt:
					return property.boundsIntValue.ToString();
				case SerializedPropertyType.ExposedReference:
					return property.exposedReferenceValue == null ? string.Empty : property.exposedReferenceValue.name;
				case SerializedPropertyType.Gradient:
				{
					Gradient gradient = GetGradientValue(property);
					if (gradient == null) return string.Empty;
					StringBuilder sB = new StringBuilder(20);
					string asciiGradient = " .:-=+*#%@";
					int gradientMultiplier = asciiGradient.Length - 1;
					for (int i = 0; i < 20; i++)
					{
						float grayscale = 1 - gradient.Evaluate(i / 19f).grayscale;
						sB.Append(asciiGradient[Mathf.Clamp(Mathf.RoundToInt(grayscale * gradientMultiplier), 0, gradientMultiplier)]);
					}

					return sB.ToString();
				}
				case SerializedPropertyType.AnimationCurve:
					return property.animationCurveValue.ToString();
#if UNITY_2019_3_OR_NEWER
				case SerializedPropertyType.ManagedReference:
#endif
				case SerializedPropertyType.Generic:
				default:
					return property.ToString();
			}
		}

		private static Gradient GetGradientValue(SerializedProperty property)
		{
			PropertyInfo propertyInfo = typeof(SerializedProperty).GetProperty(
				"gradientValue",
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
				null,
				typeof(Gradient),
				Array.Empty<Type>(),
				null
			);
			return propertyInfo?.GetValue(property, null) as Gradient;
		}

		public static void SimpleCopyTo(this SerializedProperty origin, SerializedProperty destination)
		{
			if (origin.propertyType != destination.propertyType)
				throw new ArgumentException($"{nameof(origin)} and {nameof(destination)} do not have the same {nameof(origin.propertyType)}.\n" +
				                            $"{origin.propertyType} and {destination.propertyType}");

			if (origin.isArray != destination.isArray)
				throw new ArgumentException($"Only one of {nameof(origin)} and {nameof(destination)} are array properties.");

			if (origin.isArray)
			{
				int arraySize = origin.arraySize;
				destination.arraySize = arraySize;
				for (int i = 0; i < arraySize; i++)
				{
					SerializedProperty iOrigin = origin.GetArrayElementAtIndex(i);
					SerializedProperty iDestination = destination.GetArrayElementAtIndex(i);
					iOrigin.SimpleCopyTo(iDestination);
				}

				return;
			}

			switch (origin.propertyType)
			{
				case SerializedPropertyType.Integer:
					destination.intValue = origin.intValue;
					break;
				case SerializedPropertyType.Boolean:
					destination.boolValue = origin.boolValue;
					break;
				case SerializedPropertyType.Float:
					destination.floatValue = origin.floatValue;
					break;
				case SerializedPropertyType.String:
					destination.stringValue = origin.stringValue;
					break;
				case SerializedPropertyType.ObjectReference:
					destination.objectReferenceValue = origin.objectReferenceValue;
					break;
				case SerializedPropertyType.LayerMask:
					destination.intValue = origin.intValue;
					break;
				case SerializedPropertyType.Enum:
					destination.intValue = origin.intValue;
					break;
				case SerializedPropertyType.ArraySize:
					destination.arraySize = origin.arraySize;
					break;
				case SerializedPropertyType.Character:
					destination.stringValue = origin.stringValue;
					break;
				case SerializedPropertyType.Color:
					destination.colorValue = origin.colorValue;
					break;
				case SerializedPropertyType.Vector2:
					destination.vector2Value = origin.vector2Value;
					break;
				case SerializedPropertyType.Vector3:
					destination.vector3Value = origin.vector3Value;
					break;
				case SerializedPropertyType.Vector4:
					destination.vector4Value = origin.vector4Value;
					break;
				case SerializedPropertyType.AnimationCurve:
					destination.animationCurveValue = origin.animationCurveValue;
					break;
				case SerializedPropertyType.Quaternion:
					destination.quaternionValue = origin.quaternionValue;
					break;
				case SerializedPropertyType.Vector2Int:
					destination.vector2IntValue = origin.vector2IntValue;
					break;
				case SerializedPropertyType.Vector3Int:
					destination.vector3IntValue = origin.vector3IntValue;
					break;
				case SerializedPropertyType.Rect:
					destination.rectValue = origin.rectValue;
					break;
				case SerializedPropertyType.RectInt:
					destination.rectIntValue = origin.rectIntValue;
					break;
				case SerializedPropertyType.Bounds:
					destination.boundsValue = origin.boundsValue;
					break;
				case SerializedPropertyType.BoundsInt:
					destination.boundsIntValue = origin.boundsIntValue;
					break;
				case SerializedPropertyType.ExposedReference:
					destination.exposedReferenceValue = origin.exposedReferenceValue;
					break;
#if UNITY_2019_3_OR_NEWER
				case SerializedPropertyType.ManagedReference:
#endif
				case SerializedPropertyType.FixedBufferSize:
				case SerializedPropertyType.Gradient:
				case SerializedPropertyType.Generic:
				default:
					throw new NotSupportedException($"{nameof(SimpleCopyTo)} does not support values of type {destination.propertyType}");
			}
		}

		public static void ReverseArray(this SerializedProperty property)
		{
			if (!property.isArray)
			{
				Debug.LogError($"{property} is not an array.");
				return;
			}

			int c = property.arraySize;
			for (int end = c - 1; end > 0; end--)
				property.MoveArrayElement(0, end);
		}

		private static Type scriptAttributeUtilityType;
		private static Type ScriptAttributeUtilityType =>
			scriptAttributeUtilityType ?? (scriptAttributeUtilityType = typeof(EditorWindow).Assembly.GetType("UnityEditor.ScriptAttributeUtility"));

		private static MethodInfo getHandlerMethod;
		private static MethodInfo GetHandlerMethod =>
			getHandlerMethod ?? (getHandlerMethod = ScriptAttributeUtilityType.GetMethod("GetHandler", BindingFlags.NonPublic | BindingFlags.Static));

		private static MethodInfo getFieldInfoFromPropertyMethod;
		private static MethodInfo GetFieldInfoFromPropertyMethod =>
			getFieldInfoFromPropertyMethod ?? (getFieldInfoFromPropertyMethod =
				ScriptAttributeUtilityType.GetMethod("GetFieldInfoFromProperty", BindingFlags.NonPublic | BindingFlags.Static));

		private static Type propertyHandlerType;
		private static Type PropertyHandlerType =>
			propertyHandlerType ?? (propertyHandlerType = typeof(EditorWindow).Assembly.GetType("UnityEditor.PropertyHandler"));

		private static PropertyInfo hasPropertyDrawerProperty;
		private static PropertyInfo HasPropertyDrawerProperty =>
			hasPropertyDrawerProperty ?? (hasPropertyDrawerProperty = PropertyHandlerType.GetProperty("hasPropertyDrawer", BindingFlags.Public | BindingFlags.Instance));


		public static bool HasCustomPropertyDrawer(SerializedProperty property)
		{
			var handler = GetHandlerMethod.Invoke(null, new object[] { property });
			return (bool)HasPropertyDrawerProperty.GetValue(handler);
		}
	}
}