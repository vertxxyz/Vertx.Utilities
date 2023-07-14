using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;
#if UNITY_MATHEMATICS
using Unity.Mathematics;
#endif

namespace Vertx.Utilities
{
	[StructLayout(LayoutKind.Sequential)]
	[Serializable]
	public struct Bounds2DInt : IEquatable<Bounds2DInt>
#if UNITY_2020_1_OR_NEWER
		, IFormattable
#endif
	{
		private Vector2Int _position;
		private Vector2Int _size;

		public int X
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _position.x;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _position.x = value;
		}

		public int Y
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _position.y;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _position.y = value;
		}

		public Vector2 Center
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2(X + _size.x / 2f, Y + _size.y / 2f);
		}

		public Vector2Int Min
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2Int(XMin, YMin);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				XMin = value.x;
				YMin = value.y;
			}
		}

		public Vector2Int Max
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new Vector2Int(XMax, YMax);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				XMax = value.x;
				YMax = value.y;
			}
		}

		public int XMin
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Math.Min(_position.x, _position.x + _size.x);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				int oldXMax = XMax;
				_position.x = value;
				_size.x = oldXMax - _position.x;
			}
		}

		public int YMin
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Math.Min(_position.y, _position.y + _size.y);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				int oldYMax = YMax;
				_position.y = value;
				_size.y = oldYMax - _position.y;
			}
		}

		public int XMax
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Math.Max(_position.x, _position.x + _size.x);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _size.x = value - _position.x;
		}

		public int YMax
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Math.Max(_position.y, _position.y + _size.y);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _size.y = value - _position.y;
		}

		public Vector2Int Position
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _position;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _position = value;
		}

		public Vector2Int Size
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _size;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _size = value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Bounds2DInt(int xMin, int yMin, int sizeX, int sizeY)
		{
			_position = new Vector2Int(xMin, yMin);
			_size = new Vector2Int(sizeX, sizeY);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Bounds2DInt(Vector2Int position, Vector2Int size)
		{
			_position = position;
			_size = size;
		}
		
#if UNITY_MATHEMATICS
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Bounds2DInt(int2 position, int2 size)
		{
			_position = new Vector2Int(position.x, position.y);
			_size = new Vector2Int(size.x, size.y);
		}
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetMinMax(Vector2Int minPosition, Vector2Int maxPosition)
		{
			Min = minPosition;
			Max = maxPosition;
		}
		
		// Grows the Bounds2DInt to include the /point/.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(Vector2Int point) => SetMinMax(Vector2Int.Min(Min, point), Vector2Int.Max(Max, point));

		// Grows the Bounds2DInt to include the /Bounds/.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(Bounds2DInt bounds)
		{
			Encapsulate(bounds.Min);
			Encapsulate(bounds.Max);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ClampToBounds(BoundsInt bounds)
		{
			Position = new Vector2Int(
				Math.Max(Math.Min(bounds.xMax, Position.x), bounds.xMin),
				Math.Max(Math.Min(bounds.yMax, Position.y), bounds.yMin)
			);
			Size = new Vector2Int(
				Math.Min(bounds.xMax - Position.x, Size.x),
				Math.Min(bounds.yMax - Position.y, Size.y)
			);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float SignedDistance(Vector2 point) => ((Bounds2D)this).SignedDistance(point);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2 ClosestPoint(Vector2 point) => ((Bounds2D)this).ClosestPoint(point);

		/// <summary>
		/// <see cref="Bounds2DInt"/> Expand does not work like <see cref="Bounds2D"/>, it will expand size by amount * 2, and increase the size on both sides.<br/>
		/// If it didn't expand * 2 it would have to move the center point to a non-integer value.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Expand(int amount)
		{
			_position.x -= amount;
			_position.y -= amount;
			amount *= 2;
			_size.x += amount;
			_size.y += amount;
		}
		
		/// <summary>
		/// <see cref="Bounds2DInt"/> Expand does not work like <see cref="Bounds2D"/>, it will expand size by amount * 2, and increase the size on both sides.<br/>
		/// If it didn't expand * 2 it would have to move the center point to a non-integer value.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Expand(Vector2Int amount)
		{
			_position -= amount;
			amount *= 2;
			_size += amount;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddSize(Vector2Int amount) => _size += amount;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddSize(int amount) => AddSize(new Vector2Int(amount, amount));
		
#if UNITY_MATHEMATICS
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddSize(int2 amount)
		{
			_size.x += amount.x;
			_size.y += amount.y;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetMinMax(int2 min, int2 max) => SetMinMax(new Vector2Int(min.x, min.y), new Vector2Int(max.x, max.y));
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(int2 point) => Encapsulate(new Vector2Int(point.x, point.y));
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(int2 point) => Contains(new Vector2Int(point.x, point.y));
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float SignedDistance(int2 point) => SignedDistance(new Vector2Int(point.x, point.y));
		
		public void Expand(int2 amount) => Expand(new Vector2Int(amount.x, amount.y));
#endif

		// Does another bounding box intersect with this bounding box?
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Intersects(Bounds2DInt bounds) =>
			Min.x <= bounds.Max.x && Max.x >= bounds.Min.x &&
			Min.y <= bounds.Max.y && Max.y >= bounds.Min.y;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectRay(Ray ray) => new Bounds(Center, new Vector3(Size.x, Size.y, 0)).IntersectRay(ray, out float _);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectRay(Ray ray, out float distance) => new Bounds(Center, new Vector3(Size.x, Size.y, 0)).IntersectRay(ray, out distance);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(Vector2Int position) =>
			position.x >= XMin
			&& position.y >= YMin
			&& position.x < XMax
			&& position.y < YMax;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(Bounds2DInt lhs, Bounds2DInt rhs) => lhs._position == rhs._position && lhs._size == rhs._size;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Bounds2DInt lhs, Bounds2DInt rhs) => !(lhs == rhs);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object other)
		{
			if (!(other is Bounds2DInt bounds)) return false;
			return Equals(bounds);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Bounds2DInt other) => _position.Equals(other._position) && _size.Equals(other._size);

		public override int GetHashCode() => _position.GetHashCode() ^ (_size.GetHashCode() << 2);

		public PositionEnumerator AllPositionsWithin
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new PositionEnumerator(Min, Max);
		}
		
		/// <summary>
		/// As using <see cref="Encapsulate"/> on a grid point produces something of 0 size, <see cref="AllPositionsWithin"/> will not return any values.<br/>
		/// But seeing as you may want to iterate over that position, this property will return an enumerator that includes positions as if size was grown by 1.
		/// </summary>
		public PositionEnumerator AllPositionsTouching
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => new PositionEnumerator(Min, Max + new Vector2Int(1, 1));
		}

		public struct PositionEnumerator : IEnumerator<Vector2Int>
		{
			private readonly Vector2Int _min, _max;
			private Vector2Int _current;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public PositionEnumerator(Vector2Int min, Vector2Int max)
			{
				_min = _current = min;
				_max = max;
				Reset();
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public PositionEnumerator GetEnumerator() => this;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public bool MoveNext()
			{
				if (_current.y >= _max.y)
					return false;

				_current.x++;
				if (_current.x >= _max.x)
				{
					_current.x = _min.x;
					if (_current.x >= _max.x)
						return false;

					_current.y++;
					if (_current.y >= _max.y)
					{
						return false;
					}
				}

				return true;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Reset()
			{
				_current = _min;
				_current.x--;
			}

			public Vector2Int Current
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => _current;
			}

			object IEnumerator.Current
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => Current;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void IDisposable.Dispose() { }
		}

		public static implicit operator Rect(Bounds2DInt bounds2D) => new Rect(bounds2D.Min, bounds2D.Size);
		public static implicit operator Bounds2D(Bounds2DInt bounds2D) => new Bounds2D((bounds2D.Center), bounds2D.Size);

		/// *listonly*
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString() =>
			ToString(null
#if UNITY_2020_1_OR_NEWER
				, null
#endif
			);

		// Returns a nicely formatted string for the bounds.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToString(string format)
		{
#if UNITY_2020_1_OR_NEWER
			return ToString(format, null);
#else
			return string.Format(CultureInfo.InvariantCulture.NumberFormat, "Center: {0}, Extents: {1}", _center.ToString(format), _extents.ToString(format));
#endif
		}

#if UNITY_2020_1_OR_NEWER
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToString(string format, IFormatProvider formatProvider)
		{
			if (string.IsNullOrEmpty(format))
				format = "F2";
			if (formatProvider == null)
				formatProvider = CultureInfo.InvariantCulture.NumberFormat;
			return string.Format(CultureInfo.InvariantCulture.NumberFormat, "Position: {0}, Size: {1}", _position.ToString(format, formatProvider), _size.ToString(format, formatProvider));
		}
#endif
	}
}