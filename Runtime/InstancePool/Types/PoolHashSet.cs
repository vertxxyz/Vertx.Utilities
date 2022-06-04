using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vertx.Utilities
{
	/// <summary>
	/// An expandable pool that uses a HashSet internally.
	/// </summary>
	/// <typeparam name="TInstanceType">The type of component managed by the pool.</typeparam>
	internal class PoolHashSet<TInstanceType> : IPoolCollection<TInstanceType> where TInstanceType : Component
	{
		private readonly HashSet<TInstanceType> _instances = new HashSet<TInstanceType>();

		public int Count => _instances.Count;
		public bool Push(TInstanceType instance) => _instances.Add(instance);
		public bool Remove(TInstanceType instance) => _instances.Remove(instance);

		public bool TryPop(out TInstanceType instance)
		{
			instance = null;
			if (_instances.Count == 0)
				return false;
			
			bool found = false;
			bool hasNull = false;
			foreach (TInstanceType i in _instances)
			{
				found = i != null;
				if (found)
				{
					instance = i;
					break;
				}

				hasNull = true;
			}

			if (hasNull)
				_instances.RemoveWhere(i => i == null);
			if (found)
				_instances.Remove(instance);
			return found;
		}
		
		public void TrimExcess(int capacity, HashSet<TInstanceType> temp)
		{
			int c = 0;
			foreach (var instance in _instances)
			{
				if (instance == null) continue;
				if (c++ < capacity)
					temp.Add(instance);
				else
					Object.Destroy(instance.gameObject);
			}

			_instances.IntersectWith(temp);
		}

		IEnumerator<TInstanceType> IEnumerable<TInstanceType>.GetEnumerator() => ((IEnumerable<TInstanceType>)_instances).GetEnumerator();

		public IEnumerator GetEnumerator() => _instances.GetEnumerator();
	}
}