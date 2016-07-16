using System.Windows;
using System.Windows.Media;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class MergeViewModel : ViewModel, IVirtualItem
	{
		public MergeViewModel(string id, int virtualId)
		{
			Id = id;
			VirtualId = virtualId;
		}

		public int ZIndex => 100;
		public string Id { get; }
		public int VirtualId { get; }

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

		public override string ToString() => Id;
	}
}