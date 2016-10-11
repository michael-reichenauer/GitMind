using System.Windows;
using System.Windows.Media;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class MergeViewModel : ViewModel
	{
		// UI properties
		public int ZIndex => 100;
		public string Type => nameof(MergeViewModel);	
		public Rect Rect { get; set; }
		public double Width { get; set; }
		public double Top => Rect.Top;
		public double Left => Rect.Left;
		public double Height => Rect.Height;

		public string Line { get; set; }
		public Brush Brush { get; set; }
		public int Stroke { get; set; }
		public string StrokeDash { get; set; }

		// Values that is used to determine if item is visible
		public int ChildRow { get; set; }
		public int ParentRow { get; set; }
	}
}