using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.Features.Committing
{
	internal class CommitDialogViewModel : ViewModel
	{
		private readonly string branchName;
		private readonly Func<string, IEnumerable<CommitFile>, Task<bool>> commitAction;
		private readonly IEnumerable<CommitFile> files;

		// private static readonly string TestMessage = 
		//	"01234567890123456789012345678901234567890123456789012345678901234567890123456789]";


		public CommitDialogViewModel(
			string branchName, 
			string workingFolder,
			Func<string, IEnumerable<CommitFile>, Task<bool>> commitAction,
			IEnumerable<CommitFile> files,
			Command showUncommittedDiffCommand)
		{
			this.branchName = branchName;
			this.commitAction = commitAction;
			this.files = files;
			ShowUncommittedDiffCommand = showUncommittedDiffCommand;

			files.ForEach(f => Files.Add(
				ToCommitFileViewModel(workingFolder, f)));
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

		public ObservableCollection<CommitFileViewModel> Files { get; } 
			= new ObservableCollection<CommitFileViewModel>();


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


		private static CommitFileViewModel ToCommitFileViewModel(string workingFolder, CommitFile file)
		{
			return new CommitFileViewModel
			{
				WorkingFolder = workingFolder,
				Id = Commit.UncommittedId,
				Name = file.Name,
				Status = file.Status
			};
		}
	}
}