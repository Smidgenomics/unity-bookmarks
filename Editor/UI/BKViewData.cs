// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using global::Unity.Properties;
	using UnityEditor;
	using UnityEngine;

	/// <summary>
	/// Bookmark list item data
	/// </summary>
	[Serializable]
	[GeneratePropertyBag]
	internal struct BKItemViewData
	{
		[CreateProperty]
		public string label =>
		asset.ItemType == EBKItemType.Asset
		? asset.FileName
		: $"{asset.FileName}.{asset.FileExtension}";

		[CreateProperty] public Texture icon => asset?.ItemIcon.tex;

		[CreateProperty]
		public Rect uv
		{
			get
			{
				if (!icon)
				{
					return default;
				}
				var icoUV = asset.ItemIcon.uv.Value;
				// icoUV.y = 1 - icoUV.y; // toolkit uvs are flipped
				var pos = icoUV.position;
				pos.y = 1 - (pos.y + icoUV.size.y);
				icoUV.position = pos;
				return icoUV;
			}
		}
		
		
		[CreateProperty]
		public Color IconBackgroundColor => icon ? asset.ItemIcon.bg : default;

		[CreateProperty]
		public Color BGColor => (asset?.IsSelected() ?? false) ? BKConstants.SELECT_COLOR : Color.clear;

		[CreateProperty]
		public Color IconTint => icon ? asset.ItemIcon.tint : default;

		[DontCreateProperty] public BKItemViewPrefs prefs;
		[DontCreateProperty] public BKItem asset;
	}
}

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using System.Collections.Generic;
	using global::Unity.Properties;
	using UnityEngine;
	using UnityEngine.UIElements;

	[Serializable]
	[GeneratePropertyBag]
	internal sealed class BKWindowViewData
	{
		[CreateProperty]
		public DisplayStyle filterDisplayStyle
		{
			get
			{
				if (user == null)
				{
					return DisplayStyle.None;
				}
				return user.ViewCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
			}
		}

		[CreateProperty]
		public Rect filterUV
		{
			get
			{
				if (user == null)
				{
					return new Rect(0.5f, 0.75f, 0.25f, 0.25f);
				}
				return string.IsNullOrEmpty(user.FilterKey)
				? new Rect(0.5f, 0.75f, 0.25f, 0.25f)
				: new Rect(0.75f, 0.75f, 0.25f, 0.25f);
			}
		}

		public string footerText = "-";

		[NonSerialized, DontCreateProperty] public BKUser user;
		[NonSerialized, DontCreateProperty] public readonly List<BKItemViewData> items = new();
	}
}

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using global::Unity.Properties;
	using UnityEngine;

	[Serializable]
	internal sealed class BKItemViewPrefs
	{
	
	}
}