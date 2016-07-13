using System;
using System.Collections.Generic;


namespace GitMind.Utils
{
	internal static class Compare
	{
		public static IComparer<T> With<T>(Func<T, T, int> comparer)
		{
			return new Comparer<T>(comparer);
		}

		private class Comparer<T1>: IComparer<T1>
		{
			private readonly Func<T1, T1, int> comparer;

			public Comparer(Func<T1, T1, int> comparer)
			{
				this.comparer = comparer;
			}

			public int Compare(T1 x, T1 y) => comparer(x, y);
		}
	}
}