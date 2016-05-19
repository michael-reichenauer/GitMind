using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal class GitCacheService : IGitCacheService
	{
		private const int CurrentMajorVersion = 1;
		private const int CurrentMinorVersion = 0;

		private readonly JsonSerializer serializer = new JsonSerializer();
		private readonly TaskThrottler TaskThrottler = new TaskThrottler(1);
		private bool isUpdateing;


		public async Task UpdateAsync(string path, IGitRepo gitRepo)
		{
			if (isUpdateing)
			{
				return;
			}

			try
			{
				isUpdateing = true;

				await TaskThrottler.Run(() =>Task.Run(() =>
				{
					string cachePath = GetCachePath(path);
					Timing t = new Timing();

					Repo repo = ToRepo(gitRepo);

					t.Log("copied data");

					Serialize(cachePath, repo);

					t.Log("wrote jason data");
				}));
			}
			finally
			{
				isUpdateing = false;
			}
		}


		public Task<R<IGitRepo>> GetRepoAsync(string path)
		{
			return TaskThrottler.Run(() => Task.Run(() =>
			{
				try
				{
					string cachePath = GetCachePath(path);

					if (File.Exists(cachePath))
					{
						Timing t = new Timing();
						Repo repo = Deserailize(cachePath);

						t.Log("Read json data");

						IGitRepo gitRepo = ToGitRepo(repo);

						t.Log("Copied data");
						return R.From(gitRepo);
					}
				}
				catch (Exception e) when (e.IsNotFatal())
				{
					return e;
				}

				return Error.From("Failed to read cached data");
			}));
		}


		private void Serialize(string cachePath, Repo repo)
		{
			// string tempPath = cachePath + Guid.NewGuid();

			using (Stream stream = File.Create(cachePath))
			{
				serializer.Serialize(repo, stream);
			}
		}


		private Repo Deserailize(string cachePath)
		{
			using (Stream stream = File.OpenRead(cachePath))
			{
				return serializer.Deserialize<Repo>(stream);
			}
		}


		private static GitRepo ToGitRepo(Repo repo)
		{
			return new GitRepo(
				ToGitBranches(repo.Branches),
				ToGitCommits(repo.Commits),
				ToGitTags(repo.Tags),
				repo.CurrentCommitId);
		}


		private static Repo ToRepo(IGitRepo gitRepo)
		{
			return new Repo
			{
				Branches = ToBranches(gitRepo.GetAllBranches()),
				Commits = ToCommits(gitRepo.GetAllCommts()),
				Tags = ToTags(gitRepo.GetAllTags()),
				CurrentCommitId = gitRepo.CurrentCommitId
			};
		}


		private static List<Branch> ToBranches(IEnumerable<GitBranch> branches)
		{
			return branches.Select(
				b => new Branch
				{
					Name = b.Name,
					LatestCommitId = b.LatestCommitId,
					LatestTrackingCommitId = b.LatestTrackingCommitId,
					TrackingBranchName = b.TrackingBranchName,
					IsCurrent = b.IsCurrent,
					IsRemote = b.IsRemote,
					IsAnonyous = b.IsAnonyous
				})
				.ToList();
		}


		private static List<Commit> ToCommits(IEnumerable<GitCommit> commits)
		{
			return commits.Select(
				c => new Commit
				{
					Id = c.Id,
					Author = c.Author,
					ParentIds = c.ParentIds.ToList(),
					DateTime = c.DateTime,
					CommitDate = c.CommitDate,
					BranchName = c.BranchName,
					Subject = c.Subject
				})
				.ToList();
		}


		private static List<Tag> ToTags(IReadOnlyList<GitTag> tags)
		{
			return tags.Select(t => new Tag { TagName = t.TagName, CommitId = t.CommitId }).ToList();
		}


		private static List<GitTag> ToGitTags(IEnumerable<Tag> tags)
		{
			return tags.Select(t => new GitTag(t.CommitId, t.TagName))
				.ToList();
		}


		private static IReadOnlyList<GitCommit> ToGitCommits(IEnumerable<Commit> commits)
		{
			return commits.Select(c => new GitCommit(
				c.Id,
				c.Subject,
				c.Author,
				c.ParentIds,
				c.DateTime,
				c.CommitDate,
				c.BranchName))
				.ToList();
		}


		private static IReadOnlyList<GitBranch> ToGitBranches(IEnumerable<Branch> branches)
		{
			return branches.Select(
				b => new GitBranch(
					b.Name,
					b.LatestCommitId,
					b.IsCurrent,
					b.TrackingBranchName,
					b.LatestTrackingCommitId,
					b.IsRemote,
					b.IsAnonyous))
				.ToList();
		}


		private static string GetCachePath(string path)
		{
			string workingFolderPath = path ?? Environment.CurrentDirectory;
			return Path.Combine(workingFolderPath, ".git", "gitmind.cache");
		}



		[DataContract]
		public class Repo
		{
			[DataMember]
			public int MajorVersion { get; set; } = CurrentMajorVersion;
			public int MinorVersion { get; set; } = CurrentMinorVersion;

			[DataMember]
			public List<Commit> Commits { get; set; }
			[DataMember]
			public string CurrentCommitId { get; set; }
			[DataMember]
			public List<Branch> Branches { get; set; }
			[DataMember]
			public List<Tag> Tags { get; set; }
		}

		[DataContract]
		public class Commit
		{
			[DataMember]
			public string Id { get; set; }
			[DataMember]
			public string Author { get; set; }
			[DataMember]
			public List<string> ParentIds { get; set; }
			[DataMember]
			public DateTime DateTime { get; set; }
			[DataMember]
			public DateTime CommitDate { get; set; }
			[DataMember]
			public string BranchName { get; set; }
			[DataMember]
			public string Subject { get; set; }
		}

		[DataContract]
		public class Branch
		{
			[DataMember]
			public string Name { get; set; }
			[DataMember]
			public string LatestCommitId { get; set; }
			[DataMember]
			public bool IsCurrent { get; set; }
			[DataMember]
			public string TrackingBranchName { get; set; }
			[DataMember]
			public string LatestTrackingCommitId { get; set; }
			[DataMember]
			public bool IsRemote { get; set; }
			[DataMember]
			public bool IsAnonyous { get; set; }
		}

		[DataContract]
		public class Tag
		{
			[DataMember]
			public string CommitId { get; set; }
			[DataMember]
			public string TagName { get; set; }
		}
	}
}