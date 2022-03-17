#if UNITY_2020_1_OR_NEWER
using UnityEngine;
using UnityEngine.Serialization;

namespace Vertx.Utilities
{
	/// <summary>
	/// A helper class for creating small Scriptable Objects to hold data/configuration.
	/// You can use these to avoid using singletons to reference data that is static, and to generally share EnumToValue data.
	/// </summary>
	/// <typeparam name="T">The EnumToValue data this stores.</typeparam>
	//[CreateAssetMenu(fileName = "Data Description", menuName = "Vertx/Data Descriptions/Data")]
	public abstract class EnumDataDescription<T> : ScriptableObject
		where T : EnumToValueBase
	{
		[FormerlySerializedAs("Data")]
		[SerializeField] private T data = default;
		public T Data => data;
	}
}
#endif