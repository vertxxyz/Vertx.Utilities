using System;
using System.Runtime.CompilerServices;
using UnityEngine;
#if UNITY_MATHEMATICS
using Unity.Mathematics;
#endif

namespace Vertx.Utilities
{
	/// <summary>
	/// <para>
	/// <see cref="Bounds2DInt"/>, but <see cref="NullableBounds2DInt.Encapsulate(Bounds2DInt)"/> and its variants don't expand from the default value when first called.
	/// </para>
	/// If a value has not yet been assigned methods like Intersects, IntersectRay, Contains, will return false.
	///	Other query methods have `TryGet` alternatives.
	/// </summary>
	public struct NullableBounds2DInt : IEquatable<Bounds2DInt>, IEquatable<NullableBounds2DInt>
#if UNITY_2020_1_OR_NEWER
		, IFormattable
#endif
	{
		public bool HasValue => _hasValue;

		public Bounds2DInt Value
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
		private Bounds2DInt _value;
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public NullableBounds2DInt(int xMin, int yMin, int sizeX, int sizeY)
		{
			_value = new Bounds2DInt(xMin, yMin, sizeX, sizeY);
			_hasValue = true;
		}

#if UNITY_MATHEMATICS
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public NullableBounds2DInt(int2 position, int2 size)
		{
			_value = new Bounds2DInt(position, size);
			_hasValue = true;
		}
#endif
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public NullableBounds2DInt(Vector2Int position, Vector2Int size)
		{
			_value = new Bounds2DInt(position, size);
			_hasValue = true;
		}

		public NullableBounds2DInt(Bounds2DInt bounds)
		{
			_value = bounds;
			_hasValue = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(Bounds2DInt other) => _hasValue && _value.Equals(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(NullableBounds2DInt other)
		{
			if (!_hasValue || !other._hasValue)
				return !_hasValue && !other._hasValue;
			return _value.Equals(other._value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object obj) => obj is NullableBounds2DInt other && Equals(other);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode() => !_hasValue ? _hasValue.GetHashCode() : _value.GetHashCode();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(NullableBounds2DInt lhs, NullableBounds2DInt rhs)
		{
			if (lhs._hasValue != rhs._hasValue)
				return false;
			if (!lhs._hasValue) // Both don't have a value.
				return true;
			return lhs._value == rhs._value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(NullableBounds2DInt lhs, NullableBounds2DInt rhs)
		{
			// Returns true in the presence of NaN values.
			return !(lhs == rhs);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(NullableBounds2DInt lhs, Bounds2DInt rhs)
		{
			if (!lhs._hasValue)
				return false;
			// Returns false in the presence of NaN values.
			return lhs._value == rhs;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(NullableBounds2DInt lhs, Bounds2DInt rhs)
		{
			// Returns true in the presence of NaN values.
			return !(lhs == rhs);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetMinMax(Vector2Int min, Vector2Int max)
		{
			if (!_hasValue)
			{
				Debug.LogWarning($"{nameof(SetMinMax)} was called on a {nameof(NullableBounds2DInt)} without a bounds value assigned.");
				return;
			}
			
			_value.SetMinMax(min, max);
			_hasValue = true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(Bounds2DInt bounds)
		{
			if (!_hasValue)
				Value = bounds;
			else
				_value.Encapsulate(bounds);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(NullableBounds2DInt bounds)
		{
			if (!bounds._hasValue)
				return;

			if (!_hasValue)
				Value = bounds._value;
			else
				_value.Encapsulate(bounds._value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(Vector2Int point)
		{
			if (!_hasValue)
				Value = new Bounds2DInt(point, Vector2Int.zero);
			else
				_value.Encapsulate(point);
		}
		
#if UNITY_MATHEMATICS
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void SetMinMax(int2 min, int2 max) => SetMinMax(new Vector2Int(min.x, min.y), new Vector2Int(max.x, max.y));
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Encapsulate(int2 point) => Encapsulate(new Vector2Int(point.x, point.y));
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(int2 point) => Contains(new Vector2Int(point.x, point.y));
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetSignedDistance(int2 point, out float distance) => TryGetSignedDistance(new Vector2Int(point.x, point.y), out distance);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public float SignedDistance(int2 point) => SignedDistance(new Vector2Int(point.x, point.y));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetClosestPoint(int2 query, out Vector2 point) => TryGetClosestPoint(new Vector2(query.x, query.y), out point);
		
		public void Expand(int2 amount) => Expand(new Vector2Int(amount.x, amount.y));
		
		public void AddSize(int2 amount) => AddSize(new Vector2Int(amount.x, amount.y));
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(Vector2Int point) => _hasValue && _value.Contains(point);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetSignedDistance(Vector2Int point, out float distance)
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
		public float SignedDistance(Vector2Int point)
		{
			if (!_hasValue)
			{
				Debug.LogWarning(
					$"{nameof(SignedDistance)} was called on a {nameof(NullableBounds2DInt)} without a bounds value assigned. " +
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
					$"{nameof(ClosestPoint)} was called on a {nameof(NullableBounds2DInt)} without a bounds value assigned. " +
					$"Consider using {nameof(TryGetClosestPoint)} or checking {nameof(HasValue)} before calling the method.");
				return Vector2.zero;
			}

			return _value.ClosestPoint(point);
		}

		/// <summary>
		/// <see cref="Bounds2DInt"/> Expand does not work like <see cref="Bounds2D"/>, it will expand size by amount * 2, and increase the size on both sides.<br/>
		/// If it didn't expand * 2 it would have to move the center point to a non-integer value.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Expand(int amount)
		{
			if (!_hasValue)
			{
				Debug.LogWarning(
					$"{nameof(Expand)} was called on a {nameof(NullableBounds2DInt)} without a bounds value assigned. Check {nameof(HasValue)} before calling the method.");
				Value = default;
			}

			_value.Expand(amount);
		}

		/// <summary>
		/// <see cref="Bounds2DInt"/> Expand does not work like <see cref="Bounds2D"/>, it will expand size by amount * 2, and increase the size on both sides.<br/>
		/// If it didn't expand * 2 it would have to move the center point to a non-integer value.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Expand(Vector2Int amount)
		{
			if (!_hasValue)
			{
				Debug.LogWarning(
					$"{nameof(Expand)} was called on a {nameof(NullableBounds2DInt)} without a bounds value assigned. Assign a default bounds before calling the method.");
				Value = default;
			}

			_value.Expand(amount);
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddSize(Vector2Int amount)
		{
			if (!_hasValue)
			{
				Debug.LogWarning(
					$"{nameof(Expand)} was called on a {nameof(NullableBounds2DInt)} without a bounds value assigned. Assign a default bounds before calling the method.");
				Value = default;
			}

			_value.AddSize(amount);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void AddSize(int amount) => AddSize(new Vector2Int(amount, amount));
		

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		// Does another bounding box intersect with this bounding box?
		public bool Intersects(Bounds2DInt bounds) => _hasValue && _value.Intersects(bounds);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectRay(Ray ray) => _hasValue && _value.IntersectRay(ray);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool IntersectRay(Ray ray, out float distance)
		{
			distance = 0;
			return _hasValue && _value.IntersectRay(ray, out distance);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static implicit operator NullableBounds2DInt(Bounds2DInt bounds) => new NullableBounds2DInt(bounds);

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