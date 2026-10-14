// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System.Collections.Generic;

	/// <summary>
	/// Asset list provider
	/// </summary>
	internal interface IBKReader
	{
		public IEnumerable<BKAsset> Read();
	}
}

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using Newtonsoft.Json;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using UnityEngine;
	using UnityEditor;

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

		public void AddGUID(string guid)
		{
			if (Include.AddGUID(guid))
			{
				Save();
			}
		}

		public void AddGUIDs(IEnumerable<string> guids)
		{
			if (Include.AddGUIDs(guids))
			{
				Save();
			}
		}

		public void RemoveGUID(string guid)
		{
			if (Include.RemoveGUID(guid))
			{
				Save();
			}
		}

		public BKProfile()
		{
			_icon = new(() =>
			{
				if (_iconGUID.IsNullOrEmpty())
				{
					return null;
				}
				// maybe: some kind of alias provider
				var path = AssetDatabase.GUIDToAssetPath(_iconGUID);
				var ico = AssetDatabase.LoadAssetAtPath<Texture>(path);
				return ico ? new GUIContent(ico) : null;
			});
			_sort = new(() => _sortKey);
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

		private static string ProfileLabelFromPath(string path)
		{
			return path.Replace("Settings", string.Empty) 
			.Replace("/bookmarks", string.Empty)
			.Replace(".bk.json", string.Empty);
		}

		private static void FindBookmarks(string dir, List<string> paths)
		{
			var files = Helpers.ListFiles(dir, FILE_PATTERN, true);
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
				// var json = JsonConvert.SerializeObject(this, new JsonSerializerSettings
				// {
				// 	Formatting = Formatting.Indented, // cba adding tabs
				// });
				// File.WriteAllText(ProjectPath, json);
			}
			catch(Exception e)
			{
				Debug.LogError(e.Message);
			}
		}

		public string GetDisplayTitleOrDefault()
		{
			if (!Title.IsNullOrEmpty())
			{
				return Title;
			}
			var path = ProjectPath;
			if (path.IsNullOrEmpty())
			{
				return null;
			}
			// slice off file extension
			var i = path.Length - FILE_EXT.Length; 

			// find start of file name
			for (; i >= 0 && !(path[i] == '/' || path[i] == '\\'); i--) { }
			
			var fname = path.Substring(i + 1, path.Length - i - 9); // 9 = .bk.json
			// ideally optimize/simplify this crap (string builder or all-in-one regex)
			return fname.ToLower().SnakeToPascal().ToSentence();
		}

		// Relative path from project folder
		public string ProjectPath { get; private set; }

		// Absolute path to file
		public string FilePath { get; private set; }

		[JsonProperty("defaultSort")] private string _sortKey = "name";

		//[JsonProperty("defaultSort")]
		public BKSort DefaultSort => _sort.Value;

		// Display text, used for window tab if set
		[JsonProperty("title")] public string Title { get; private set; }

		// Filtered views
		[JsonProperty("views")] public BKView[] Views { get; private set; } = Array.Empty<BKView>();
		
		// Assets to include
		[JsonProperty("include")]
		public BKInclude Include { get; private set; } = new ();

		// GUID of display icon for view
		[JsonProperty("icon")] private string _iconGUID;

		public GUIContent Icon => _icon?.Value;
		private Lazy<GUIContent> _icon;
		private Lazy<BKSort> _sort;

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
			if (lastEdit == _lastWrite) { return; }
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

		private IEnumerable<BKAsset> GetAllAssets()
		{
			return Include.FindAssets();
		}

		private long GetLastFileEdit()
		{
			return File.GetLastWriteTime(FilePath).ToFileTime();
		}

		private sealed class EmptyReader : IBKReader
		{
			public static readonly EmptyReader Instance = new ();

			private EmptyReader(){}
			public IEnumerable<BKAsset> Read() => Array.Empty<BKAsset>();
		}

		private sealed class ReadHandle : IBKReader
		{
			public Action reloadFn;
			public Func<long> dateFn;
			public Func<IEnumerable<BKAsset>> getFn;
			private long _lastDate;
			private IEnumerable<BKAsset> _cachedAssets = Array.Empty<BKAsset>();

			public IEnumerable<BKAsset> Read()
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