// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System.Threading;
	using System.Text;

	/// <summary>
	/// Bookmark extensions
	/// </summary>
	internal static class Bookmarks_
	{
		public static string SnakeToPascal(this string str)
		{
			string titleCase = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(str.Replace('_', ' '));
			return titleCase.Replace(" ", string.Empty);
		}

		// shorthand for string.IsNullOrEmpty(s)
		public static bool IsNullOrEmpty(this string s) => string.IsNullOrEmpty(s);

		public static string ToSentence(this string s)
		{
			var sb = new StringBuilder();
			for(var i = 0; i < s.Length; i++)
			{
				var c = s[i];
				if(i > 0)
				{
					if (char.IsLower(s[i - 1]) && char.IsUpper(s[i]))
					{
						sb.Append(" ");
					}
				}
				sb.Append(c);
			}
			return sb.ToString();
		}
	}
}