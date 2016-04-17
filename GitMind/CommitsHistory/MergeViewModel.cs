using System.Windows;
using System.Windows.Media;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class MergeViewModel : ViewModel
	{
		public MergeViewModel(
			int mergeId,
			int parentRowIndex,
			int childRowIndex,
			Rect rect,
			string line,
			Brush brush,
			int stroke,
			string strokeDash)
		{
			MergeId = mergeId;
			ParentRowIndex = parentRowIndex;
			ChildRowIndex = childRowIndex;
			Rect.Set(rect);
			Width.Set(rect.Width);
			Line = line;
			Brush = brush;
			Stroke = stroke;
			StrokeDash = strokeDash;
		}

		public string Type => "Merge";
		public int MergeId { get; }
		public int ParentRowIndex { get; }
		public int ChildRowIndex { get; }
		public Property<Rect> Rect => Property<Rect>();
		public Property<double> Width => Property<double>();
		public string Line { get; }
		public Brush Brush { get; }
		public int Stroke { get; }
		public string StrokeDash { get; }	
	}
}