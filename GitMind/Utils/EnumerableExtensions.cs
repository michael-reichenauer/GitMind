using System.Collections.Generic;



namespace System.Linq
{
	public static class EnumerableExtensions
	{
		public static void ForEach<T>(this IEnumerable<T> enumeration, Action<T> action)
		{
			foreach (T item in enumeration)
			{
				action(item);
			}
		}

		public static IReadOnlyList<TSource> AsReadOnlyList<TSource>(this IReadOnlyList<TSource> source)
		{
			return source;
		}

		public static IReadOnlyList<TSource> ToReadOnlyList<TSource>(this IEnumerable<TSource> enumeration)
		{
			return enumeration.ToList();
		}

		/// <summary>
		///  Returns distinct elements from a sequence by using a specified 
		///  predicate to compare values of two elements.
		/// </summary>
		public static IEnumerable<TSource> Distinct<TSource>(this IEnumerable<TSource> source,
			Func<TSource, TSource, bool> comparer)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			if (comparer == null)
			{
				throw new ArgumentNullException(nameof(comparer));
			}

			// Use the MSDN provided Distinct function with a private custom IEqualityComparer comparer.
			return source.Distinct(new DistinctComparer<TSource>(comparer));
		}


		private class DistinctComparer<TSource> : IEqualityComparer<TSource>
		{
			private readonly Func<TSource, TSource, bool> comparer;

			public DistinctComparer(Func<TSource, TSource, bool> comparer)
			{
				this.comparer = comparer;
			}

			public bool Equals(TSource x, TSource y) => comparer(x, y);

			// Always returns 0 to force the Distinct comparer function to call the Equals() function
			// to do the comparison
			public int GetHashCode(TSource obj) => 0;
		}
	}
}