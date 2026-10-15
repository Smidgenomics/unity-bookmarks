// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System.IO;
	using System;
	using UnityEditor;
	using UnityEngine;

	// misc helper functions
	internal static class BKUtils
	{
		public static long GetLastFileEditTime(string filePath)
		{
			return File.GetLastWriteTime(filePath).ToFileTime();
		}
		
		public static void OpenProjectFile(string relativePath)
		{
			var path = Path.Combine(BKConstants.PROJECT_ROOT, relativePath).Replace(@"\", "/");
			if (!File.Exists(path))
			{
				return;
			}
			System.Diagnostics.Process.Start(path);
		}

		public static long GetFileSize(string path)
		{
			try
			{
				return File.Exists(path)
				? new FileInfo(path).Length
				: 0;
			}
			catch
			{
				// ignored
			}
			return 0;
		}

		public static bool CanOpenAsset(UnityEngine.Object ob)
		{
#if UNITY_6000_3_OR_NEWER
			return AssetDatabase.CanOpenAssetInEditor(ob.GetEntityId());
#else
			return AssetDatabase.CanOpenAssetInEditor(ob.GetInstanceID());
#endif
		}

		public static void OpenAsset(UnityEngine.Object ob)
		{
#if UNITY_6000_3_OR_NEWER
			AssetDatabase.OpenAsset(ob.GetEntityId());
#else
			AssetDatabase.OpenAsset(ob.GetInstanceID());
#endif
		}

		public static Texture LoadTexture(string guidOrName)
		{
			if (guidOrName.IsNullOrEmpty())
			{
				return null;
			}
			if (guidOrName.IsGUID())
			{
				return AssetDatabase.LoadAssetAtPath<Texture>(AssetDatabase.GUIDToAssetPath(guidOrName));
			}
			return EditorGUIUtility.IconContent(guidOrName)?.image;
		}

		public static string FileNameFromPath(string path, bool ext = false)
		{
			if (path is { Length:0 })
			{
				return null;
			}
			var sepIndex = path.LastIndexOf('/');
			if (ext)
			{
				return path.Substring(sepIndex + 1);
			}
			var extIndex = path.LastIndexOf('.');
			var len = extIndex - sepIndex - 1;
			return path.Substring(sepIndex + 1, len);
		}

		public static string[] ListFiles(string dir, string pattern, bool recursive = false)
		{
			if (!Directory.Exists(dir))
			{
				return Array.Empty<string>();
			}

			if (!recursive)
			{
				return Directory.GetFiles(dir, pattern);
			}
			return Directory.GetFiles(dir, pattern, SearchOption.AllDirectories);
		}

		private static readonly string[] FSIZE_LABEL = { "B", "KB", "MB", "GB", "TB" };

		public static string FormatFileSize(long size)
		{
			int order = 0;
			while (size >= 1024 && order < FSIZE_LABEL.Length - 1)
			{
				order++;
				size = size / 1024;
			}
			var unit = FSIZE_LABEL[order];
			return $"{size} {unit}";
		}

	}
}