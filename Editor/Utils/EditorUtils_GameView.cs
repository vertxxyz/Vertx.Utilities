using System;
using System.Reflection;
using Vertx.Utilities.Editor.Internal;

namespace Vertx.Utilities.Editor
{
	public static partial class EditorUtils
	{
		/// <summary>
		/// Tries to add a <see cref="UnityEditor.GameViewSize"/> to the GameView. If it's already present in the list, nothing will be added.<br/>
		/// The "current group" can change based on platform, so be aware that the size added here is not shared across all groups.
		/// </summary>
		/// <returns>True if the size was added</returns>
		public static bool TryAddGameViewSizeToCurrentGroup(GameViewSizeUnchecked gameViewSize)
			=> InternalExtensions.TryAddCustomGameViewSizeForCurrentGroup(new InternalExtensions.GameViewSizeInternal(
				gameViewSize.BaseText,
				gameViewSize.SizeType == GameViewSizeUnchecked.Type.AspectRatio ? InternalExtensions.GameViewSizeInternal.Type.AspectRatio : InternalExtensions.GameViewSizeInternal.Type.FixedResolution,
				gameViewSize.Width,
				gameViewSize.Height));
	}

	/// <summary>
	/// <see cref="UnityEditor.GameViewSize"/>, but the values are entirely unchecked.<br/>
	/// Values will be checked when re-assigned to internal functions and may change to be clamped or modified.
	/// </summary>
	public readonly struct GameViewSizeUnchecked
	{
		public enum Type
		{
			AspectRatio,
			FixedResolution
		}

		public readonly string BaseText;
		public readonly Type SizeType;
		public readonly int Width;
		public readonly int Height;

		public GameViewSizeUnchecked(string baseText, Type sizeType, int width, int height)
		{
			BaseText = baseText;
			SizeType = sizeType;
			Width = width;
			Height = height;
		}

		public enum AspectReduction
		{
			Default,
			Normalised
		}

		public GameViewSizeUnchecked(Type sizeType, int width, int height, AspectReduction aspectMethod = AspectReduction.Default)
		{
			(width, height) = ReduceAspect(width, height);
			string baseText;
			switch (sizeType)
			{
				case Type.AspectRatio:
					switch (aspectMethod)
					{
						case AspectReduction.Default:
						{
							baseText = $"{width}:{height} Aspect";
							break;
						}
						case AspectReduction.Normalised:
						{
							(float x, float y) = ReduceAspectNormalised(width, height);
							baseText = $"{x:0.##}:{y:0.##} Aspect";
							break;
						}
						default:
							throw new ArgumentOutOfRangeException(nameof(aspectMethod), aspectMethod, null);
					}

					break;
				case Type.FixedResolution:
					baseText = "";
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(sizeType), sizeType, null);
			}

			BaseText = baseText;
			SizeType = sizeType;
			Width = width;
			Height = height;
		}

		/// <summary>
		/// Returns an aspect reduced to the smallest whole numbers
		/// 32 : 18 -> 16 : 9 for example
		/// </summary>
		public static (int x, int y) ReduceAspect(int x, int y)
		{
			x = Math.Max(0, x);
			y = Math.Max(0, y);
			int gcd = (int)Gcd((uint)x, (uint)y);
			return (x / gcd, y / gcd);
		}

		/// <summary>
		/// Returns an aspect reduced such that the smaller value is set to 1
		/// 16 : 9 -> 1.77777777778 : 1 for example
		/// </summary>
		public static (float x, float y) ReduceAspectNormalised(int x, int y)
		{
			if (x == y)
				return (1, 1);
			if (x > y)
				return (x / (float)y, 1);
			return (1, y / (float)x);
		}

		private static uint Gcd(uint a, uint b)
		{
			while (a != 0 && b != 0)
			{
				if (a > b)
					a %= b;
				else
					b %= a;
			}

			return a | b;
		}
	}
}