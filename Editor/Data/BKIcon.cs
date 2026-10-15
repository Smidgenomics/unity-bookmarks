// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System.Runtime.Serialization;
	using Newtonsoft.Json;
	using UnityEngine;

	[System.Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class BKIconSettings
	{
		public LoadedIcon GetIcon() => _icon.Value;

		[JsonProperty("tex")] private string _tex;
		[JsonProperty("uv")] private string _uv;
		[JsonProperty("tint")] private string _tint;
		[JsonProperty("bg")] private string _bg;

		private CachedLoad<LoadedIcon> _icon = CachedLoad<LoadedIcon>.Default;

		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			_icon = new CachedLoad<LoadedIcon>(LoadIcon);
		}

		private LoadedIcon LoadIcon()
		{
			return new LoadedIcon()
			{
				tint = BKParse.ParseSkinValue(_tint, BKParse.ParseColor, Color.white),
				bg = BKParse.ParseSkinValue(_bg, BKParse.ParseColor, Color.clear),
				tex =  BKUtils.LoadTexture(_tex),
				uv = BKParse.ParseSkinValue(_uv, BKParse.ParseRect, new Rect(0, 0, 1, 1))
			};
		}

		private static CachedLoad<Texture> GetIconLazy(string str, CachedLoad<Texture> dValue)
		{
			return new CachedLoad<Texture>(() => BKUtils.LoadTexture(str));
		}
	}
}