// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System.Collections.Generic;
	using System.Text.RegularExpressions;

	[System.Serializable]
	internal sealed class IconRules
	{
		public IconRules(Dictionary<string, BKIconSettings> rules)
		{
			rules ??= new();
			foreach (var (r, s) in rules)
			{
				if (s == null || string.IsNullOrEmpty(r))
				{
					continue;
				}
				_regex.Add(RegexFromRule(r));
				_icons.Add(new CachedLoad<LoadedIcon>(() => s.GetIcon()));
			}
		}

		public bool TryGetIconByPath(string path, out LoadedIcon icon)
		{
			icon = default;
			for (var i = 0; i < _regex.Count; i++)
			{
				if (_regex[i].IsMatch(path))
				{
					icon = _icons[i].Value;
					return true;
				}
			}
			return false;
		}

		private readonly List<Regex> _regex = new();
		private readonly List<CachedLoad<LoadedIcon>> _icons = new();

		private static Regex RegexFromRule(string rule)
		{
			if (rule.StartsWith('^'))
			{
				return new Regex(rule);
			}
			
			// TODO: match by type
			if (rule.Contains('*'))
			{
				return new Wildcard(rule);
			}

			// backwards compatibility fix, make old extension scheme still work
			return new Wildcard("*." + rule);
		}
	}
}