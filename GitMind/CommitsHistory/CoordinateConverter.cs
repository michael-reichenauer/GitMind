using System;
using System.Windows;


namespace GitMind.CommitsHistory
{
	internal class CoordinateConverter : ICoordinateConverter
	{

		private static readonly int RowHeight = 17;
		private static readonly int ColumnSize = 20;
		public static readonly int HalfRow = RowHeight / 2;


		public int ConvertToRow(double y)
		{
			return (int)y / RowHeight;
		}


		public int ConvertFromRow(int row)
		{
			return row * RowHeight;
		}


		public int ConvertToColumn(double x)
		{
			return (int)((x + 3) / ColumnSize);
		}


		public int ConvertFromColumn(int column)
		{
			return ColumnSize * column;
		}


		public int GetTopRowIndex(Rect rectangle, int commitsCount)
		{
			int firstIndex = (int)Math.Floor(rectangle.Top / RowHeight);
			firstIndex = Math.Max(firstIndex, 0);
			firstIndex = Math.Min(firstIndex, commitsCount);
			return firstIndex;
		}


		public int GetBottomRowIndex(Rect rectangle, int commitsCount)
		{
			int lastIndex = (int)Math.Ceiling(rectangle.Bottom / RowHeight);
			lastIndex = Math.Min(lastIndex, commitsCount - 1);
			return lastIndex;
		}


		public int GetRowExtent(int commitsCount)
		{
			return commitsCount * RowHeight;
		}

		
	}
}