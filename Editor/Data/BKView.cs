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

		// display name
		[JsonProperty("title")] public string Title { get; private set; }
		[JsonProperty("key")] private string _key;
		[JsonProperty("icon")] private string _iconRef; // guid/name
		[JsonProperty("sort")] private BKSort _sort;
		[JsonProperty("rules")] private BKFilterRule[] _filters;
		[JsonProperty("op")] private string _op; // & or |

		// lazily loaded icon asset
		private CachedLoad<Texture> _icon;

		public BKViewFilter()
		{
			_icon = new CachedLoad<Texture>(LoadIcon);
		}

		private Texture LoadIcon() => BKUtils.LoadTexture(_iconRef);

		public bool Contains(BKItem asset)
		{
			if (_filters == null)
			{
				return false;
			}

			if (_op == "|")
			{
				foreach (var f in _filters)
				{
					if (f.IsMatch(asset))
					{
						return true;
					}
				}
				return false;
			}

			foreach (var f in _filters)
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