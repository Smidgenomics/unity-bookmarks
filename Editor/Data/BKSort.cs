// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Newtonsoft.Json;

	/// <summary>
	/// Item sorter
	/// </summary>
	[Serializable]
	[JsonObject(MemberSerialization.OptIn)]
	[JsonConverter(typeof(JsonToStringConverter))]
	internal sealed class BKSort
	{
		public static readonly BKSort Default = new ("n");

		public IEnumerable<BKItem> OrderItems(IEnumerable<BKItem> items)
		{
			if (_orderFns.Count == 0)
			{
				return items;
			}

			var (fn, desc) = _orderFns[0];
			
			var sorted = desc
				? items.OrderByDescending(x => fn.Invoke(x))
				: items.OrderBy(x => fn.Invoke(x));

			for (var i = 1; i < _orderFns.Count; i++)
			{
				var (nextFn, nextDesc) = _orderFns[i];
				sorted = nextDesc
					? sorted.ThenByDescending(x => nextFn.Invoke(x))
					: sorted.ThenBy(x => nextFn.Invoke(x));
			}
			return sorted;
		}

		private delegate IComparable OrderFn(BKItem item);
		private readonly List<(OrderFn, bool)> _orderFns = new();
		private string _raw;

		private BKSort(string s)
		{
			_raw = s;
			if (s.Contains(','))
			{
				HashSet<EBKSortBy> usedKeys = new();
				var segments = s.Split(',');
				foreach (var segment in segments)
				{
					var (type, desc) = ParseSort(segment);
					if (usedKeys.Add(type))
					{
						_orderFns.Add((GetFunction(type), desc));
					}
				}
			}
			else
			{
				var (type, desc) = ParseSort(s);
				_orderFns.Add((GetFunction(type), desc));
			}
		}

		private IComparable GetFileName(BKItem a) => a.FileName;
		private IComparable GetTypeName(BKItem a) => a.MainType.Name;
		private IComparable GetFileExtension(BKItem a) => a.FileExtension;
		private IComparable GetFilePath(BKItem a) => a.FilePath;

		private OrderFn GetFunction(EBKSortBy sort)
		{
			return sort switch
			{
				EBKSortBy.Name => GetFileName,
				EBKSortBy.Type => GetTypeName,
				EBKSortBy.Path => GetFilePath,
				EBKSortBy.Extension => GetFileExtension,
				_ => GetFileName
			};
		}

		public static implicit operator string(BKSort s)
		{
			return s._raw;
		}

		public override string ToString() => this;

		public static implicit operator BKSort(string s)
		{
			if(string.IsNullOrEmpty(s))
			{
				return null;
			}
			return new BKSort(s);
		}

		private static (EBKSortBy,bool) ParseSort(string sort)
		{
			sort = sort.Trim();
			if (sort.Length == 0)
			{
				return default;
			}
			var desc = sort[0] == '-';
			var sorType = sort[desc ? 1 : 0] switch
			{
				't' => EBKSortBy.Type,
				'p' => EBKSortBy.Path,
				'e' => EBKSortBy.Extension,
				_ => EBKSortBy.Name
			};
			return (sorType, desc);
		}
	}
}