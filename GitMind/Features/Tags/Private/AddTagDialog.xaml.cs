using System.Windows;
using GitMind.Git;


namespace GitMind.Features.Tags.Private
{
	/// <summary>
	/// Interaction logic for CrateBranchDialog.xaml
	/// </summary>
	public partial class AddTagDialog : Window
	{
		private readonly AddTagDialogViewModel viewModel;


		public AddTagDialog(Window owner)
		{
			Owner = owner;
			InitializeComponent();

			viewModel = new AddTagDialogViewModel();
			DataContext = viewModel;
			AddTagText.Focus();
		}

		public string TagText => viewModel.TagText;
	}
}
