using System.Windows;


namespace GitMind.Features.Branching
{
	/// <summary>
	/// Interaction logic for BranchDialog.xaml
	/// </summary>
	public partial class BranchDialog : Window
	{
		private readonly BranchDialogViewModel viewModel;


		public BranchDialog(Window owner)
		{
			Owner = owner;
			InitializeComponent();

			viewModel = new BranchDialogViewModel();
			DataContext = viewModel;
			BranchNameText.Focus();
		}


		public string BranchName => viewModel.BranchName;
	}
}
