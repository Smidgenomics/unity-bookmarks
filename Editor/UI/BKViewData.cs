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
		public string label
		{
			get
			{
				return item.FileName;
				// item.ItemType == EBKItemType.Asset
				// ? item.FileName
				// : $"{item.FileName}.{item.FileExtension}";
			}
		}
		

		[CreateProperty] public Texture icon => item?.ItemIcon.tex;

		[CreateProperty]
		public Rect uv
		{
			get
			{
				if (!icon)
				{
					return default;
				}
				var icoUV = item.ItemIcon.uv.Value;
				var pos = icoUV.position;
				pos.y = 1 - (pos.y + icoUV.size.y);
				icoUV.position = pos;
				return icoUV;
			}
		}
		
		[CreateProperty]
		public Rect typeUV
		{
			get
			{
				if (!string.IsNullOrEmpty(item.GUID))
				{
					var p = item.FilePath;

					// Packages/
					if (p[0] == 'P')
					{
						return new Rect(0.25f, 0.25f, 0.25f, 0.25f);
					}
					return default;
				}
				
				// outside assets/packages
				return new Rect(0.5f, 0.25f, 0.25f, 0.25f);
			}
		}

		// [CreateProperty] public Color TypeIconTint => EditorGUIUtility.isProSkin ? Color.white : Color.black;
		[CreateProperty] public Color TypeIconTint => Color.white;

		[CreateProperty] public Color IconBackgroundColor => icon ? item.ItemIcon.bg : default;

		[CreateProperty]
		public Color BGColor
		{
			get
			{
				bool selected =  prefs?.lastClicked == item;

				return selected ? BKConstants.SELECT_COLOR : Color.clear;
			}
		}

		[CreateProperty]
		public Color IconTint => icon ? item.ItemIcon.tint : default;

		[DontCreateProperty] public BKItemViewPrefs prefs;
		[DontCreateProperty] public BKItem item;
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

		[CreateProperty]
		public string currentPath => prefs.lastClicked?.FilePath;

		[CreateProperty]
		public string currentSize => prefs.lastClicked != null
		? BKUtils.FormatFileSize(prefs.lastClicked.FileSize)
		: "";
		
		[NonSerialized, DontCreateProperty] public BKUser user;
		[NonSerialized, DontCreateProperty] public readonly BKItemViewPrefs prefs = new();

		[CreateProperty] public readonly List<BKItemViewData> items = new();
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
		public BKItem lastClicked;
	
	}
}