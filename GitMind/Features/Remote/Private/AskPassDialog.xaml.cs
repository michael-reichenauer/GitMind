using System.Windows;


namespace GitMind.Features.Remote.Private
{
	/// <summary>
	/// Interaction logic for AskPassDialog.xaml
	/// </summary>
	public partial class AskPassDialog : Window
	{
		private readonly AskPassDialogViewModel viewModel;


		public AskPassDialog(Window owner)
		{
			Owner = owner;
			InitializeComponent();

			viewModel = new AskPassDialogViewModel();
			DataContext = viewModel;
			ResponseText.Focus();
		}

		public string Prompt { set => viewModel.PromptText = value; }
	}
}
