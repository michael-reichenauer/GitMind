using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.Features.Committing
{
	internal class CommitDialogViewModel : ViewModel
	{
		private readonly string branchName;
		private readonly Func<string, IEnumerable<CommitFile>, Task<bool>> commitActionAsync;
		private readonly IEnumerable<CommitFile> files;
		private readonly Command<string> undoUncommittedFileCommand;

		//private static readonly string TestSubject =
		//"01234567890123456789012345678901234567890123456789]";

		//private static readonly string TestDescription =
		//"012345678901234567890123456789012345678901234567890123456789012345678912]";


		public CommitDialogViewModel(
			string branchName,
			string workingFolder,
			Func<string, IEnumerable<CommitFile>, Task<bool>> commitActionAsync,
			IEnumerable<CommitFile> files,
			Command showUncommittedDiffCommand,
			Command<string> undoUncommittedFileCommand)
		{
			this.branchName = branchName;
			this.commitActionAsync = commitActionAsync;
			this.files = files;
			this.undoUncommittedFileCommand = undoUncommittedFileCommand;
			ShowUncommittedDiffCommand = showUncommittedDiffCommand;

			files.ForEach(f => Files.Add(
				ToCommitFileViewModel(workingFolder, f)));

			//Subject = TestSubject;
			//Description = TestDescription;
		}


		public Command<Window> OkCommand => Command<Window>(SetOK);

		public Command<Window> CancelCommand => Command<Window>(w => w.DialogResult = false);

		public Command ShowUncommittedDiffCommand { get; }

		public Command<string> UndoUncommittedFileCommand => Command<string>(UndoUncommittedFile);


		private void UndoUncommittedFile(string path)
		{
			CommitFileViewModel file = Files.FirstOrDefault(f => f.Name == path);

			if (file != null)
			{
				undoUncommittedFileCommand.Execute(path);

				Files.Remove(file);
			}
		}


		public string BranchText => $"Commit on {branchName}";

		public string Message => GetMessage();


		private string GetMessage()
		{
			if (!string.IsNullOrWhiteSpace(Subject) && !string.IsNullOrWhiteSpace(Description))
			{
				return $"{Subject.Trim()}\n\n{Description.Trim()}";
			}
			else if (!string.IsNullOrWhiteSpace(Subject))
			{
				return Subject.Trim();
			}
			else if (!string.IsNullOrWhiteSpace(Description))
			{
				return Description.Trim();
			}

			return "";
		}


		public string Subject
		{
			get { return Get(); }
			set { Set(value).Notify(nameof(OkCommand)); }
		}

		public string Description
		{
			get { return Get(); }
			set { Set(value).Notify(nameof(OkCommand)); }
		}

		public ObservableCollection<CommitFileViewModel> Files { get; }
			= new ObservableCollection<CommitFileViewModel>();


		private async void SetOK(Window window)
		{
			if (string.IsNullOrWhiteSpace(Message) || Files.Count == 0)
			{
				return;
			}

			Log.Debug($"Commit: \"{Message}\"");
			files.ForEach(f => Log.Debug($"  {f.Path}"));

			await commitActionAsync(Message, files);

			window.DialogResult = true;
		}


		private CommitFileViewModel ToCommitFileViewModel(string workingFolder, CommitFile file)
		{
			return new CommitFileViewModel(file, UndoUncommittedFileCommand)
			{
				WorkingFolder = workingFolder,
				Id = Commit.UncommittedId,
				Name = file.Path,
				Status = file.StatusText
			};
		}
	}
}