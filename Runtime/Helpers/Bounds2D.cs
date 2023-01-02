using System;
#if UNITY_2020_1_OR_NEWER
using System.Globalization;
#endif
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Vertx.Utilities
{
	[StructLayout(LayoutKind.Sequential)]
	[Serializable]
	public struct Bounds2D : IEquatable<Bounds2D>
#if UNITY_2020_1_OR_NEWER
		, IFormattable
#endif
	{
		[SerializeField] private Vector2 _center;
		[SerializeField] private Vector2 _extents;

		// Creates new Bounds2D with a given /center/ and total /size/. Bound ::ref::extents will be half the given size.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Bounds2D(Vector2 center, Vector2 size)
		{
			_center = center;
			_extents = size * 0.5F;
		}

		// used to allow Bounds2D to be used as keys in hash tables
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => center.GetHashCode() ^ (extents.GetHashCode() << 2);

		// also required for being able to use Vector4s as keys in hash tables
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object other) => other is Bounds2D bounds2D && Equals(bounds2D);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Bounds2D other) => center.Equals(other.center) && extents.Equals(other.extents);

		// The center of the bounding box.
		public Vector2 center
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _center;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _center = value;
		}

		// The total size of the box. This is always twice as large as the ::ref::extents.
		public Vector2 size
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _extents * 2.0F;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _extents = value * 0.5F;
		}

		// The extents of the box. This is always half of the ::ref::size.
		public Vector2 extents
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _extents;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _extents = value;
		}

		// The minimal point of the box. This is always equal to ''center-extents''.
		public Vector2 min
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => center - extents;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => SetMinMax(value, max);
		}

		// The maximal point of the box. This is always equal to ''center+extents''.
		public Vector2 max
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => center + extents;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => SetMinMax(min, value);
		}

		//*undoc*
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(Bounds2D lhs, Bounds2D rhs)
		{
			// Returns false in the presence of NaN values.
			return lhs.center == rhs.center && lhs.extents == rhs.extents;
		}

		//*undoc*
		// Returns true in the presence of NaN values.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Bounds2D lhs, Bounds2D rhs) => !(lhs == rhs);

		// Sets the Bounds2D to the /min/ and /max/ value of the box.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetMinMax(Vector2 min, Vector2 max)
		{
			extents = (max - min) * 0.5F;
			center = min + extents;
		}

		// Grows the Bounds2D to include the /point/.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(Vector2 point) => SetMinMax(Vector2.Min(min, point), Vector2.Max(max, point));

		// Grows the Bounds2D to include the /Bounds/.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(Bounds2D bounds)
		{
			Encapsulate(bounds.center - bounds.extents);
			Encapsulate(bounds.center + bounds.extents);
		}

		// Expand the Bounds2D by increasing its /size/ by /amount/ along each side.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Expand(float amount)
		{
			amount *= .5f;
			extents += new Vector2(amount, amount);
		}

		// Expand the Bounds2D by increasing its /size/ by /amount/ along each side.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Expand(Vector2 amount) => extents += amount * .5f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(Vector2 point) =>
			min.x <= point.x && max.x >= point.x &&
			min.y <= point.y && max.y >= point.y;

		// Does another bounding box intersect with this bounding box?
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Intersects(Bounds2D bounds) =>
			min.x <= bounds.max.x && max.x >= bounds.min.x &&
			min.y <= bounds.max.y && max.y >= bounds.min.y;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectRay(Ray ray) => new Bounds(center, size).IntersectRay(ray, out float _);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectRay(Ray ray, out float distance) => new Bounds(center, size).IntersectRay(ray, out distance);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2 ClosestPoint(Vector2 point)
		{
			if (point.x < min.x)
			{
				if (point.y < min.y)
					return min;
				if (point.y > max.y)
					return new Vector2(min.x, max.y);
				return new Vector2(min.x, point.y);
			}

			if (point.x > max.x)
			{
				if (point.y < min.y)
					return new Vector2(max.x, min.y);
				if (point.y > max.y)
					return max;
				return new Vector2(max.x, point.y);
			}

			if (point.y < min.y)
				return new Vector2(point.x, min.y);

			if (point.y > max.y)
				return new Vector2(point.x, max.y);

			return point;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float SignedDistance(Vector2 point)
		{
			point -= center;
			Vector2 d = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - extents;
			return Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0);
		}
		
		public static implicit operator Rect(Bounds2D bounds2D) => new Rect(bounds2D.min, bounds2D.size);

		/// *listonly*
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString() => ToString(null, null);

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
			return string.Format(CultureInfo.InvariantCulture.NumberFormat, "Center: {0}, Extents: {1}", _center.ToString(format, formatProvider), _extents.ToString(format, formatProvider));
		}
#endif
	}
}