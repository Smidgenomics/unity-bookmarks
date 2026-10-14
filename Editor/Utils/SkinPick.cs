// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using UnityEditor;

	// selects value based on editor skin
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