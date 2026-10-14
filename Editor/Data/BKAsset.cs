// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using Newtonsoft.Json;
	using System;
	using UnityEditor;
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
	internal sealed class BKAsset : IEquatable<BKAsset>
	{
		public string GUID { get; }
		public string FileName => _fileName.Value;
		public long FileSize => _fileSize.Value;
		public string AssetPath => _assetPath.Value;
		public UObject Asset => _asset.Value;
		public Type MainType => _assetType.Value;
		public Texture AssetIcon => _icon.Value;
		
		public bool IsValid() => !CheckInvalid();

		public bool IsSelected()
		{
			return Selection.Contains(Asset);
		}

		/// <summary>
		/// Reload cached information like asset paths
		/// (to deal with possible move)
		/// </summary>
		public void Reload()
		{
			// clear current value
			_assetPath = new CachedLoad<string>(LoadAssetPath);
		}

		public static implicit operator BKAsset(string guid)
		{
			guid = SanitizeGUID(guid); // guid might be alias
			return guid.IsNullOrEmpty()
			? null
			: new BKAsset(guid);
		}

		// convert to most stable ref we have: guid
		public static implicit operator string(BKAsset a) => a.GUID;
		
		// truthy value depends on asset existing
		public static implicit operator bool(BKAsset a) => a != null && a.IsValid();

		public override int GetHashCode() => GUID.GetHashCode();

		bool IEquatable<BKAsset>.Equals(BKAsset other) => GUID == other?.GUID;
		
		private CachedLoad<UObject> _asset;
		private CachedLoad<Type> _assetType;
		private CachedLoad<long> _fileSize;
		private CachedLoad<string> _fileName;
		private CachedLoad<string> _assetPath;
		private CachedLoad<Texture> _icon;
		
		private BKAsset(string guid)
		{
			GUID = guid;
			_asset = new CachedLoad<UObject>(LoadAsset);
			_assetType = new CachedLoad<Type>(LoadAssetType);
			_assetPath = new CachedLoad<string>(LoadAssetPath);
			_fileName = new CachedLoad<string>(LoadFileName);
			_fileSize = new CachedLoad<long>(LoadFileSize);
			_icon = new CachedLoad<Texture>(LoadIcon);
		}

		private static string SanitizeGUID(string guidOrAlias)
		{
			if (guidOrAlias.IsNullOrEmpty())
			{
				return null;
			}
			// maybe here: map alias to guid
			return guidOrAlias;
		}

		private bool CheckInvalid()
		{
			return
			GUID.IsNullOrEmpty()
			|| (_asset.IsValueCreated && !_asset.Value) // a load attempt was made
			|| (_assetPath.IsValueCreated && _assetPath.Value.IsNullOrEmpty());
		}

		private UObject LoadAsset() => AssetDatabase.LoadAssetAtPath<UObject>(_assetPath.Value);
		private Type LoadAssetType() => AssetDatabase.GetMainAssetTypeAtPath(AssetPath);
		private string LoadAssetPath() => AssetDatabase.GUIDToAssetPath(GUID);
		private string LoadFileName() => NameFromPath(_assetPath.Value);
		private long LoadFileSize() => GetFileSize(_assetPath.Value);
		private Texture LoadIcon() => AssetDatabase.GetCachedIcon(_assetPath.Value);

		private static string NameFromPath(string path)
		{
			if (path is { Length:0 })
			{
				return null;
			}
			// saves us having to load object just to get its name
			// but we don't know if object has been deleted
			var sepIndex = path.LastIndexOf('/');
			var extIndex = path.LastIndexOf('.');
			var len = extIndex - sepIndex - 1;
			return path.Substring(sepIndex + 1, len);
		}

		private static long GetFileSize(string path)
		{
			try
			{
				return File.Exists(path)
				? new System.IO.FileInfo(path).Length
				: 0;
			}
			catch
			{
				// ignored
			}
			return 0;
		}
	}
}