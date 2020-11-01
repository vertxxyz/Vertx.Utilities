using System;
using UnityEngine;

namespace Vertx.Utilities
{
	public static class RotationUtils
	{
		/// <summary>
		/// Rotates a rotation in place by an angle-axis rotation.
		/// </summary>
		/// <param name="rotation">The rotation to rotate</param>
		/// <param name="angle">Angle to rotate by in degrees</param>
		/// <param name="axis">Axis to rotate around</param>
		/// <param name="space">Coordinate space to rotate in. Local space is relative to <see cref="rotation"/></param>
		public static void RotateRef(this ref Quaternion rotation, float angle, Vector3 axis, Space space = Space.World)
		{
			switch (space)
			{
				case Space.World:
					rotation = Quaternion.AngleAxis(angle, axis) * rotation;
					break;
				case Space.Self:
					rotation = Quaternion.AngleAxis(angle, rotation * axis) * rotation;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(space), space, null);
			}
		}
		
		/// <summary>
		/// Returns a rotation modified by an angle-axis rotation.
		/// </summary>
		/// <param name="rotation">The rotation to rotate</param>
		/// <param name="angle">Angle to rotate by in degrees</param>
		/// <param name="axis">Axis to rotate around</param>
		/// <param name="space">Coordinate space to rotate in. Local space is relative to <see cref="rotation"/></param>
		/// <returns>The modified rotation</returns>
		public static Quaternion Rotate(this Quaternion rotation, float angle, Vector3 axis, Space space = Space.World)
		{
			switch (space)
			{
				case Space.World:
					return Quaternion.AngleAxis(angle, axis) * rotation;
				case Space.Self:
					return Quaternion.AngleAxis(angle, rotation * axis) * rotation;
				default:
					throw new ArgumentOutOfRangeException(nameof(space), space, null);
			}
		}
	}
}