using System.Collections.Generic;


namespace GitMind.Utils
{
	internal static class Sorter
	{
		public static void Sort<T>(IList<T> list, IComparer<T> comparer)
		{
			CustomSort(list, comparer);
		}

		private static void CustomSort<T>(IList<T> list, IComparer<T> comparer)
		{
			for (int i = 0; i < list.Count; i++)
			{
				bool swapped = false;
				T item = list[i];

				for (int j = i + 1; j < list.Count; j++)
				{
					if (comparer.Compare(item, list[j]) > 0)
					{
						T tmp = list[j];
						list.RemoveAt(j);
						list.Insert(i, tmp);
						swapped = true;
					}
				}

				if (swapped)
				{
					i = i - 1;
				}
			}
		}

		////		private static void BubbleSort<T>(IList<T> list, IComparer<T> comparer)
		////		{
		////			bool swapped;
		////			do
		////			{
		////				swapped = false;
		////				for (int i = 1; i < list.Count; i++)
		////				{
		////					int r = comparer.Compare(list[i - 1], list[i]);
		////					if (r > 0)
		////					{
		////						T tmp = list[i - 1];
		////						list[i - 1] = list[i];
		////						list[i] = tmp;
		////						swapped = true;
		////					}
		////				}
		////			} while (swapped);
		////		}


		////		private static void InsertionSort<T>(IList<T> list, IComparer<T> comparer)
		////		{
		////			for (int i = 1; i < list.Count; i++)
		////			{
		////				int j = i;
		////				while (j > 0 && comparer.Compare(list[j - 1], list[j]) > 0)
		////				{
		////					T tmp = list[j - 1];
		////					list[j - 1] = list[j];
		////					list[j] = tmp;
		////					j = j - 1;
		////				}
		////			}

		////			/*
		////			for i ← 1 to length(A) - 1
		////    j ← i
		////    while j > 0 and A[j-1] > A[j]
		////        swap A[j] and A[j-1]
		////        j ← j - 1
		////    end while
		////end for
		////			*/
		////		}
	}
}