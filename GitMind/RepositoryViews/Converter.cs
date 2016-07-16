using System;
using System.Windows;


namespace GitMind.RepositoryViews
{
	internal static class Converter
	{
		private static readonly int RowHeight = 17;
		private static readonly int ColumnSize = 20;
		public static readonly int HalfRow = RowHeight / 2;


		public static int ToRow(double y)
		{
			return (int)y / RowHeight;
		}


		public static int ToY(int row)
		{
			return row * RowHeight;
		}


		public static int ToColumn(double x)
		{
			return (int)((x + 3) / ColumnSize);
		}


		public static int ToX(int column)
		{
			return ColumnSize * column;
		}


		public static int ToTopRowIndex(Rect rectangle, int commitsCount)
		{
			int firstIndex = (int)Math.Floor(rectangle.Top / RowHeight);
			firstIndex = Math.Max(firstIndex, 0);
			firstIndex = Math.Min(firstIndex, commitsCount);
			return firstIndex;
		}


		public static int ToBottomRowIndex(Rect rectangle, int commitsCount)
		{
			int lastIndex = (int)Math.Ceiling(rectangle.Bottom / RowHeight);
			lastIndex = Math.Min(lastIndex, commitsCount);
			return lastIndex;
		}


		public static int ToRowExtent(int commitsCount)
		{
			return commitsCount * RowHeight;
		}	
	}
}