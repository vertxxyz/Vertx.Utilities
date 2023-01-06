using System;
using System.Diagnostics;

namespace Vertx.Utilities
{
	public static class Utils
	{
		/// <summary>
		/// A call to a lambda that will be stripped in builds.<br/>
		/// Note that functions that produce variables that are captured by the action will likely not be stripped.<br/>
		/// Don't use this function if you are not using a lambda, in that case stripping will not properly occur.
		/// </summary>
		/// <param name="action">Action that runs only in the editor</param>
		[Conditional("UNITY_EDITOR")]
		public static void EditorOnly(Action action) => action();

		/// <summary>
		/// A call to a lambda that will be stripped in release builds.<br/>
		/// Note that functions that produce variables that are captured by the action will likely not be stripped.<br/>
		/// Don't use this function if you are not using a lambda, in that case stripping will not properly occur.
		/// </summary>
		/// <param name="action">Action that runs only in the editor</param>
		[Conditional("UNITY_EDITOR"), Conditional("DEBUG")]
		public static void DebugOnly(Action action) => action();
	}
}