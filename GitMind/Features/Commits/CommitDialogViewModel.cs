using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.Features.Commits
{
	internal class CommitDialogViewModel : ViewModel
	{
		private readonly string branchName;
		private readonly Func<string, IReadOnlyList<string>, Task<bool>> commitAction;
		private readonly IReadOnlyList<string> files;

		// private static readonly string TestMessage = 
		//	"01234567890123456789012345678901234567890123456789012345678901234567890123456789]";


		public CommitDialogViewModel(
			string branchName, 
			Func<string, IReadOnlyList<string>, Task<bool>> commitAction, 
			IReadOnlyList<string> files,
			Command showUncommittedDiffCommand)
		{
			this.branchName = branchName;
			this.commitAction = commitAction;
			this.files = files;
			ShowUncommittedDiffCommand = showUncommittedDiffCommand;
		}


		public ICommand OkCommand => Command<Window>(SetOK);

		public ICommand CancelCommand => Command<Window>(w => w.DialogResult = false);

		public Command ShowUncommittedDiffCommand { get; }

		public string BranchText => $"Commit on {branchName}";

		public string Message
		{
			get { return Get(); }
			set { Set(value).Notify(nameof(OkCommand)); }
		}


		private void SetOK(Window window)
		{
			if (string.IsNullOrEmpty(Message))
			{
				return;
			}

			Log.Debug($"Commit:\n{Message}");
			files.ForEach(f => Log.Debug($"  {f}"));

			commitAction(Message, files);

			window.DialogResult = true;
		}
	}
}