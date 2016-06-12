using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitMind.Utils;
using Newtonsoft.Json;
//using JsonSerializer = GitMind.Utils.JsonSerializer;


namespace GitMind.GitModel.Private
{
	internal class CacheService : ICacheService
	{
		private readonly TaskThrottler TaskThrottler = new TaskThrottler(1);


		public async Task CacheAsync(MRepository repository)
		{
			await WriteRepository(repository);

			await WriteCommitFilesAsync(repository);
		}


		public async Task<MRepository> TryGetAsync()
		{
			MRepository repository = await TryReadRepositoryAsync();
			if (repository != null)
			{
				repository.CommitsFilesTask = ReadCommitFilesAsync();
			}

			return repository;
		}


		private async Task WriteRepository(MRepository repository)
		{
			await TaskThrottler.Run(() => Task.Run(() =>
			{
				Log.Debug("Caching repository ...");
				string cachePath = GetCachePath(null);
				Timing t = new Timing();
				repository.PrepareForSerialization();
				t.Log("PrepareForSerialization");

				Serialize(cachePath, repository);

				t.Log($"Wrote repository with {repository.Commits.Count} commits");
			}));
		}


		private async Task WriteCommitFilesAsync(MRepository repository)
		{
			var filesById = await repository.CommitsFilesTask;

			await TaskThrottler.Run(() => Task.Run(() =>
			{
				Log.Debug("Caching commit files  ...");
				string cachePath = GetCachePath(null) + ".files";
				Timing t = new Timing();

				Serialize(cachePath, filesById);

				t.Log($"Wrote commit files for {filesById.Count} commits");
			}));
		}


		public async Task<MRepository> TryReadRepositoryAsync()
		{
			return await TaskThrottler.Run(() => Task.Run(() =>
			{
				Log.Debug("Reading cached repository ...");
				string cachePath = GetCachePath(null);
				Timing t = new Timing();

				MRepository repository = Deserialize<MRepository>(cachePath);
				if (repository == null)
				{
					Log.Debug("No cached repository");
					return null;
				}

				if (repository.Version != MRepository.CurrentVersion)
				{
					Log.Warn(
						$"Cached version differs {repository.Version} != Current {MRepository.CurrentVersion}");
					return null;
				}

				t.Log($"Read repository for {repository.CommitList.Count} commits");

				repository.CompleteDeserialization();
				t.Log("CompleteDeserialization");
				return repository;
			}));
		}


		public async Task<IDictionary<string, IEnumerable<CommitFile>>> ReadCommitFilesAsync()
		{
			return await TaskThrottler.Run(() => Task.Run(() =>
			{
				Log.Debug("Reading cached commit files ...");
				string cachePath = GetCachePath(null) + ".files";
				Timing t = new Timing();

				IDictionary<string, IEnumerable<CommitFile>> commitsFiles =
				Deserialize<IDictionary<string, IEnumerable<CommitFile>>>(cachePath);

				commitsFiles = commitsFiles ?? new Dictionary<string, IEnumerable<CommitFile>>();
				t.Log($"Read commits file for {commitsFiles.Count} commits");

				return commitsFiles;
			}));
		}


		private void Serialize<T>(string cachePath, T data)
		{
			string tempPath = cachePath + ".tmp." + Guid.NewGuid();
			string tempPath2 = cachePath + ".tmp." + Guid.NewGuid();

			using (FileStream fs = File.Open(tempPath, FileMode.Create))
			using (StreamWriter sw = new StreamWriter(fs))
			using (JsonWriter jw = new JsonTextWriter(sw))
			{
				JsonSerializer serializer = new JsonSerializer();
				serializer.Serialize(jw, data);
			}

			if (File.Exists(cachePath))
			{
				File.Move(cachePath, tempPath2);
			}

			File.Move(tempPath, cachePath);

			if (File.Exists(tempPath2))
			{
				File.Delete(tempPath2);
			}
		}


		private T Deserialize<T>(string cachePath)
		{
			if (!File.Exists(cachePath))
			{
				return default(T);
			}

			using (StreamReader file = File.OpenText(cachePath))
			{
				JsonSerializer serializer = new JsonSerializer();
				T deserialize = (T)serializer.Deserialize(file, typeof(T));
				return deserialize;
			}
		}


		private static string GetCachePath(string path)
		{
			string workingFolderPath = path ?? Environment.CurrentDirectory;
			return Path.Combine(workingFolderPath, ".git", "gitmind.cache");
		}
	}
}