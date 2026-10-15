// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using UnityEngine;
	using System.Linq;
	using System.Collections.Generic;

	/// <summary>
	/// Wrapper around bookmarks fetching, because tracking it in the UI is a cock
	/// </summary>
	[Serializable]
	internal sealed class BKUser
	{
		private const string DEF_TITLE = "Bookmarks";
		private const string DEF_FILE = "ProjectSettings/bookmarks/default.bk.json";

		public event Action viewChanged;

		public void AddGUID(string guid)
		{
			Profile?.AddGUID(guid);
		}

		public void AddGUIDs(string[] guids)
		{
			Profile?.AddGUIDs(guids);
		}

		public void RemoveGUID(string guid)
		{
			Profile?.RemoveGUID(guid);
		}

		public bool HasProfile => Profile is { IsInvalid: false };
		public string ProfilePath => _filePath;

		public string FilterString
		{
			get => _filterString;
			set => UpdateFilter(value);
		}

		private BKProfile Profile => GetProfile();

		public IEnumerable<BKItem> Assets => GetAssets();

		public BKViewFilter CurrentFilter => GetCurrentFilter();

		public IEnumerable<BKViewFilter> Filters => ViewCount > 0 ? Profile.Filters : Array.Empty<BKViewFilter>();

		public string FilterKey
		{
			get => _viewKey;
			set => SetFilter(value);
		}

		public int ViewCount => Profile?.Filters?.Length ?? 0;

		public void Load(string path)
		{
			if (path == _filePath)
			{
				return;
			}
			LoadPath(path);
		}

		public void Reload()
		{
			if (string.IsNullOrEmpty(_filePath))
			{
				return;
			}
			LoadPath(_filePath); // load from scratch
		}

		public void Edit()
		{
			if (_filePath == null)
			{
				return;
			}
			BKUtils.OpenProjectFile(_filePath);
		}

		public string GetDisplayTitle()
		{
			// view -> profile -> default
			var title = CurrentFilter?.Title ?? Profile?.GetDisplayTitle();
			return !title.IsNullOrEmpty() ? title : DEF_TITLE;
		}
		public Texture GetDisplayIcon()
		{
			return CurrentFilter?.Icon ?? Profile?.ProfileIcon ?? DEFAULT_ICON.Value;
		}

		private static readonly Lazy<Texture> DEFAULT_ICON = new(() =>
		{
			return BKUtils.LoadTexture(BKConstants.ICO_GUID_WINDOW);
		});

		[SerializeField] private string _filePath = DEF_FILE; // path to bookmarks file relative to project folder
		[SerializeField] private string _viewKey; // view key, if it exists
		[SerializeField] private string _filterString = string.Empty;
		[SerializeField] private string[] _filterParts = Array.Empty<string>();

		private void UpdateFilter(string filterStr)
		{
			_filterString = filterStr.ToLower();
			_filterParts = _filterString.Split(' ');
			for (var i = 0; i < _filterParts.Length; i++)
			{
				_filterParts[i] = _filterParts[i].Trim();
			}
			viewChanged?.Invoke();

		}

		private BKProfile GetProfile()
		{
			// safety check - loading profile might have failed
			var p = _getFn.Invoke(this);
			if(p is { IsInvalid: true })
			{
				Debug.LogError($"Loading bookmarks file failed: '{p.ProjectPath}'");
				LoadPath(null);
				return null;
			}
			return p;
		}

		private BKProfile _profile; // currently loaded profile
		private Func<BKUser, BKProfile> _getFn = InitBK;

		private bool Filter(BKViewFilter view, BKItem a)
		{
			if (view != null && !view.Contains(a))
			{
				return false;
			}

			if (_filterParts.Length > 0)
			{
				var name = a.FileName;

				foreach (var p in _filterParts)
				{
					if (name.Contains(p))
					{
						return true;
					}
				}
				return false;
			}
			return true;
		}

		private IEnumerable<BKItem> GetAssets()
		{
			if (Profile == null)
			{
				return Array.Empty<BKItem>();
			}

			var reader = Profile.GetReader();
			var currentView = GetCurrentFilter();

			var r = reader.Read()
			.Where(a => a && Filter(currentView, a));

			var sort = currentView?.Sort ?? Profile.DefaultSort;

			r = sort.Descending
			? r.OrderByDescending(x => sort.OrderBy(x))
			: r.OrderBy(x => sort.OrderBy(x));

			return r;
		}

		private BKViewFilter GetCurrentFilter()
		{
			var i = IndexOfView(_viewKey);
			return i == -1 ? null : GetFilterAtIndex(i);
		}

		private void SetFilter(string key)
		{
			_viewKey = null;
			if(IndexOfView(key) > -1)
			{
				_viewKey = key; // finally, use key
			}
			viewChanged?.Invoke();
		}

		private BKViewFilter GetFilterAtIndex(int i) => Profile.Filters[i];

		private int IndexOfView(string key)
		{
			for(var i = 0; i < ViewCount; i++)
			{
				var v = GetFilterAtIndex(i);
				if(v.ViewKey == key) return i;
			}
			return -1;
		}

		private void LoadPath(string path)
		{
			// reset state
			ResetState();
			_filePath = path;
			viewChanged?.Invoke();
		}

		private void ResetState()
		{
			_viewKey = null;
			_filePath = null;
			_profile = null;
			// reset lazy loading
			_getFn = InitBK;
		}

		// note: getters are static so they can be used to init getter field

		// plain getter, uses loaded value
		private static BKProfile GetBK(BKUser h) => h._profile;

		// init getter, loads profile
		private static BKProfile InitBK(BKUser h) // init getter
		{
			h._profile = BKProfile.LoadFromPath(h._filePath);
			if(h._profile == null) { h._filePath = null; }
			h._getFn = GetBK; // uses loaded value


			h.viewChanged?.Invoke();

			return h._profile;
		}
	}
}