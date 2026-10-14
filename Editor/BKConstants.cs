// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using UnityEngine;

	internal static class BKConstants
	{
		// project root directory
		public static readonly string PROJECT_ROOT = Application.dataPath[..^7];

		// asset select color
		public static readonly SkinPick<Color> SELECT_COLOR =
		(
			new Color(0.24f, 0.5f, 0.87f, 1f),
			new Color(0.24f, 0.38f, 0.57f, 1f)
		);
		
		// window tab icon
		public static readonly SkinPick<string> ICO_GUID_WINDOW =
		(
			"56653dbf033bc8a47bd28d00b20e43ca",
			"168330aef01628e43841502b24626ec0"
		);
		
		// time before updating filter
		public const double SEARCH_BUFFER_TIME = 0.3;

		// uxml template asset
		public const string WIN_TEMPLATE_GUID = "8bb2895c7be3a0a4c96851613519e78e";

	}
}