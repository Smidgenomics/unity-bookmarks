// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using System.Collections.Generic;
	using Newtonsoft.Json;

	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class BKInclude
	{
		public bool AddRef(string guidOrPath)
		{
			if (guidOrPath.IsNullOrEmpty())
			{
				return false;
			}
			return _refs.Add(guidOrPath);
		}

		public bool AddRefs(IEnumerable<string> guidsOrPaths)
		{
			var changed = false;
			foreach(var g in guidsOrPaths)
			{
				changed |= AddRef(g);
			}
			return changed;
		}
		public bool RemoveRef(string guidOrPath)
		{
			return _refs.Remove(guidOrPath);
		}

		//  Find all asset guids
		public HashSet<BKItem> FindAssets()
		{
			HashSet<BKItem> assets = new();

			foreach(var guid in _refs)
			{
				BKItem a = guid;
				if (a == null)
				{
					continue;
				}
				assets.Add(a);
			}
			foreach(var q in _queries)
			{
				foreach(var guid in q.GetIncludes())
				{
					assets.Add(guid);
				}
			}
			return assets;
		}

		// explicit asset GUIDs to include in list
		[JsonProperty("refs")] private HashSet<string> _refs = new();

		// asset find queries
		[JsonProperty("queries")] private BKQuery[] _queries = Array.Empty<BKQuery>();
	}
}