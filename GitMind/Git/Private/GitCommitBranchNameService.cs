﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal class GitCommitBranchNameService : IGitCommitBranchNameService
	{
		public static readonly string CommitBranchNoteNameSpace = "GitMind.Branches";
		public static readonly string ManualBranchNoteNameSpace = "GitMind.Branches.Manual";

		private readonly WorkingFolder workingFolder;
		private readonly IRepoCaller repoCaller;
		private readonly IGitNetworkService gitNetworkService;


		public GitCommitBranchNameService(
			WorkingFolder workingFolder,
			IRepoCaller repoCaller,
			IGitNetworkService gitNetworkService)
		{
			this.workingFolder = workingFolder;
			this.repoCaller = repoCaller;
			this.gitNetworkService = gitNetworkService;
		}


		public async Task EditCommitBranchNameAsync(
			string commitId,
			string rootId,
			BranchName branchName)
		{
			Log.Debug($"Set manual branch name {branchName} for commit {commitId} ...");
			SetNoteBranches(ManualBranchNoteNameSpace, commitId, branchName);

			await PushNotesAsync(ManualBranchNoteNameSpace, rootId);
		}


		public Task SetCommitBranchNameAsync(string commitId, BranchName branchName)
		{
			Log.Debug($"Set commit branch name {branchName} for commit {commitId} ...");
			SetNoteBranches(CommitBranchNoteNameSpace, commitId, branchName);

			return Task.CompletedTask;
		}


		public IReadOnlyList<CommitBranchName> GetEditedBranchNames(string rootId)
		{
			return GetNoteBranches(ManualBranchNoteNameSpace, rootId);
		}


		public IReadOnlyList<CommitBranchName> GetCommitBrancheNames(string rootId)
		{
			return GetNoteBranches(CommitBranchNoteNameSpace, rootId);
		}



		public async Task PushNotesAsync(string rootId)
		{
			await PushNotesAsync(CommitBranchNoteNameSpace, rootId);
			await PushNotesAsync(ManualBranchNoteNameSpace, rootId);
		}


		private void SetNoteBranches(
			string nameSpace, string commitId, BranchName branchName)
		{
			Log.Debug($"Set note {nameSpace} for commit {commitId} with branch {branchName} ...");

			try
			{
				string file = Path.Combine(workingFolder, ".git", nameSpace);
				File.AppendAllText(file, $"{commitId} {branchName}\n");
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to add commit name for {commitId} {branchName}, {e}");
			}
		}


		private async Task PushNotesAsync(string nameSpace, string rootId)
		{
			Log.Debug($"Push notes {nameSpace} at root commit {rootId} ...");

			// git push origin refs/notes/GitMind.Branches
			// git notes --ref=GitMind.Branches merge -s cat_sort_uniq refs/notes/origin/GitMind.Branches
			// git fetch origin refs/notes/GitMind.Branches:refs/notes/origin/GitMind.Branches

			string addedNotesText = "";
			try
			{
				string file = Path.Combine(workingFolder, ".git", nameSpace);
				if (File.Exists(file))
				{
					addedNotesText = File.ReadAllText(file);
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to read local {nameSpace}, {e}");
			}

			if (string.IsNullOrWhiteSpace(addedNotesText))
			{
				Log.Debug("Notes is empty, no need to push notes");
				return;
			}
			else
			{
				Log.Debug($"Adding notes:\n{addedNotesText}");
			}

			await FetchNotesAsync(nameSpace);

			string originNotesText = repoCaller.UseRepo(repo =>
			{
				IReadOnlyList<GitNote> notes = repo.GetCommitNotes(rootId);
				GitNote note = notes.FirstOrDefault(n => n.NameSpace == $"origin/{nameSpace}");

				return note?.Message ?? "";
			})
			.Or("");

			string notesText = originNotesText + addedNotesText;

			repoCaller.UseRepo(repo => repo.SetCommitNote(rootId, new GitNote(nameSpace, notesText)));

			string[] refs = { $"refs/notes/{nameSpace}:refs/notes/{nameSpace}" };
			R result = await gitNetworkService.PushRefsAsync(refs);
			if (result.IsOk)
			{
				string file = Path.Combine(workingFolder, ".git", nameSpace);
				if (File.Exists(file))
				{
					File.Delete(file);
				}
			}
		}


		public async Task<R> FetchAllNotesAsync()
		{
			Log.Debug("Fetch all notes ...");
			string[] noteRefs =
			{
				$"+refs/notes/{CommitBranchNoteNameSpace}:refs/notes/origin/{CommitBranchNoteNameSpace}",
				$"+refs/notes/{ManualBranchNoteNameSpace}:refs/notes/origin/{ManualBranchNoteNameSpace}",
			};

			return await gitNetworkService.FetchRefsAsync(noteRefs);
		}


		private async Task<R> FetchNotesAsync(string nameSpace)
		{
			Log.Debug($"Fetch notes for {nameSpace} ...");
			string[] noteRefs =
			{
				$"+refs/notes/{nameSpace}:refs/notes/origin/{nameSpace}",
				$"+refs/notes/{nameSpace}:refs/notes/{nameSpace}"
			};

			return await gitNetworkService.FetchRefsAsync(noteRefs);
		}


		private IReadOnlyList<CommitBranchName> GetNoteBranches(string nameSpace, string rootId)
		{
			Log.Debug($"Getting notes {nameSpace} from root commit {rootId} ...");

			string notesText = repoCaller.UseRepo(repo =>
			{
				IReadOnlyList<GitNote> notes = repo.GetCommitNotes(rootId);
				GitNote note = notes.FirstOrDefault(n => n.NameSpace == $"origin/{nameSpace}");
				return note?.Message ?? "";
			})
			.Or("");

			try
			{
				string file = Path.Combine(workingFolder, ".git", nameSpace);
				if (File.Exists(file))
				{
					notesText += File.ReadAllText(file);
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to read local {nameSpace}, {e}");
			}

			List<CommitBranchName> branchNames = ParseBranchNames(notesText);

			Log.Debug($"Got {branchNames.Count} branch names for {nameSpace}");

			return branchNames;
		}

		private List<CommitBranchName> ParseBranchNames(string text)
		{
			List<CommitBranchName> branchNames = new List<CommitBranchName>();

			try
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					return branchNames;
				}

				string[] lines = text.Split("\n".ToCharArray());
				foreach (string line in lines)
				{
					string[] parts = line.Split(" ".ToCharArray());
					if (parts.Length == 2)
					{
						string commitId = parts[0];
						BranchName branchName = parts[1].Trim();
						branchNames.Add(new CommitBranchName(commitId, branchName));
					}
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to parse notes text, error: {e}\n text:\n{text}");
			}

			return branchNames;
		}
	}
}