// smidgens @ github

#pragma warning disable 0414

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using global::Unity.Properties;
	using UnityEditor;
	using UnityEditor.UIElements;
	using UnityEngine;
	using UnityEngine.UIElements;

	[System.Serializable]
#if SM_DEV
	[UxmlElement("BKToolbarButton", libraryPath = "Bookmarks")]
#endif
	[Icon("UIToolkit/Icons/ToolbarButton.png")]
	internal sealed partial class BKToolbarButton : ToolbarButton
	{
		private const string _USS_GUID = "a4c982adbd92b53408e1c7bce4e57bd2";
		
		[UxmlAttribute, CreateProperty]
		public Rect UV
		{
			get => GetIconImage()?.uv ?? default;
			set
			{
				var ico = GetIconImage();
				if (ico != null)
				{
					ico.uv = value;
					ico.tintColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
				}
			}
		}

		public BKToolbarButton()
		{
			var styleAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(_USS_GUID));
			styleSheets.Add(styleAsset);
			AddToClassList("bk-toolbar__button");

		}

		private Image GetIconImage()
		{
			_icon ??= this.Q<Image>();
			return _icon;
		}

		private Image _icon;
		
	}
}