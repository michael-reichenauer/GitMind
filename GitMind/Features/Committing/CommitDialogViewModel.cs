﻿using System;
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

		// private static readonly string TestMessage = 
		// "0123456789001234567890012345678900123456789001234567890]";


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
		}


		public Command<Window> OkCommand => Command<Window>(SetOK);

		public Command<Window> CancelCommand => Command<Window>(w => w.DialogResult = false);

		public Command ShowUncommittedDiffCommand { get; }

		public string BranchText => $"Commit on {branchName}";

		public string Message
		{
			get { return Get(); }
			set { Set(value).Notify(nameof(OkCommand)); }
		}

		public ObservableCollection<CommitFileViewModel> Files { get; }
			= new ObservableCollection<CommitFileViewModel>();


		private async void SetOK(Window window)
		{
			if (string.IsNullOrEmpty(Message))
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
			return new CommitFileViewModel(undoUncommittedFileCommand)
			{
				WorkingFolder = workingFolder,
				Id = Commit.UncommittedId,
				Name = file.Path,
				Status = file.Status
			};
		}
	}
}