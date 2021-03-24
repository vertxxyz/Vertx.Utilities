using System;
using UnityEngine;

namespace RiftWreckers
{
	public class InterfaceProviderAttribute : PropertyAttribute
	{
		public readonly Type Type;
		public InterfaceProviderAttribute(Type type) => Type = type;
	}
}