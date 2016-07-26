using System;
using System.Threading.Tasks;
using System.Windows;


namespace GitMind.Features.Commits
{
	/// <summary>
	/// Interaction logic for CommitDialog.xaml
	/// </summary>
	public partial class CommitDialog : Window
	{
		public CommitDialog(string branchName, Func<string, Task<bool>> commitAction)
		{
			InitializeComponent();
			DataContext = new CommitDialogViewModel(branchName, commitAction);
			MessageText.Focus();
		}
	}
}
