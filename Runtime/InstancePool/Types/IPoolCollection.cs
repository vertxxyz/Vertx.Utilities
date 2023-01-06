using System.Collections.Generic;
using UnityEngine;

namespace Vertx.Utilities
{
	public interface IPoolCollection<TInstanceType> : IEnumerable<TInstanceType> where TInstanceType : Component
	{
		int Count { get; }
		bool Push(TInstanceType instance);

		bool TryPop(out TInstanceType instance);
		void TrimExcess(int capacity, HashSet<TInstanceType> temp);
	}
}