using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitMind.Common;
using GitMind.Features.Diffing;
using GitMind.GitModel.Private;
using GitMind.Utils;
using LibGit2Sharp;



namespace GitMind.Git
{
	internal class GitRepository : IDisposable
	{
		// string emptyTreeSha = "4b825dc642cb6eb9a060e54bf8d69288fbee4904";;

		private readonly IDiffService diffService;
		private readonly string workingFolder;
		private readonly Repository repository;

		private static readonly StatusOptions StatusOptions =
			new StatusOptions { DetectRenamesInWorkDir = true, DetectRenamesInIndex = true };

		//private static readonly MergeOptions MergeDefault =
		//	new MergeOptions {FastForwardStrategy = FastForwardStrategy.Default};




		private GitRepository(
			IDiffService diffService,
			string workingFolder, 
			Repository repository)
		{
			this.diffService = diffService;
			this.workingFolder = workingFolder;
			this.repository = repository;
		}


		public static GitRepository Open(IDiffService diffService, string folder)
		{
			return new GitRepository(diffService, folder, new Repository(folder));
		}


		//public static IReadOnlyList<string> GetRefsIds(string folder)
		//{
		//	using (Repository repo = new Repository(folder))
		//	{
		//		return repo.Refs.Select(r => r.CanonicalName + r.TargetIdentifier)
		//			.ToList();
		//	}
		//}


		public IEnumerable<GitBranch> Branches => repository.Branches
			.Select(b => new GitBranch(b, repository));

		public IEnumerable<GitTag> Tags => repository.Tags.Select(t => new GitTag(t));

		public GitBranch Head => new GitBranch(repository.Head, repository);

		public GitDiff Diff => new GitDiff(diffService, repository.Diff, repository);

		public string UserName => repository.Config.GetValueOrDefault<string>("user.name");


		public void Dispose()
		{
			repository.Dispose();
		}


		public IReadOnlyList<GitNote> GetCommitNotes(CommitId commitId)
		{
			Commit commit = repository.Lookup<Commit>(new ObjectId(commitId.Sha));
			if (commit != null)
			{
				return commit.Notes
					.Select(note => new GitNote(note.Namespace ?? "", note.Message))
					.ToList();
			}
			else
			{
				Log.Warn($"Could not find commit {commitId}");
			}

			return new GitNote[0];
		}


		public void SetCommitNote(CommitId commitId, GitNote gitNote)
		{
			Signature committer = repository.Config.BuildSignature(DateTimeOffset.Now);

			Commit commit = repository.Lookup<Commit>(new ObjectId(commitId.Sha));
			if (commit != null)
			{
				repository.Notes.Add(commit.Id, gitNote.Message, committer, committer, gitNote.NameSpace);
			}
			else
			{
				Log.Warn($"Could not find commit {commitId}");
			}
		}


		public void GetFile(string fileId, string filePath)
		{
			Blob blob = repository.Lookup<Blob>(new ObjectId(fileId));

			if (blob != null)
			{
				using (var stream = File.Create(filePath))
				{
					blob.GetContentStream().CopyTo(stream);
				}
			}
		}


		public void Resolve(string path)
		{
			string fullPath = Path.Combine(workingFolder, path);
			Log.Debug($"Resolving {path}");
			if (File.Exists(fullPath))
			{
				repository.Index.Add(path);
			}
			else
			{
				repository.Remove(path);
			}

			// Temp workaround to trigger status update after resolving conflicts, ill be handled better
			string tempPath = fullPath + ".tmp";
			File.AppendAllText(tempPath, "tmp");
			File.Delete(tempPath);
		}


		public GitCommit GetCommit(string commitSha)
		{
			Commit commit = repository.Lookup<Commit>(new ObjectId(commitSha));

			return new GitCommit(
				commit.Sha,
				commit.MessageShort,
				commit.Author.Name,
				commit.Author.When.LocalDateTime,
				commit.Committer.When.LocalDateTime,
				commit.Parents.Select(p => new CommitId(p.Sha)).ToList());
		}
	}
}