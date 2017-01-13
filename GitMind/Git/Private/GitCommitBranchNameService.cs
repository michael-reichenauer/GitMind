using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common;
using GitMind.GitModel.Private;
using GitMind.RepositoryViews;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal class GitCommitBranchNameService : IGitCommitBranchNameService
	{
		public static readonly string CommitBranchNoteNameSpace = "GitMind.Branches";
		public static readonly string ManualBranchNoteNameSpace = "GitMind.Branches.Manual";

		private readonly WorkingFolder workingFolder;
		private readonly IRepoCaller repoCaller;
		private readonly Lazy<IRepositoryMgr> repositoryMgr;
		private readonly IGitNetworkService gitNetworkService;


		public GitCommitBranchNameService(
			WorkingFolder workingFolder,
			IRepoCaller repoCaller,
			Lazy<IRepositoryMgr> repositoryMgr,
			IGitNetworkService gitNetworkService)
		{
			this.workingFolder = workingFolder;
			this.repoCaller = repoCaller;
			this.repositoryMgr = repositoryMgr;
			this.gitNetworkService = gitNetworkService;
		}


		public async Task EditCommitBranchNameAsync(
			CommitSha commitSha, CommitSha rootSha, BranchName branchName)
		{
			Log.Debug($"Set manual branch name {branchName} for commit {commitSha} ...");
			SetNoteBranches(ManualBranchNoteNameSpace, commitSha, branchName);

			await PushNotesAsync(ManualBranchNoteNameSpace, rootSha);
		}


		public Task SetCommitBranchNameAsync(CommitSha commitSha, BranchName branchName)
		{
			SetNoteBranches(CommitBranchNoteNameSpace, commitSha, branchName);

			return Task.CompletedTask;
		}


		public IReadOnlyList<CommitBranchName> GetEditedBranchNames(CommitSha rootSha)
		{
			return GetNoteBranches(ManualBranchNoteNameSpace, rootSha);
		}


		public IReadOnlyList<CommitBranchName> GetCommitBrancheNames(CommitSha rootId)
		{
			return GetNoteBranches(CommitBranchNoteNameSpace, rootId);
		}


		public async Task PushNotesAsync(CommitSha rootId)
		{
			await PushNotesAsync(CommitBranchNoteNameSpace, rootId);
			await PushNotesAsync(ManualBranchNoteNameSpace, rootId);
		}


		private void SetNoteBranches(
			string nameSpace, CommitSha commitSha, BranchName branchName)
		{
			try
			{
				string file = Path.Combine(workingFolder, ".git", nameSpace);
				CommitId id = new CommitId(commitSha);
				File.AppendAllText(file, $"{id} {branchName}\n");
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to add commit name for {commitSha} {branchName}, {e}");
			}
		}


		private async Task PushNotesAsync(string nameSpace, CommitSha rootId)
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

			await FetchNotesAsync(nameSpace);

			string originNotesText = repoCaller.UseRepo(repo =>
			{
				IReadOnlyList<GitNote> notes = repo.GetCommitNotes(rootId);
				GitNote note = notes.FirstOrDefault(n => n.NameSpace == $"origin/{nameSpace}");

				return note?.Message ?? "";
			})
			.Or("");

			string notesText = MergeNotes(originNotesText, addedNotesText);

			if (notesText == originNotesText)
			{
				Log.Debug($"Notes {nameSpace} have not changed");
				return;
			}

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


		private string MergeNotes(string originNotesText, string addedNotesText)
		{
			List<CommitBranchName> branchNames = ParseBranchNames(originNotesText + addedNotesText);

			Dictionary<string, BranchName> nameById = new Dictionary<string, BranchName>();

			List<CommitBranchName> mergedNames = new List<CommitBranchName>();
			foreach (CommitBranchName commitBranchName in branchNames)
			{
				if (nameById.TryGetValue(commitBranchName.Id, out BranchName branchName))
				{
					if (commitBranchName.Name == branchName)
					{
						// Ignore Duplicate
						continue;
					}
					else
					{
						// Later entry indicate a change, remove old entry
						var existing = mergedNames.Find(n => n.Id == commitBranchName.Id);
						mergedNames.Remove(existing);
						Log.Debug($"Changed {existing}");
						Log.Debug($"  to {commitBranchName}");
					}
				}

				// Normal copy of entry
				nameById[commitBranchName.Id] = commitBranchName.Name;
				mergedNames.Add(commitBranchName);			
			}

			Log.Debug($"Number of merged entries: {mergedNames.Count}");

			StringBuilder sb = new StringBuilder();			
			mergedNames.ForEach(n => sb.Append($"{n.Id} {n.Name}\n"));

			return sb.ToString();
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


		private IReadOnlyList<CommitBranchName> GetNoteBranches(string nameSpace, CommitSha rootId)
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
						string id = parts[0];

						id = TryGetCommitId(id);

						BranchName branchName = parts[1].Trim();
						branchNames.Add(new CommitBranchName(id, branchName));
					}
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to parse notes text, error: {e}\n text:\n{text}");
			}

			return branchNames;
		}


		private string TryGetCommitId(string id)
		{
			if (CommitId.TryParse(id, out CommitId commitId))
			{
				return id;
			}
			else
			{
				var gitCommits = repositoryMgr.Value.Repository?.MRepository?.GitCommits;
				if (gitCommits == null)
				{
					return id;
				}

				var commits = gitCommits.ToList();
				foreach (var pair in commits)
				{
					if (pair.Value.Sha.Sha.StartsWith(id, StringComparison.OrdinalIgnoreCase))
					{
						commitId = new CommitId(pair.Value.Sha);
						return commitId.Id;
					}
				}
			}

			return id;
		}
	}
}