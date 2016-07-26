using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GitMind.Utils.UI;


namespace GitMind.Features.Commits
{
	internal class CommitDialogViewModel : ViewModel
	{
		private readonly string branchName;
		private readonly Func<string, Task<bool>> commitAction;

		//private static readonly string TestMessage = 
		//	"01234567890123456789012345678901234567890123456789012345678901234567890123456789]";


		public CommitDialogViewModel()
		{
		}

		public CommitDialogViewModel(string branchName, Func<string, Task<bool>> commitAction)
		{
			this.branchName = branchName;
			this.commitAction = commitAction;
		}


		public ICommand OkCommand => Command<Window>(SetOK);

		public ICommand CancelCommand => Command<Window>(w => w.DialogResult = false);

		public string BranchText => $"Commit to branch: {branchName}";

		public string Message
		{
			get { return Get(); }
			set { Set(value); }
		}


		private void SetOK(Window window)
		{
			commitAction(Message);

			window.DialogResult = true;
		}
	}
}