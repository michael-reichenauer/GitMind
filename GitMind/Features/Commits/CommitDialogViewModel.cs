using System.Windows;
using System.Windows.Input;
using GitMind.Utils.UI;


namespace GitMind.Features.Commits
{
	internal class CommitDialogViewModel : ViewModel
	{
		public ICommand OkCommand { get; } = new Command<Window>(w => w.DialogResult = true);

		public ICommand CancelCommand { get; } = new Command<Window>(w => w.DialogResult = false);


	}
}