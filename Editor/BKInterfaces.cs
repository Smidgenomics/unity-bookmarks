// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System.Collections.Generic;

	/// <summary>
	/// Asset list provider
	/// </summary>
	internal interface IBKReader
	{
		public IEnumerable<BKItem> Read();
	}
}
