// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using Newtonsoft.Json;
	using System;
	using UnityEditor;
	using UnityEditor.Search;
	using UnityEngine;
	using UnityEngine.Windows;
	using UObject = UnityEngine.Object;

	/// <summary>
	/// Wrapper for managing asset in bookmarks UI
	/// - Includes implicit conversion from string (asset guid)
	/// - Generates data lazily
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class BKItem : IEquatable<BKItem>
	{
		public string GUID { get; }
		public EBKItemType ItemType { get; }
		public string FileName => _fileName.Value;
		public long FileSize => _fileSize.Value;
		public string FilePath => _filePath.Value;
		public UObject Asset => _asset.Value;
		public Type MainType => _assetType.Value;
		public LoadedIcon ItemIcon => _icon.Value;
		public string FileExtension => _fileExt.Value;
		public BKProfile Profile { get; set; }
		public bool IsDraggable() => ItemType == EBKItemType.Asset;
		public bool IsValid() => !CheckInvalid();

		public bool IsSelected()
		{
			if (ItemType == EBKItemType.Asset)
			{
				return Selection.Contains(Asset);
			}
			return false;
		}

		public void Select()
		{
			if (ItemType == EBKItemType.Asset)
			{
				Selection.activeObject = Asset;
			}
		}

		public bool CanOpenFile()
		{
			return ItemType == EBKItemType.External || BKUtils.CanOpenAsset(Asset);
		}

		public void Open()
		{
			if (CanOpenFile())
			{
				if (ItemType == EBKItemType.External)
				{
					BKUtils.OpenProjectFile(FilePath);
				}
				else
				{
					BKUtils.OpenAsset(Asset);
				}
			}
		}

		/// <summary>
		/// Reload cached information like asset paths
		/// (to deal with possible move)
		/// </summary>
		public void Reload()
		{
			// clear current value
			if (ItemType == EBKItemType.Asset)
			{
				_filePath = new CachedLoad<string>(LoadAssetPathFromGUID);
			}
		}

		public static implicit operator BKItem(string guidOrPath)
		{
			if (guidOrPath.IsNullOrEmpty() || !TryGetItemType(guidOrPath, out var itemType))
			{
				return null;
			}

			// see if we can convert path to guid
			if (itemType == EBKItemType.External && guidOrPath.StartsWith("Assets/"))
			{
				var possibleGUID = AssetDatabase.AssetPathToGUID(guidOrPath);
				if (!string.IsNullOrEmpty(possibleGUID))
				{
					itemType = EBKItemType.Asset;
					guidOrPath = possibleGUID;
				}
			}
			return new BKItem(guidOrPath, itemType);
		}

		// convert to most stable ref we have: guid
		public static implicit operator string(BKItem a)
		{
			return a.ItemType == EBKItemType.Asset ? a.GUID : a.FilePath;
		}

		// truthy value depends on asset existing
		public static implicit operator bool(BKItem a) => a != null && a.IsValid();

		public override int GetHashCode() => FilePath.GetHashCode();

		bool IEquatable<BKItem>.Equals(BKItem other) => GUID == other?.GUID;

		private CachedLoad<UObject> _asset = CachedLoad<UObject>.Default;
		private CachedLoad<Type> _assetType = new (GetDefaultType);
		private CachedLoad<string> _filePath;
		private CachedLoad<long> _fileSize;
		private CachedLoad<string> _fileName;
		private CachedLoad<string> _fileExt;
		private CachedLoad<LoadedIcon> _icon;

		private string _externalPath;

		private BKItem(string str, EBKItemType type)
		{
			ItemType = type;

			_fileName = new CachedLoad<string>(LoadFileName);
			_fileSize = new CachedLoad<long>(LoadFileSize);
			_fileExt = new CachedLoad<string>(GetFileExtension);

			if (type == EBKItemType.Asset)
			{
				GUID = str;
				_assetType = new CachedLoad<Type>(LoadAssetType);
				_asset = new CachedLoad<UObject>(LoadAssetFromGUID);
				_filePath = new CachedLoad<string>(LoadAssetPathFromGUID);
				_icon = new CachedLoad<LoadedIcon>(LoadAssetIcon);
			}
			else
			{
				_icon = new(GetExternalIcon);
				_externalPath = str;
				_filePath = new CachedLoad<string>(GetKnownPath);
			}
		}
		
		private static bool TryGetItemType(string val, out EBKItemType type)
		{
			type = default;
			if (val.IsGUID())
			{
				type = EBKItemType.Asset;
				return true;
			}
			if (File.Exists(val))
			{
				type = EBKItemType.External;
				return true;
			}
			return false;
		}

		private bool CheckInvalid()
		{
			if (ItemType == EBKItemType.Asset)
			{
				return
				GUID.IsNullOrEmpty()
				|| (_asset.IsValueCreated && !_asset.Value) // a load attempt was made
				|| (_filePath.IsValueCreated && _filePath.Value.IsNullOrEmpty());
			}
			return !File.Exists(_filePath.Value);
		}

		private UObject LoadExternal() => null;
		private UObject LoadAssetFromGUID() => AssetDatabase.LoadAssetAtPath<UObject>(_filePath.Value);

		private string GetFileExtension()
		{
			var sep = _filePath.Value.LastIndexOf('.');
			return _filePath.Value.Substring(sep + 1);
		}
		
		private static Type GetDefaultType() => typeof(DefaultAsset);

		private Type LoadAssetType() => AssetDatabase.GetMainAssetTypeAtPath(_filePath.Value);

		private string GetKnownPath() => _externalPath;
		private string LoadAssetPathFromGUID() => AssetDatabase.GUIDToAssetPath(GUID);
		private string LoadFileName() => BKUtils.FileNameFromPath(_filePath.Value);
		private long LoadFileSize() => BKUtils.GetFileSize(_filePath.Value);

		private LoadedIcon LoadAssetIcon()
		{
			if (Profile != null && Profile.TryGetIcon(FileExtension, out var ico))
			{
				return ico;
			}
			return LoadedIcon.FromTexture(AssetDatabase.GetCachedIcon(_filePath.Value));
		}

		private LoadedIcon GetExternalIcon()
		{
			if (Profile != null && Profile.TryGetIcon(FilePath, out var ico))
			{
				return ico;
			}
			return LoadedIcon.FromTexture(SearchUtils.GetTypeIcon(typeof(TextAsset)));
		}
	}
}