// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using Newtonsoft.Json;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using UnityEngine;

	/// <summary>
	/// Bookmarks file
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
	internal sealed class BKProfile
	{
		public const string FILE_EXT = "bk.json";
		public const string FILE_PATTERN = "*." + FILE_EXT;

		public bool IsInvalid => _deleted;

		public bool IsStale()
		{
			return GetLastFileEdit() != _lastEdited;
		}

		public bool TryGetIcon(string key, out LoadedIcon icon)
		{
			if (_fileIcons.TryGetValue(key, out var ico))
			{
				icon = ico.GetIcon();
				return true;
			}
			icon = default;
			return false;
		}

		public bool AddRef(string guid)
		{
			if (Include.AddRef(guid))
			{
				Save();
				return true;
			}
			return false;
		}

		public bool AddRefs(IEnumerable<string> guids)
		{
			if (Include.AddRefs(guids))
			{
				Save();
				return true;
			}

			return false;
		}

		public bool RemoveRef(string guid)
		{
			var r = Include.RemoveRef(guid);
			if (r)
			{
				Save();
			}
			return r;
		}

		public BKProfile()
		{
			_icon = new CachedLoad<Texture>(LoadProfileIcon);
		}

		public static BKProfile LoadFromPath(string path)
		{
			if (!File.Exists(path))
			{
				return null;
			}
			var json = File.ReadAllText(path);
			var profile = JsonConvert.DeserializeObject<BKProfile>(json, GetSerializerSettings());
			profile.FilePath = $"{BKConstants.PROJECT_ROOT}/{path}";
			profile._lastEdited = BKUtils.GetLastFileEditTime(profile.FilePath);
			profile.ProjectPath = path;
			return profile;
		}

		private static JsonSerializerSettings _cachedSerializationSettings;

		private static JsonSerializerSettings GetSerializerSettings()
		{
			_cachedSerializationSettings ??= new JsonSerializerSettings
			{
				Converters = new List<JsonConverter>()
				{
					// new JsonToStringConverter(typeof(BKSort))
				}
				// TypeNameHandling = TypeNameHandling.All,
				// MissingMemberHandling = MissingMemberHandling.Ignore,
				// NullValueHandling = NullValueHandling.Ignore
			};
			return _cachedSerializationSettings;
		}

		public static IReadOnlyList<(string, string)> GetProfileOptions()
		{
			List<string> paths = new();
			FindBookmarks("ProjectSettings/bookmarks", paths);
			FindBookmarks("UserSettings/bookmarks", paths);

			var options = new List<(string, string)>();
			
			foreach (var path in paths)
			{
				options.Add((ProfileLabelFromPath(path), path));
			}
			return options;
		}

		private Texture LoadProfileIcon() => BKUtils.LoadTexture(_iconGUID);

		private static string ProfileLabelFromPath(string path)
		{
			return path.Replace("Settings", string.Empty) 
			.Replace("/bookmarks", string.Empty)
			.Replace(".bk.json", string.Empty);
		}

		private static void FindBookmarks(string dir, List<string> paths)
		{
			var files = BKUtils.ListFiles(dir, FILE_PATTERN, true);
			foreach (var uf in files)
			{
				paths.Add(uf.Replace(@"\", "/"));
			}
		}

		// Write profile to file
		public void Save()
		{
			try
			{
				using var fs = File.Create(ProjectPath);
				using var sw = new StreamWriter(fs);
				using var jtw = new JsonTextWriter(sw);
				jtw.Formatting = Formatting.Indented;
				jtw.Indentation = 1;
				jtw.IndentChar = '\t';
				var serializer = new JsonSerializer
				{
					NullValueHandling = NullValueHandling.Ignore
				};
				serializer.Serialize(jtw, this);
			}
			catch(Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		public string GetDisplayTitle()
		{
			if (Title.IsNullOrEmpty())
			{
				var fileName = BKUtils.FileNameFromPath(ProjectPath, true);
				return fileName[..fileName.IndexOf('.')];
			}
			return Title;
		}

		// Relative path from project folder
		public string ProjectPath { get; private set; }

		// Absolute path to file
		public string FilePath { get; private set; }
		
		public BKSort DefaultSort => _defaultSort ?? BKSort.Default;

		public string Title => _title;

		// Display text, used for window tab if set
		// [JsonProperty("title", Order = -1)] public string Title { get; private set; }
		[JsonProperty("title")] private string _title;

		// GUID of display icon for view
		[JsonProperty("icon")] private string _iconGUID;

		[JsonProperty("sort")] private BKSort _defaultSort;

		[JsonProperty("fileIcons")]
		private Dictionary<string, BKIconSettings> _fileIcons = new();

		public BKViewFilter[] Filters => _filters;

		public BKInclude Include => _include;
		
		// Assets to include
		[JsonProperty("include")] private BKInclude _include = new ();

		// Filtered views
		[JsonProperty("filters")] private BKViewFilter[] _filters = Array.Empty<BKViewFilter>();

		


		public Texture ProfileIcon => _icon.Value;
		private CachedLoad<Texture> _icon;
		private FileChangeHandle _changeHandle;

		public IBKItemProvider GetItemProvider()
		{
			_itemProvider ??= new ItemProvider
			{
				getFn = GetAllAssets,
				dateFn = GetLastFileEdit,
				reloadFn = ReloadProfile
			};
			return _itemProvider;
		}

		private ItemProvider _itemProvider;
		private long _lastEdited;
		private bool _deleted;

		private void ReloadProfile()
		{
			var lastEdit = GetLastFileEdit();
			// nothing to reload
			if (lastEdit == _lastEdited)
			{
				return;
			}
			_lastEdited = lastEdit;
			try
			{
				var json = File.ReadAllText(FilePath);
				JsonConvert.PopulateObject(json, this);
			}
			catch
			{
				_deleted = true;
			}
		}

		private IEnumerable<BKItem> GetAllAssets()
		{
			var r = Include.FindAssets();
			foreach (var item in r)
			{
				item.Profile = this;
			}
			return r;
		}

		private long GetLastFileEdit()
		{
			return File.GetLastWriteTime(FilePath).ToFileTime();
		}

		private sealed class DefaultItemProvider : IBKItemProvider
		{
			public static readonly DefaultItemProvider Instance = new ();
			private DefaultItemProvider(){}
			public IEnumerable<BKItem> GetItems() => Array.Empty<BKItem>();
		}

		private sealed class ItemProvider : IBKItemProvider
		{
			public Action reloadFn;
			public Func<long> dateFn;
			public Func<IEnumerable<BKItem>> getFn;

			private FileChangeHandle _changeHandle;
			private long _lastDate;
			private IEnumerable<BKItem> _cachedAssets = Array.Empty<BKItem>();

			public IEnumerable<BKItem> GetItems()
			{
				var latestDate = dateFn.Invoke();
				if(latestDate != _lastDate)
				{
					_lastDate = latestDate;
					reloadFn.Invoke();
					_cachedAssets = getFn.Invoke();
				}
				return _cachedAssets;
			}
		}

	}
}