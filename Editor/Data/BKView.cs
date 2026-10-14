// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using Newtonsoft.Json;
	using System.Text.RegularExpressions;
	using System;
	using System.Linq;
	using UnityEngine;

	/// <summary>
	/// Filters out assets covered by bookmarks profile
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]	
	internal sealed class BKView
	{
		// optional display icon
		public Texture Icon => _icon.Value;

		public BKSort Sort => _sort.Value;

		// view key, serialized
		public string Key => _key ?? Label;
	
		// display name
		[JsonProperty("label")] public string Label { get; private set; }

		// Display key
		[JsonProperty("key")] private string _key;
		
		// GUID of display icon for view
		[JsonProperty("icon")] private string _iconGUID;

		// Base types to include
		[JsonProperty("types")] private string[] _typePaths = Array.Empty<string>();

		// Path wildcards
		[JsonProperty("paths")] private string[] _pathPatterns = Array.Empty<string>();

		// lazily loaded icon asset
		private Lazy<Texture> _icon;
		private Lazy<Type[]> _types;
		private Lazy<Regex[]> _paths;

		[JsonProperty("sort")] private string _sortKey = "name";

		private Lazy<BKSort> _sort;

		public BKView()
		{
			_icon = new(() => BKUtils.LoadTexture(_iconGUID));

			_types = new(() =>
			{
				return
				_typePaths.Select(tp => Type.GetType(tp))
				.Where(t => t != null)
				.ToArray();
			});

			_paths = new(() =>
			{
				return
				_pathPatterns.Select(pattern => Wildcard.New(pattern))
				.Where(p => p!= null)
				.ToArray();
			});
			_sort = new(() => _sortKey);
		}

		public bool Contains(BKAsset asset)
		{
			if (!MatchPath(asset.AssetPath)) { return false; }
			if (!MatchType(asset.MainType)) { return false; }
			return true;
		}

		private bool MatchPath(string path)
		{
			if(_paths.Value.Length == 0) { return true; }
			return _paths.Value.FirstOrDefault(r => r.IsMatch(path)) != null;
		}

		private bool MatchType(Type assetType)
		{
			if(_types.Value.Length == 0) { return true; }
			return _types.Value.FirstOrDefault(t => t.IsAssignableFrom(assetType)) != null;
		}

	}
}