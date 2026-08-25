using System;
using System.Globalization;
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
		public override int GetHashCode() => Center.GetHashCode() ^ (Extents.GetHashCode() << 2);

		// also required for being able to use Vector4s as keys in hash tables
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object other) => other is Bounds2D bounds2D && Equals(bounds2D);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Bounds2D other) => Center.Equals(other.Center) && Extents.Equals(other.Extents);

		// The center of the bounding box.
		public Vector2 Center
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _center;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _center = value;
		}

		// The total size of the box. This is always twice as large as the ::ref::extents.
		public Vector2 Size
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _extents * 2.0F;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _extents = value * 0.5F;
		}

		// The extents of the box. This is always half of the ::ref::size.
		public Vector2 Extents
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _extents;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _extents = value;
		}

		// The minimal point of the box. This is always equal to ''center-extents''.
		public Vector2 Min
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Center - Extents;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => SetMinMax(value, Max);
		}

		// The maximal point of the box. This is always equal to ''center+extents''.
		public Vector2 Max
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Center + Extents;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => SetMinMax(Min, value);
		}

		//*undoc*
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(Bounds2D lhs, Bounds2D rhs)
		{
			// Returns false in the presence of NaN values.
			return lhs.Center == rhs.Center && lhs.Extents == rhs.Extents;
		}

		//*undoc*
		// Returns true in the presence of NaN values.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(Bounds2D lhs, Bounds2D rhs) => !(lhs == rhs);

		// Sets the Bounds2D to the /min/ and /max/ value of the box.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetMinMax(Vector2 min, Vector2 max)
		{
			Extents = (max - min) * 0.5F;
			Center = min + Extents;
		}

		// Grows the Bounds2D to include the /point/.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(Vector2 point) => SetMinMax(Vector2.Min(Min, point), Vector2.Max(Max, point));

		// Grows the Bounds2D to include the /Bounds/.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(Bounds2D bounds)
		{
			Encapsulate(bounds.Center - bounds.Extents);
			Encapsulate(bounds.Center + bounds.Extents);
		}

		// Expand the Bounds2D by increasing its /size/ by /amount/ along each side.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Expand(float amount)
		{
			amount *= .5f;
			Extents += new Vector2(amount, amount);
		}

		// Expand the Bounds2D by increasing its /size/ by /amount/ along each side.
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Expand(Vector2 amount) => Extents += amount * .5f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(Vector2 point) =>
			Min.x <= point.x && Max.x >= point.x &&
			Min.y <= point.y && Max.y >= point.y;

		// Does another bounding box intersect with this bounding box?
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Intersects(Bounds2D bounds) =>
			Min.x <= bounds.Max.x && Max.x >= bounds.Min.x &&
			Min.y <= bounds.Max.y && Max.y >= bounds.Min.y;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectRay(Ray ray) => new Bounds(Center, Size).IntersectRay(ray, out float _);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectRay(Ray ray, out float distance) => new Bounds(Center, Size).IntersectRay(ray, out distance);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2 ClosestPoint(Vector2 point)
		{
			if (point.x < Min.x)
			{
				if (point.y < Min.y)
					return Min;
				if (point.y > Max.y)
					return new Vector2(Min.x, Max.y);
				return new Vector2(Min.x, point.y);
			}

			if (point.x > Max.x)
			{
				if (point.y < Min.y)
					return new Vector2(Max.x, Min.y);
				if (point.y > Max.y)
					return Max;
				return new Vector2(Max.x, point.y);
			}

			if (point.y < Min.y)
				return new Vector2(point.x, Min.y);

			if (point.y > Max.y)
				return new Vector2(point.x, Max.y);

			return point;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float SignedDistance(Vector2 point)
		{
			point -= Center;
			Vector2 d = new Vector2(Mathf.Abs(point.x), Mathf.Abs(point.y)) - Extents;
			return Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0);
		}
		
		public static implicit operator Rect(Bounds2D bounds2D) => new Rect(bounds2D.Min, bounds2D.Size);

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
			return string.Format(CultureInfo.InvariantCulture.NumberFormat, "Center: {0}, Extents: {1}", _center.ToString(format, formatProvider), _extents.ToString(format, formatProvider));
		}
#endif
	}
}