using System.Windows;
using System.Windows.Media;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class MergeViewModel : ViewModel
	{
		// UI properties
		public int ZIndex => 100;
		public string Id { get; set; }
		public string Type => nameof(MergeViewModel);	
		public Rect Rect { get; set; }
		public double Width { get; set; }
		public string Line { get; set; }
		public Brush Brush { get; set; }
		public int Stroke { get; set; }
		public string StrokeDash => "";

		// Values that is used to determine if item is visible
		public int ChildRow { get; set; }
		public int ParentRow { get; set; }

		public override string ToString() => Id;
	}
}