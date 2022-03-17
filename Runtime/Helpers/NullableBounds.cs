using System;
using UnityEngine;

namespace Vertx.Utilities
{
    /// <summary>
    /// <para>
    /// <see cref="Bounds"/>, but <see cref="NullableBounds.Encapsulate(Bounds)"/> and its variants don't expand from the default value when first called.
    /// </para>
    /// If a value has not yet been assigned methods like Intersects, IntersectRay, Contains, will return false.
    ///	Other query methods have `TryGet` alternatives.
    /// </summary>
    public struct NullableBounds : IEquatable<Bounds>, IEquatable<NullableBounds>
#if UNITY_2021_1_OR_NEWER
        , IFormattable
#endif
    {
        public bool HasValue => _hasValue;

        public Bounds Value
        {
            get => _value;
            set
            {
                _value = value;
                _hasValue = true;
            }
        }

        private bool _hasValue;
        private Bounds _value;

        public NullableBounds(Vector3 center, Vector3 size)
        {
            _value = new Bounds(center, size * 0.5f);
            _hasValue = true;
        }

        public NullableBounds(Bounds bounds)
        {
            _value = bounds;
            _hasValue = true;
        }

        public bool Equals(Bounds other) => _hasValue && _value.Equals(other);

        public bool Equals(NullableBounds other)
        {
            if (!_hasValue || !other._hasValue)
                return !_hasValue && !other._hasValue;
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj) => obj is NullableBounds other && Equals(other);

        public override int GetHashCode() => !_hasValue ? _hasValue.GetHashCode() : _value.GetHashCode();

        public static bool operator ==(NullableBounds lhs, Bounds rhs)
        {
            if (!lhs._hasValue)
                return false;
            // Returns false in the presence of NaN values.
            return lhs._value == rhs;
        }

        public static bool operator !=(NullableBounds lhs, Bounds rhs)
        {
            // Returns true in the presence of NaN values.
            return !(lhs == rhs);
        }

        public void SetMinMax(Vector3 min, Vector3 max)
        {
            _value.SetMinMax(min, max);
            _hasValue = true;
        }

        public void Encapsulate(Bounds bounds)
        {
            if (!_hasValue)
                Value = bounds;
            else
                _value.Encapsulate(bounds);
        }

        public void Encapsulate(NullableBounds bounds)
        {
            if (!bounds._hasValue)
                return;

            if (!_hasValue)
                Value = bounds._value;
            else
                _value.Encapsulate(bounds._value);
        }

        public void Encapsulate(Vector3 point)
        {
            if (!_hasValue)
                Value = new Bounds(point, Vector3.zero);
            else
                _value.Encapsulate(point);
        }

        public bool Contains(Vector3 point) => _hasValue && _value.Contains(point);

        public bool TryGetSqrDistance(Vector3 point, out float distance)
        {
            if (!_hasValue)
            {
                distance = default;
                return false;
            }

            distance = _value.SqrDistance(point);
            return true;
        }

        public float SqrDistance(Vector3 point)
        {
            if (!_hasValue)
            {
                Debug.LogWarning(
                    $"{nameof(SqrDistance)} was called on a {nameof(NullableBounds)} without a bounds value assigned. " +
                    $"Consider using {nameof(TryGetSqrDistance)} or checking {nameof(HasValue)} before calling the method.");
                return 0;
            }

            return _value.SqrDistance(point);
        }

        public bool TryGetClosestPoint(Vector3 query, out Vector3 point)
        {
            if (!_hasValue)
            {
                point = default;
                return false;
            }

            point = _value.ClosestPoint(query);
            return true;
        }

        public Vector3 ClosestPoint(Vector3 point)
        {
            if (!_hasValue)
            {
                Debug.LogWarning(
                    $"{nameof(ClosestPoint)} was called on a {nameof(NullableBounds)} without a bounds value assigned. " +
                    $"Consider using {nameof(TryGetClosestPoint)} or checking {nameof(HasValue)} before calling the method.");
                return Vector3.zero;
            }

            return _value.ClosestPoint(point);
        }

        public void Expand(float amount)
        {
            if (!_hasValue)
            {
                Debug.LogWarning(
                    $"{nameof(Expand)} was called on a {nameof(NullableBounds)} without a bounds value assigned. Check {nameof(HasValue)} before calling the method.");
                Value = default;
            }

            _value.Expand(amount);
        }

        public void Expand(Vector3 amount)
        {
            if (!_hasValue)
            {
                Debug.LogWarning(
                    $"{nameof(Expand)} was called on a {nameof(NullableBounds)} without a bounds value assigned. Assign a default bounds before calling the method.");
                Value = default;
            }

            _value.Expand(amount);
        }

        // Does another bounding box intersect with this bounding box?
        public bool Intersects(Bounds bounds) => _hasValue && _value.Intersects(bounds);

        public bool IntersectRay(Ray ray) => _hasValue && _value.IntersectRay(ray);

        public bool IntersectRay(Ray ray, out float distance)
        {
            distance = 0;
            return _hasValue && _value.IntersectRay(ray, out distance);
        }

        public static implicit operator NullableBounds(Bounds bounds) => new NullableBounds(bounds);

        public override string ToString() => !_hasValue ? "Null" : _value.ToString();
        
        public string ToString(string format) => !_hasValue ? "Null" : _value.ToString(format);

#if UNITY_2021_1_OR_NEWER
        public string ToString(string format, IFormatProvider formatProvider) => !_hasValue ? "Null" : _value.ToString(format, formatProvider);
#endif
    }
}