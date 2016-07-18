using System.Windows;
using System.Windows.Media;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class MergeViewModel : ViewModel
	{
		public int ZIndex => 100;
		public string Id { get; set; }

		public string Type => nameof(MergeViewModel);

		public int ChildRow { get; set; }
		public int ParentRow { get; set; }
		public Rect Rect { get; set; }
		public double Width { get; set; }
		public string Line { get; set; }
		public Brush Brush { get; set; }
		public int Stroke { get; set; }
		public string StrokeDash => "";

		public override string ToString() => Id;
	}
}