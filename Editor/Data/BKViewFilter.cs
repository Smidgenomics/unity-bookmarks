// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using Newtonsoft.Json;
	using System;
	using UnityEngine;

	/// <summary>
	/// Filters out assets covered by bookmarks profile
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]	
	internal sealed class BKViewFilter
	{
		// optional display icon
		public Texture Icon => _icon.Value;
		public BKSort Sort => _sort;
		public string ViewKey => _key ?? Title; // view key, serialized
		public string Title => _title;

		// display name
		[JsonProperty("key")] private string _key;
		[JsonProperty("title")] private string _title;
		[JsonProperty("icon")] private string _iconRef; // guid/name
		[JsonProperty("sort")] private BKSort _sort;
		[JsonProperty("op")] private string _op; // & or |
		[JsonProperty("rules")] private BKFilterRule[] _rules;

		// lazily loaded icon asset
		private CachedLoad<Texture> _icon;

		public BKViewFilter()
		{
			_icon = new CachedLoad<Texture>(LoadIcon);
		}

		private Texture LoadIcon()
		{
			return BKUtils.LoadTexture(_iconRef);
		}

		public bool Contains(BKItem asset)
		{
			if (_rules == null)
			{
				return false;
			}

			if (_op == "|")
			{
				foreach (var f in _rules)
				{
					if (f.IsMatch(asset))
					{
						return true;
					}
				}
				return false;
			}

			foreach (var f in _rules)
			{
				if (f is not { IsValid: true })
				{
					continue;
				}
				if (!f.IsMatch(asset))
				{
					return false;
				}
			}
			return true;
		}

	}
}