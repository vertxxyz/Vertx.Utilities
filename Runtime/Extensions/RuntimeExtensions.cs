using UnityEngine;

namespace Vertx.Utilities
{
	public static class RuntimeExtensions
	{
		private const string cloneText = "(Clone)";

		/// <summary>
		/// Trims appended text from Object names. Will trim (Clone) by default.
		/// This is not recursive, and only will remove a single instance of the word.
		/// </summary>
		/// <param name="object">The Object to trim the name on.</param>
		/// <param name="trimmedText">The text to trim. This is (Clone) by default.</param>
		public static void TrimName(this Object @object, string trimmedText = cloneText)
		{
			string name = @object.name;
			if (name.EndsWith(trimmedText))
				@object.name = name.Substring(0, name.Length - trimmedText.Length);
		}
	}
}