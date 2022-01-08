using System;
using System.Collections.Generic;
using System.Reflection;

namespace Vertx.Utilities.Editor
{
	public static partial class EditorUtils
	{
		private static Type GetArrayOrListElementType(this Type listType)
		{
			if (listType.IsArray)
				return listType.GetElementType();
			return listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>) ? listType.GetGenericArguments()[0] : null;
		}

		/// <summary>
		/// Returns the type that Unity would be serializing for the properties contained within a FieldInfo.
		/// Ie. if an Array or List is provided, this function will return the element type.
		/// </summary>
		/// <param name="fieldInfo">The field to query the serialized type</param>
		/// <returns>The element type serialized by Unity</returns>
		public static Type GetSerializedTypeFromFieldInfo(FieldInfo fieldInfo)
		{
			Type type = fieldInfo.FieldType;
			return type.GetArrayOrListElementType() ?? type;
		}
	}
}