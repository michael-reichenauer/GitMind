﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GitMind.GitModel;
using GitMind.RepositoryViews;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.Features.Committing
{
	internal class CommitDialogViewModel : ViewModel
	{
		private readonly ICommitService commitService = new CommitService();
		private readonly IRepositoryCommands repositoryCommands;
	
		//private static readonly string TestSubject =
		//"01234567890123456789012345678901234567890123456789]";

		//private static readonly string TestDescription =
		//"012345678901234567890123456789012345678901234567890123456789012345678912]";


		public CommitDialogViewModel(
			IRepositoryCommands repositoryCommands,
			string branchName,
			string workingFolder,
			IEnumerable<CommitFile> files,
			string commitMessage,
			bool isMerging)
		{
			CommitFiles = files.ToList();

			this.repositoryCommands = repositoryCommands;

			files.ForEach(f => Files.Add(
				ToCommitFileViewModel(workingFolder, f)));

			if (!string.IsNullOrWhiteSpace(commitMessage))
			{
				string[] lines = commitMessage.Split("\n".ToCharArray());
				string subject = lines[0];
				string mergeSubjectSuffix = $" into {branchName}";
				if (!subject.EndsWith(mergeSubjectSuffix))
				{
					subject += mergeSubjectSuffix;
				}

				Subject = subject;

				if (lines.Length > 1)
				{
					Description = string.Join("\n", lines.Skip(1));
				}
			}

			BranchText = isMerging ? $"Commit merge to {branchName}" : $"Commit on {branchName}";
			//Subject = TestSubject;
			//Description = TestDescription;
		}


		public Command<Window> OkCommand => Command<Window>(SetOK);

		public Command<Window> CancelCommand => Command<Window>(w => w.DialogResult = false);

		public Command ShowUncommittedDiffCommand => AsyncCommand(
			() => commitService.ShowUncommittedDiff(repositoryCommands));

		public Command<string> UndoUncommittedFileCommand => Command<string>(UndoUncommittedFile);

		public bool IsChanged { get; private set; }

		private void UndoUncommittedFile(string path)
		{
			CommitFileViewModel file = Files.FirstOrDefault(f => f.Name == path);

			if (file != null)
			{
				commitService.UndoUncommittedFileAsync(repositoryCommands, path);

				Files.Remove(file);
				IsChanged = true;
			}
		}


		public string BranchText { get; }

		public string Message => GetMessage();
		public IReadOnlyList<CommitFile> CommitFiles { get; }


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


		private void SetOK(Window window)
		{
			if (string.IsNullOrWhiteSpace(Message) || Files.Count == 0)
			{
				return;
			}

			Log.Debug($"Commit: \"{Message}\"");

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