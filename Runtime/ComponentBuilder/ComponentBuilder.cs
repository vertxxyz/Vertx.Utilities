using System.Collections.Generic;
using UnityEngine;
// ReSharper disable NotAccessedField.Local

namespace Vertx.Utilities
{
	/// <summary>
	/// A helper component allowing simple construction of scripts that reference other components
	/// The implementation for this component is all in-editor.
	/// </summary>
	public class ComponentBuilder : MonoBehaviour
	{
		[SerializeField] private string namespaceName;
		[SerializeField] private string componentName;
		[SerializeField] private List<ReferenceGroup> references;

		[System.Serializable]
		public class ReferenceGroup
		{
			public Object Object;
			public ReferenceType ReferenceType;
			public string Name;
		}

		public enum ReferenceType
		{
			PrivateField,
			PublicField,
			PrivateFieldWithGetProperty,
			PropertyWithPrivateSet,
			PropertyWithPublicSet
		}
	}
}