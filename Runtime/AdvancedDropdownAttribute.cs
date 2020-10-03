using System;

namespace Vertx.Utilities
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
	public abstract class AdvancedDropdownAttribute : Attribute
	{
		public string Name { get; }
		public string Path { get; }

		/// <summary>
		/// When implemented this attribute can be combined with AdvancedDropdownUtils.CreateAdvancedDropdownFromAttribute
		/// to generate an AdvancedDropdown using all types that have the attribute applied.
		/// </summary>
		/// <param name="name">Name of the item.</param>
		/// <param name="path">Paths do not contain the name. eg. "Folder/Sub Folder"</param>
		public AdvancedDropdownAttribute(string name, string path)
		{
			Name = name;
			Path = path;
		}
	}
}