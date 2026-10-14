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
	[UxmlElement("BKToolbarMenu", libraryPath = "Bookmarks")]
#endif
	[Icon("UIToolkit/Icons/ToolbarMenu.png")]
	internal sealed partial class BKMenuButton : ToolbarMenu
	{
		private const string _USS_GUID = "d21c4d72d983cfb41b9905657e88c2a4";

		private Image _icon;

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
			var styleAsset = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(_USS_GUID));
			styleSheets.Add(styleAsset);
			AddToClassList("bk-menubutton");
			_icon = new Image();
			_icon.AddToClassList("bk-menubutton__icon");
			_icon.tintColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
			Add(_icon);

			var menuArrow = this.Q<VisualElement>(className: "unity-toolbar-menu__arrow");
			Remove(menuArrow);
			Add(menuArrow);

		}
	}
}