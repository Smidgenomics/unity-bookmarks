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
		[CreateProperty] public string label => asset.FileName;
		[CreateProperty] public Texture icon => asset?.AssetIcon;
		[CreateProperty] public Rect uv => new Rect(0, 0, 1, 1);

		[CreateProperty]
		public Color BGColor => (asset?.IsSelected() ?? false) ? BKConstants.SELECT_COLOR : Color.clear;

		[CreateProperty]
		public Color iconTint => EditorGUIUtility.isProSkin
		? Color.white
		: Color.black;

		[DontCreateProperty] public BKItemViewPrefs prefs;
		[DontCreateProperty] public BKAsset asset;
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
				return string.IsNullOrEmpty(user.View)
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