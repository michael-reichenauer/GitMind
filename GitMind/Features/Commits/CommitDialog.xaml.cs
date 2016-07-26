using System.Windows;


namespace GitMind.Features.Commits
{
	/// <summary>
	/// Interaction logic for CommitDialog.xaml
	/// </summary>
	public partial class CommitDialog : Window
	{
		public CommitDialog()
		{
			InitializeComponent();
			DataContext = new CommitDialogViewModel();
		}
	}
}
