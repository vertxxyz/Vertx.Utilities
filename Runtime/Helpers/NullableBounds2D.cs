using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Vertx.Utilities
{
	/// <summary>
	/// <para>
	/// <see cref="Bounds2D"/>, but <see cref="NullableBounds2D.Encapsulate(Bounds2D)"/> and its variants don't expand from the default value when first called.
	/// </para>
	/// If a value has not yet been assigned methods like Intersects, IntersectRay, Contains, will return false.
	///	Other query methods have `TryGet` alternatives.
	/// </summary>
	public struct NullableBounds2D : IEquatable<Bounds2D>, IEquatable<NullableBounds2D>
#if UNITY_2020_1_OR_NEWER
		, IFormattable
#endif
	{
		public bool HasValue => _hasValue;

		public Bounds2D Value
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set
			{
				_value = value;
				_hasValue = true;
			}
		}

		private bool _hasValue;
		private Bounds2D _value;

		public NullableBounds2D(Vector2 center, Vector2 size)
		{
			_value = new Bounds2D(center, size * 0.5f);
			_hasValue = true;
		}

		public NullableBounds2D(Bounds2D bounds)
		{
			_value = bounds;
			_hasValue = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Bounds2D other) => _hasValue && _value.Equals(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(NullableBounds2D other)
		{
			if (!_hasValue || !other._hasValue)
				return !_hasValue && !other._hasValue;
			return _value.Equals(other._value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object obj) => obj is NullableBounds2D other && Equals(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => !_hasValue ? _hasValue.GetHashCode() : _value.GetHashCode();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(NullableBounds2D lhs, NullableBounds2D rhs)
		{
			if (lhs._hasValue != rhs._hasValue)
				return false;
			if (!lhs._hasValue) // Both don't have a value.
				return true;
			return lhs._value == rhs._value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(NullableBounds2D lhs, NullableBounds2D rhs)
		{
			// Returns true in the presence of NaN values.
			return !(lhs == rhs);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(NullableBounds2D lhs, Bounds2D rhs)
		{
			if (!lhs._hasValue)
				return false;
			// Returns false in the presence of NaN values.
			return lhs._value == rhs;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(NullableBounds2D lhs, Bounds2D rhs)
		{
			// Returns true in the presence of NaN values.
			return !(lhs == rhs);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetMinMax(Vector2 min, Vector2 max)
		{
			_value.SetMinMax(min, max);
			_hasValue = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(Bounds2D bounds)
		{
			if (!_hasValue)
				Value = bounds;
			else
				_value.Encapsulate(bounds);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(NullableBounds2D bounds)
		{
			if (!bounds._hasValue)
				return;

			if (!_hasValue)
				Value = bounds._value;
			else
				_value.Encapsulate(bounds._value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(Vector2 point)
		{
			if (!_hasValue)
				Value = new Bounds2D(point, Vector2.zero);
			else
				_value.Encapsulate(point);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(Vector2 point) => _hasValue && _value.Contains(point);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetSignedDistance(Vector2 point, out float distance)
		{
			if (!_hasValue)
			{
				distance = default;
				return false;
			}

			distance = _value.SignedDistance(point);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float SignedDistance(Vector2 point)
		{
			if (!_hasValue)
			{
				Debug.LogWarning(
					$"{nameof(SignedDistance)} was called on a {nameof(NullableBounds2D)} without a bounds value assigned. " +
					$"Consider using {nameof(SignedDistance)} or checking {nameof(HasValue)} before calling the method.");
				return 0;
			}

			return _value.SignedDistance(point);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetClosestPoint(Vector2 query, out Vector2 point)
		{
			if (!_hasValue)
			{
				point = default;
				return false;
			}

			point = _value.ClosestPoint(query);
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Vector2 ClosestPoint(Vector2 point)
		{
			if (!_hasValue)
			{
				Debug.LogWarning(
					$"{nameof(ClosestPoint)} was called on a {nameof(NullableBounds2D)} without a bounds value assigned. " +
					$"Consider using {nameof(TryGetClosestPoint)} or checking {nameof(HasValue)} before calling the method.");
				return Vector2.zero;
			}

			return _value.ClosestPoint(point);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Expand(float amount)
		{
			if (!_hasValue)
			{
				Debug.LogWarning(
					$"{nameof(Expand)} was called on a {nameof(NullableBounds2D)} without a bounds value assigned. Check {nameof(HasValue)} before calling the method.");
				Value = default;
			}

			_value.Expand(amount);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Expand(Vector2 amount)
		{
			if (!_hasValue)
			{
				Debug.LogWarning(
					$"{nameof(Expand)} was called on a {nameof(NullableBounds2D)} without a bounds value assigned. Assign a default bounds before calling the method.");
				Value = default;
			}

			_value.Expand(amount);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// Does another bounding box intersect with this bounding box?
		public bool Intersects(Bounds2D bounds) => _hasValue && _value.Intersects(bounds);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectRay(Ray ray) => _hasValue && _value.IntersectRay(ray);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectRay(Ray ray, out float distance)
		{
			distance = 0;
			return _hasValue && _value.IntersectRay(ray, out distance);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator NullableBounds2D(Bounds2D bounds) => new NullableBounds2D(bounds);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override string ToString() => !_hasValue ? "Null" : _value.ToString();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToString(string format) => !_hasValue ? "Null" : _value.ToString(format);

#if UNITY_2020_1_OR_NEWER
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public string ToString(string format, IFormatProvider formatProvider) => !_hasValue ? "Null" : _value.ToString(format, formatProvider);
#endif
	}
}