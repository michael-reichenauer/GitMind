using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal class GitCommitBranchNameService : IGitCommitBranchNameService
	{
		private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);

		private readonly IRepoCaller repoCaller;
		private static readonly string CommitBranchNoteNameSpace = "GitMind.Branches";
		private static readonly string ManualBranchNoteNameSpace = "GitMind.Branches.Manual";


		public GitCommitBranchNameService()
			: this(new RepoCaller())
		{		
		}

		public GitCommitBranchNameService(IRepoCaller repoCaller)
		{
			this.repoCaller = repoCaller;
		}


		public async Task EditCommitBranchNameAsync(
			string workingFolder, 
			string commitId,
			string rootId,
			BranchName branchName,
			ICredentialHandler credentialHandler)
		{
			Log.Debug($"Set manual branch name {branchName} for commit {commitId} ...");
			SetNoteBranches(workingFolder, ManualBranchNoteNameSpace, commitId, branchName);

			await PushNotesAsync(workingFolder, ManualBranchNoteNameSpace, rootId, credentialHandler);
		}


		public Task SetCommitBranchNameAsync(string workingFolder, string commitId, BranchName branchName)
		{
			Log.Debug($"Set commit branch name {branchName} for commit {commitId} ...");
			SetNoteBranches(workingFolder, CommitBranchNoteNameSpace, commitId, branchName);

			return Task.CompletedTask;
		}


		public IReadOnlyList<CommitBranchName> GetEditedBranchNames(string workingFolder, string rootId)
		{
			return GetNoteBranches(workingFolder, ManualBranchNoteNameSpace, rootId);
		}


		public IReadOnlyList<CommitBranchName> GetCommitBrancheNames(string workingFolder, string rootId)
		{
			return GetNoteBranches(workingFolder, CommitBranchNoteNameSpace, rootId);
		}


		public async Task FetchAllNotesAsync(string workingFolder)
		{
			Log.Debug("Fetch all notes ...");
			string[] noteRefs = {
				$"refs/notes/{CommitBranchNoteNameSpace}:refs/notes/origin/{CommitBranchNoteNameSpace}",
				$"refs/notes/{ManualBranchNoteNameSpace}:refs/notes/origin/{ManualBranchNoteNameSpace}",
			};

			await repoCaller.UseRepoAsync(workingFolder, FetchTimeout, repo => repo.FetchRefs(noteRefs));
		}


		public async Task PushNotesAsync(
			string workingFolder, string rootId, ICredentialHandler credentialHandler)
		{
			await PushNotesAsync(workingFolder, CommitBranchNoteNameSpace, rootId, credentialHandler);
			await PushNotesAsync(workingFolder, ManualBranchNoteNameSpace, rootId, credentialHandler);
		}


		private void SetNoteBranches(
			string workingFolder, string nameSpace, string commitId, BranchName branchName)
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


		private async Task PushNotesAsync(
			string workingFolder, string nameSpace, string rootId, ICredentialHandler credentialHandler)
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

			await FetchNotesAsync(workingFolder, nameSpace);

			string originNotesText = repoCaller.UseRepo(workingFolder, repo =>
			{
				IReadOnlyList<GitNote> notes = repo.GetCommitNotes(rootId);
				GitNote note = notes.FirstOrDefault(n => n.NameSpace == $"origin/{nameSpace}");

				return note?.Message ?? "";
			})
			.Or("");

			string notesText = originNotesText + addedNotesText;

			repoCaller.UseRepo(workingFolder, repo =>
				repo.SetCommitNote(rootId, new GitNote(nameSpace, notesText)));

			await repoCaller.UseRepoAsync(workingFolder, repo =>
			{
				repo.PushRefs($"refs/notes/{nameSpace}", credentialHandler);

				string file = Path.Combine(workingFolder, ".git", nameSpace);
				if (File.Exists(file))
				{
					File.Delete(file);
				}
			});
		}


		private async Task FetchNotesAsync(string workingFolder, string nameSpace)
		{
			Log.Debug($"Fetch notes for {nameSpace} ...");
			string[] noteRefs =
			{
				$"+refs/notes/{nameSpace}:refs/notes/origin/{nameSpace}",
				$"+refs/notes/{nameSpace}:refs/notes/{nameSpace}"
			};

			await repoCaller.UseRepoAsync(workingFolder, FetchTimeout, repo => repo.FetchRefs(noteRefs));
		}


		private IReadOnlyList<CommitBranchName> GetNoteBranches(
			string workingFolder, string nameSpace, string rootId)
		{
			Log.Debug($"Getting notes {nameSpace} from root commit {rootId} ...");

			string notesText = repoCaller.UseRepo(workingFolder, repo =>
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
			branchNames.Skip(Math.Max(0, branchNames.Count - 5)).ForEach(n => Log.Debug($"  {n}"));

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