// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using global::Unity.Properties;
	using UnityEditor;
	using UnityEditor.UIElements;
	using UnityEngine;
	using UnityEngine.UIElements;

	// Note: Only exists to expose UV property of icon
	[System.Serializable]
	[UxmlElement("BKToolbarButton", libraryPath = "Bookmarks")]
	[Icon("UIToolkit/Icons/ToolbarButton.png")]
	internal sealed partial class BKToolbarButton : ToolbarButton
	{
		private const string _USS_CLASS = "sm-bookmarks__toolbar__button";
		
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
			AddToClassList(_USS_CLASS);
		}

		private Image GetIconImage()
		{
			_icon ??= this.Q<Image>();
			return _icon;
		}

		private Image _icon;
		
	}
}