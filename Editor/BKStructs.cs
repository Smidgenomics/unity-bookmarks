// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;

	// stack friendly version of System.Lazy<T>
	internal struct CachedLoad<T>
	{
		public static CachedLoad<T> Default => new(DefaultGetter);

		public T Value => _getter.Invoke(ref this);
		public bool IsValueCreated { get; private set; }
		public CachedLoad(Func<T> fn)
		{
			_fn = fn;
			_value = default;
			_getter = GetterInit;
			IsValueCreated = false;
		}

		public static implicit operator CachedLoad<T>(Func<T> fn) => new(fn);

		private static T DefaultGetter() => default;

		private delegate T Getter(ref CachedLoad<T> v);

		private Getter _getter;
		private Func<T> _fn;
		private T _value;

		private static T GetterLoaded(ref CachedLoad<T> cl) => cl._value;

		private static T GetterInit(ref CachedLoad<T> cl)
		{
			cl._value = cl._fn.Invoke();
			cl._fn = null;
			cl.IsValueCreated = true;
			cl._getter = GetterLoaded;
			return cl._value;
		}
	}
}

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using UnityEditor;

	// selects value based on active editor skin
	internal readonly struct SkinPick<T>
	{
		public T Value => EditorGUIUtility.isProSkin ? _dValue : _lValue;

		public SkinPick(T lightValue, T darkValue)
		{
			_dValue = darkValue;
			_lValue = lightValue;
		}

		public static implicit operator T(SkinPick<T> v) => v.Value;
		public static implicit operator SkinPick<T>((T, T) val) => new(val.Item1, val.Item2);
		public static implicit operator SkinPick<T>(T val) => new(val, val);

		private readonly T _dValue, _lValue;
	}
}


namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System.IO;

	// wrapper to keep track of edits to file
	internal struct FileChangeHandle
	{
		public FileChangeHandle(string path)
		{
			_path = path;
			_lastEdit = 0;
			Refresh();
		}

		public void Refresh() => _lastEdit = GetEditTime();

		public readonly bool Exists() => File.Exists(_path);

		public readonly bool HasChanged() => _lastEdit != GetEditTime();

		private readonly string _path;
		private long _lastEdit;

		private readonly long GetEditTime()
		{
			return Exists() ? File.GetLastWriteTimeUtc(_path).ToFileTime() : 0;
		}
	}
}

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using UnityEngine;

	internal struct LoadedIcon
	{
		public static LoadedIcon FromTexture(Texture tex)
		{
			return new LoadedIcon()
			{
				tex = tex,
				tint = Color.white,
				uv = new Rect(0,0,1,1),
			};
		}

		public SkinPick<Texture> tex;
		public SkinPick<Color> tint;
		public SkinPick<Color> bg;
		public SkinPick<Rect> uv;
	}
}