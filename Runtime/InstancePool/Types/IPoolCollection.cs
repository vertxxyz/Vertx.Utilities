using System.Collections.Generic;
using UnityEngine;

namespace Vertx.Utilities
{
	internal interface IPoolCollection<TInstanceType> : IEnumerable<TInstanceType> where TInstanceType : Component
	{
		public int Count { get; }
		public bool Add(TInstanceType instance);
		public bool Remove(TInstanceType instance);
		bool TryGet(out TInstanceType instance);
		void TrimExcess(int capacity, HashSet<TInstanceType> temp);
	}
}