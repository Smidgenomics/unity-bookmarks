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
		public bool AddGUID(string guid)
		{
			if (guid.IsNullOrEmpty())
			{
				return false;
			}
			return _guids.Add(guid);
		}

		public bool AddGUIDs(IEnumerable<string> guids)
		{
			var changed = false;
			foreach(var g in guids)
			{
				changed |= AddGUID(g);
			}
			return changed;
		}
		public bool RemoveGUID(string guid)
		{
			return _guids.Remove(guid);
		}

		//  Find all asset guids
		public HashSet<BKAsset> FindAssets()
		{
			HashSet<BKAsset> assets = new();

			foreach(var guid in _guids)
			{
				BKAsset a = guid;
				if (a == null)
				{
					continue;
				}
				assets.Add(a);
			}
			foreach(var q in _find)
			{
				foreach(var guid in q.GetGUIDs())
				{
					assets.Add(guid);
				}
			}
			return assets;
		}

		// explicit asset GUIDs to include in list
		[JsonProperty("guids")] private HashSet<string> _guids = new();

		// find queries
		[JsonProperty("find")] private BKQuery[] _find = Array.Empty<BKQuery>();
	}
}