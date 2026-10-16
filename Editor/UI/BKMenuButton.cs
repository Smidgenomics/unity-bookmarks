// smidgens @ github

#pragma warning disable 0414

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using global::Unity.Properties;
	using UnityEditor;
	using UnityEditor.UIElements;
	using UnityEngine;
	using UnityEngine.UIElements;

	// Note: Exists to add icon to button
	[System.Serializable]
	[UxmlElement("BKToolbarMenu", libraryPath = "Bookmarks")]
	[Icon("UIToolkit/Icons/ToolbarMenu.png")]
	internal sealed partial class BKMenuButton : ToolbarMenu
	{
		private const string _USS_CLASS = "sm-bookmarks__toolbar__menu";

		[UxmlAttribute, CreateProperty]
		public Rect UV
		{
			get => _icon?.uv ?? default;
			set
			{
				if (_icon != null)
				{
					_icon.uv = value;
				}
			}
		}

		[UxmlAttribute, CreateProperty]
		public Texture Image
		{
			get => _icon?.image;
			set
			{
				if (_icon != null)
				{
					_icon.image = value;
				}
			}
		}

		public BKMenuButton()
		{
			AddToClassList(_USS_CLASS);
			_icon = new Image();
			Add(_icon);
			var menuArrow = this.Q<VisualElement>(className: "unity-toolbar-menu__arrow");
			Remove(menuArrow);
			Add(menuArrow);
		}
		
		private Image _icon;
		
	}
}