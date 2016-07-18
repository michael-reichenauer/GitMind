using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class BranchViewModel : ViewModel
	{
		private readonly ICommand showBranchCommand;

		public string Type => "Branch";
		public int ZIndex => 200;

		public BranchViewModel(ICommand showBranchCommand, ICommand hideBranchCommand)
		{			
			this.showBranchCommand = showBranchCommand;
			HideBranchCommand = hideBranchCommand;
		}

		public ObservableCollection<BranchItem> ActiveBranches { get; set; }
		public ICommand HideBranchCommand { get; }
		public string Id { get; set; }

		public IReadOnlyList<BranchItem> ChildBranches =>
			BranchItem.GetBranches(
				Branch.GetChildBranches()
					.Where(b => !ActiveBranches.Any(ab => ab.Branch == b))
					.Take(50)
					.ToList(),
				showBranchCommand);

		public IReadOnlyList<BranchNameItem> MultiBranches { get; set; }

		public bool HasChildren => ChildBranches.Count > 0;


		public Branch Branch { get; set; }

		public int BranchColumn { get; set; }


		public bool IsMultiBranch
		{
			get { return Get(); }
			set { Set(value).Notify(nameof(ChildBranches), nameof(MultiBranches)); }
		}

		public string Name
		{
			get { return Get(); }
			set { Set(value).Notify(nameof(HasChildren)); }
		}

		public int LatestRowIndex
		{
			get { return Get(); }
			set { Set(value); }
		}

		public int FirstRowIndex
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

		public Brush HoverBrush => Brush;

		public string BranchToolTip
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string HideBranchText => "Hide branch: " + Branch.Name;

		public override string ToString() => $"{Name}";
	}
}