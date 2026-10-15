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
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class BKProfile
	{
		public const string FILE_EXT = "bk.json";
		public const string FILE_PATTERN = "*." + FILE_EXT;

		public bool IsInvalid => _deleted;

		public static IBKReader GetDefaultReader() => EmptyReader.Instance;

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

		public void AddGUID(string guid)
		{
			if (Include.AddRef(guid))
			{
				Save();
			}
		}

		public void AddGUIDs(IEnumerable<string> guids)
		{
			if (Include.AddRefs(guids))
			{
				Save();
			}
		}

		public void RemoveGUID(string guid)
		{
			if (Include.RemoveRef(guid))
			{
				Save();
			}
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
			var settings = JsonConvert.DeserializeObject<BKProfile>(json);
			settings.FilePath = $"{BKConstants.PROJECT_ROOT}/{path}";
			settings.ProjectPath = path;
			return settings;
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
				(new JsonSerializer()).Serialize(jtw, this);
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

		// Display text, used for window tab if set
		[JsonProperty("title")] public string Title { get; private set; }

		[JsonProperty("defaultSort")] private BKSort _defaultSort;

		[JsonProperty("fileIcons", NullValueHandling = NullValueHandling.Ignore)]
		private Dictionary<string, BKIconSettings> _fileIcons = new();

		// Filtered views
		[JsonProperty("filters")] public BKViewFilter[] Filters { get; private set; } = Array.Empty<BKViewFilter>();

		// Assets to include
		[JsonProperty("include")] public BKInclude Include { get; private set; } = new ();

		// GUID of display icon for view
		[JsonProperty("icon")] private string _iconGUID;

		public Texture ProfileIcon => _icon.Value;
		private CachedLoad<Texture> _icon;
		private FileChangeHandle _changeHandle;

		public IBKReader GetReader()
		{
			_reader ??= new ReadHandle
			{
				getFn = GetAllAssets,
				dateFn = GetLastFileEdit,
				reloadFn = Reload
			};
			return _reader;
		}

		private ReadHandle _reader;
		private long _lastWrite;
		private bool _deleted;

		private void Reload()
		{
			var lastEdit = GetLastFileEdit();
			// nothing to reload
			if (lastEdit == _lastWrite)
			{
				return;
			}
			_lastWrite = lastEdit;
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

		private sealed class EmptyReader : IBKReader
		{
			public static readonly EmptyReader Instance = new ();
			private EmptyReader(){}
			public IEnumerable<BKItem> Read() => Array.Empty<BKItem>();
		}

		private sealed class ReadHandle : IBKReader
		{
			public Action reloadFn;
			public Func<long> dateFn;
			public Func<IEnumerable<BKItem>> getFn;

			private FileChangeHandle _changeHandle;
			private long _lastDate;
			private IEnumerable<BKItem> _cachedAssets = Array.Empty<BKItem>();

			public IEnumerable<BKItem> Read()
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