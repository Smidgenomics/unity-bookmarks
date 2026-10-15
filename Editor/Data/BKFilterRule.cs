// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using System.Text.RegularExpressions;
	using UnityEngine;

	internal sealed class BKFilterRule
	{
		public bool IsValid => _fn != null;

		public BKFilterRule(string rule)
		{
			if (rule == null || rule.Length < 3)
			{
				return;
			}

			_invert = rule[0] == '!';

			if (_invert)
			{
				rule = rule.Substring(1);
			}
			
			var str = rule[2..];
		
			_regex = GetRegex(str);

			if (rule.StartsWith("t:"))
			{
				if (_regex != null)
				{
					_fn = MatchTypeRegex;
				}
				else
				{
					_type = Type.GetType(str);
					if (_type != null)
					{
						_fn = MatchType;
					}
				}
			}
			else if (rule.StartsWith("p:"))
			{
				if (_regex != null)
				{
					_fn = MatchPath;
				}
			}
		}
		
		public static implicit operator BKFilterRule(string filter)
		{
			return new BKFilterRule(filter);
		}
		
		private readonly Regex _regex;
		private readonly Type _type;
		private readonly Func<BKItem, bool> _fn = GetFalse;
		private readonly bool _invert;
		public bool IsMatch(BKItem a)
		{
			if (_fn == null)
			{
				return false;
			}
			var r = _fn.Invoke(a);
			if (_invert)
			{
				r = !r;
			}
			return r;
		}

		private static bool GetFalse(BKItem _) => false;
		
		private bool MatchTypeRegex(BKItem a)
		{
			return _regex.IsMatch(a.MainType.FullName!);
		}

		private bool MatchType(BKItem a)
		{
			return _type.IsAbstract
			? _type.IsAssignableFrom(a.MainType)
			: a.MainType == _type;
		}

		private bool MatchPath(BKItem a)
		{
			return _regex.IsMatch(a.FilePath);
		}

		private static Regex GetRegex(string rule)
		{
			if (rule.Contains('*'))
			{
				return new Wildcard(rule);
			}
			if (rule.StartsWith('^'))
			{
				return new Regex(rule);
			}
			return null;
		}
	}
}