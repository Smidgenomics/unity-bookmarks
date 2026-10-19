// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using UnityEngine;
	using System.Text.RegularExpressions;

	/// <summary>
	/// Bookmark extensions
	/// </summary>
	internal static class Bookmarks_
	{
		public static bool IsGUID(this string str)
		{
			return str is { Length:32 }
			&& _GUID_REGEX.IsMatch(str);
		}

		// shorthand for string.IsNullOrEmpty(s)
		public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);

		public static void CopyToClipboard(this string s) => GUIUtility.systemCopyBuffer = s;

		private static readonly Regex _GUID_REGEX = new ("^([a-f]|[0-9]){32}$");
	}
}