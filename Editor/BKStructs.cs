// smidgens @ github

namespace Smidgenomics.Unity.Bookmarks.Editor
{
	using System;

	// stack friendly version of System.Lazy<T>
	internal struct CachedLoad<T>
	{
		public T Value => _getter.Invoke(ref this);
		public bool IsValueCreated { get; private set; }
		public CachedLoad(Func<T> fn)
		{
			_fn = fn;
			_value = default;
			_getter = GetterInit;
			IsValueCreated = false;
		}

		private delegate T Getter(ref CachedLoad<T> v);

		private Getter _getter;
		private Func<T> _fn;
		private T _value;

		private static T GetterLoaded(ref CachedLoad<T> cl) => cl._value;

		private static T GetterInit(ref CachedLoad<T> cl)
		{
			cl._value = cl._fn.Invoke();
			cl._fn = null;
			cl.IsValueCreated = true;
			cl._getter = GetterLoaded;
			return cl._value;
		}
	}
}