using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using GitMind.Git;
using GitMind.Git.Private;
using GitMind.Utils;
using Newtonsoft.Json;
//using JsonSerializer = GitMind.Utils.JsonSerializer;


namespace GitMind.GitModel.Private
{
	internal class CacheService : ICacheService
	{
		private const int CurrentMajorVersion = 1;
		private const int CurrentMinorVersion = 0;

		//private readonly JsonSerializer serializer = new JsonSerializer();
		private readonly TaskThrottler TaskThrottler = new TaskThrottler(1);
		private bool isUpdateing;


		public async Task Cache(MRepository repository)
		{
			await TaskThrottler.Run(() => Task.Run(() =>
			{
				Log.Debug("Caching repository ...");
				string cachePath = GetCachePath(null);
				Timing t = new Timing();
				repository.PrepareForSerialization();
				t.Log("PrepareForSerialization");

				Serialize(cachePath, repository);

				t.Log($"Wrote jason data for {repository.Commits.Count} commits");
			}));

			var r = await GetFromCache();
		}

		public async Task<MRepository> GetFromCache()
		{
			return await TaskThrottler.Run(() => Task.Run(() =>
			{
				Log.Debug("Reading cached repository ...");
				string cachePath = GetCachePath(null);
				Timing t = new Timing();

				MRepository repository = Deserialize<MRepository>(cachePath);
				t.Log($"Read jason data for {repository.CommitList.Count} commits");

				repository.CompleteDeserialization();
				t.Log("CompleteDeserialization");
				return repository;
			}));
		}


		public async Task UpdateAsync(string path, IGitRepo gitRepo)
		{
			if (isUpdateing)
			{
				return;
			}

			try
			{
				isUpdateing = true;

				await TaskThrottler.Run(() => Task.Run(() =>
				 {
					 string cachePath = GetCachePath(path);
					 Timing t = new Timing();

					 RepositoryDto repositoryDto = ToRepo(gitRepo);

					 t.Log("copied data");

					 Serialize(cachePath, repositoryDto);

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
						RepositoryDto repositoryDto = Deserialize<RepositoryDto>(cachePath);

						t.Log("Read json data");

						IGitRepo gitRepo = ToGitRepo(repositoryDto);

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


		private void Serialize<T>(string cachePath, T data)
		{
			// string tempPath = cachePath + Guid.NewGuid();	
			using (FileStream fs = File.Open(cachePath, FileMode.OpenOrCreate))
			using (StreamWriter sw = new StreamWriter(fs))
			using (JsonWriter jw = new JsonTextWriter(sw))
			{
				jw.Formatting = Formatting.Indented;

				JsonSerializer serializer = new JsonSerializer();
				serializer.Serialize(jw, data);
			}
		}


		private T Deserialize<T>(string cachePath)
		{
			using (StreamReader file = File.OpenText(cachePath))
			{
				JsonSerializer serializer = new JsonSerializer();
				T deserialize = (T)serializer.Deserialize(file, typeof(T));
				return deserialize;
			}
		}


		private static GitRepo ToGitRepo(RepositoryDto repositoryDto)
		{
			return new GitRepo(
				ToGitBranches(repositoryDto.Branches),
				ToGitCommits(repositoryDto.Commits),
				ToGitTags(repositoryDto.Tags),
				null,
				null);
		}


		private static RepositoryDto ToRepo(IGitRepo gitRepo)
		{
			return new RepositoryDto
			{
				Branches = ToBranches(gitRepo.GetAllBranches()),
				Commits = ToCommits(gitRepo.GetAllCommts()),
				Tags = ToTags(gitRepo.GetAllTags()),
				CurrentCommitId = gitRepo.CurrentCommit.Id
			};
		}


		private static List<BranchDto> ToBranches(IEnumerable<GitBranch> branches)
		{
			return branches.Select(
				b => new BranchDto
				{
					Name = b.Name,
					LatestCommitId = b.LatestCommitId,
					LatestTrackingCommitId = null,
					TrackingBranchName = b.TrackingBranchName,
					IsCurrent = b.IsCurrent,
					IsRemote = b.IsRemote,
					IsAnonyous = false
				})
				.ToList();
		}


		private static List<CommitDto> ToCommits(IEnumerable<GitCommit> commits)
		{
			return commits.Select(
				c => new CommitDto
				{
					Id = c.Id,
					Author = c.Author,
					ParentIds = c.ParentIds.ToList(),
					DateTime = c.AuthorDate,
					CommitDate = c.CommitDate,
					BranchName = null,
					Subject = c.Subject
				})
				.ToList();
		}


		private static List<TagDto> ToTags(IReadOnlyList<GitTag> tags)
		{
			return tags.Select(t => new TagDto { TagName = t.TagName, CommitId = t.CommitId }).ToList();
		}


		private static List<GitTag> ToGitTags(IEnumerable<TagDto> tags)
		{
			return tags.Select(t => new GitTag(t.CommitId, t.TagName))
				.ToList();
		}


		private static IReadOnlyList<GitCommit> ToGitCommits(IEnumerable<CommitDto> commits)
		{
			return commits.Select(c => new GitCommit(
				c.Id,
				c.Subject,
				c.Author,
				c.ParentIds,
				c.DateTime,
				c.CommitDate))
				.ToList();
		}


		private static IReadOnlyList<GitBranch> ToGitBranches(IEnumerable<BranchDto> branches)
		{
			return branches.Select(
				b => new GitBranch(
					b.Name,
					b.LatestCommitId,
					b.IsCurrent,
					b.TrackingBranchName,
					b.IsRemote))
				.ToList();
		}


		private static string GetCachePath(string path)
		{
			string workingFolderPath = path ?? Environment.CurrentDirectory;
			return Path.Combine(workingFolderPath, ".git", "gitmind.cache");
		}



		[DataContract]
		public class RepositoryDto
		{
			[DataMember]
			public int MajorVersion { get; set; } = CurrentMajorVersion;
			public int MinorVersion { get; set; } = CurrentMinorVersion;

			[DataMember]
			public List<CommitDto> Commits { get; set; }
			[DataMember]
			public string CurrentCommitId { get; set; }
			[DataMember]
			public List<BranchDto> Branches { get; set; }
			[DataMember]
			public List<TagDto> Tags { get; set; }
		}

		[DataContract]
		public class CommitDto
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
		public class BranchDto
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
		public class TagDto
		{
			[DataMember]
			public string CommitId { get; set; }
			[DataMember]
			public string TagName { get; set; }
		}
	}
}