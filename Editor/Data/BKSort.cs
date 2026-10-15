// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using Newtonsoft.Json;

	/// <summary>
	/// Item sorter
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	internal sealed class BKSort
	{
		public static readonly BKSort Default = new (EBKSortBy.Name, false);
		public EBKSortBy SortBy { get; }
		public bool Descending { get; }

		public int Compare(BKItem a, BKItem b)
		{
			return _compareFn.Invoke(a, b);
		}

		public IComparable OrderBy(BKItem a)
		{
			return _orderFn.Invoke(a);
		}

		private readonly Func<BKItem,BKItem,int> _compareFn;
		private readonly Func<BKItem,IComparable> _orderFn;

		private BKSort(EBKSortBy sort, bool desc)
		{
			SortBy = sort;
			Descending = desc;
			_compareFn = sort == EBKSortBy.Name ? CompareName : CompareType;
			_orderFn = sort == EBKSortBy.Name ? OrderByName : OrderByType;
		}

		private IComparable OrderByName(BKItem a) => a.FileName;
		private IComparable OrderByType(BKItem a) => a.MainType.Name;

		public static implicit operator string(BKSort s)
		{
			var m = s.Descending ? '-' : '\0';
			var n = s.SortBy == EBKSortBy.Type ? 't' : 'n';
			return $"{m}{n}";
		}

		public static implicit operator BKSort(string s)
		{
			if(string.IsNullOrEmpty(s))
			{
				return null;
			}
			var (type, desc) = ParseSort(s);
			return new BKSort(type, desc);
		}

		private static (EBKSortBy,bool) ParseSort(string sort)
		{
			if (sort.Length < 1)
			{
				return default;
			}
			var desc = sort[0] == '-';
			var type = sort[desc ? 1 : 0] == 't' ? EBKSortBy.Type : EBKSortBy.Name;
			return (type, desc);
		}

		private int CompareName(BKItem a, BKItem b)
		{
			SwapIf(ref a, ref b, Descending);
			return a.FileName.CompareTo(b);
		}

		private int CompareType(BKItem a, BKItem b)
		{
			SwapIf(ref a, ref b, Descending);
			return String.Compare(a.MainType.Name, b.MainType.Name, StringComparison.Ordinal);
		}

		private static void SwapIf<T>(ref T a, ref T b, bool condition)
		{
			if (!condition)
			{
				return;
			}
			(a, b) = (b, a);
		}
	}
}