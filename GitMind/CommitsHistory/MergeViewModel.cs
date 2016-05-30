using System.Windows;
using System.Windows.Media;
using GitMind.Utils.UI;


namespace GitMind.CommitsHistory
{
	internal class MergeViewModel : ViewModel
	{
		public string Type => "Merge";

		public int ChildRow
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int ParentRow
		{
			get { return Get(); }
			set { Set(value); }
		}

		public Rect Rect
		{
			get { return Get(); }
			set { Set(value); }
		}

		public double Width
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Line
		{
			get { return Get(); }
			set { Set(value); }
		}

		public Brush Brush
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int Stroke
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string StrokeDash
		{
			get { return Get(); }
			set { Set(value); }
		}
	}
}