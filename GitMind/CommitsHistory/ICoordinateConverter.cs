using System.Windows;


namespace GitMind.CommitsHistory
{
	internal interface ICoordinateConverter
	{
		int GetTopRowIndex(Rect rectangle, int commitsCount);
		int GetBottomRowIndex(Rect rectangle, int commitsCount);
		int GetRowExtent(int commitsCount);
		int ConvertToColumn(double x);
		int ConvertFromColumn(int column);
		int ConvertFromRow(int row);
		int ConvertToRow(double y);
	}
}