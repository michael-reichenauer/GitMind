using System.Windows;
using GitMind.Git;


namespace GitMind.Features.Branching.Private
{
	/// <summary>
	/// Interaction logic for CrateBranchDialog.xaml
	/// </summary>
	public partial class CrateBranchDialog : Window
	{
		private readonly CreateBranchDialogViewModel viewModel;


		public CrateBranchDialog(Window owner)
		{
			Owner = owner;
			InitializeComponent();

			viewModel = new CreateBranchDialogViewModel();
			DataContext = viewModel;
			BranchNameText.Focus();
		}


		public BranchName BranchName => viewModel.BranchName;
		public bool IsPublish => viewModel.IsPublish;
	}
}
