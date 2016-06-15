using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GitMind.Utils;
using Newtonsoft.Json;
using ProtoBuf;


//using JsonSerializer = GitMind.Utils.JsonSerializer;


namespace GitMind.GitModel.Private
{
	internal class CacheService : ICacheService
	{
		private readonly TaskThrottler TaskThrottler = new TaskThrottler(10);


		public async Task CacheAsync(MRepository repository)
		{
			await WriteRepository(repository);

			await WriteCommitFilesAsync(repository);
		}


		public async Task<MRepository> TryGetAsync()
		{
			//await Task.Yield();
			//return null;
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

				//Serialize(cachePath, repository);
				//t.Log($"Wrote repository with {repository.Commits.Count} commits");

				Serialize2(cachePath, repository);
				t.Log($"Wrote repository 2222 with {repository.Commits.Count} commits");
			}));
		}


		private async Task WriteCommitFilesAsync(MRepository repository)
		{
			IDictionary<string, IList<CommitFile>> filesById = await repository.CommitsFilesTask;

			await TaskThrottler.Run(() => Task.Run(() =>
			{
				Log.Debug("Caching commit files  ...");
				string cachePath = GetCachePath(null) + ".files";
				Timing t = new Timing();

				Serialize2(cachePath, filesById);

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

				MRepository repository = Deserialize2<MRepository>(cachePath);
				if (repository == null)
				{
					Log.Debug("No cached repository");
					return null;
				}
				t.Log($"Read repository for {repository.CommitList.Count} commits");

				if (repository.Version != MRepository.CurrentVersion)
				{
					Log.Warn(
						$"Cached version differs {repository.Version} != Current {MRepository.CurrentVersion}");
					return null;
				}

				//MRepository repository2 = Deserialize2<MRepository>(cachePath);
				//if (repository2 == null)
				//{
				//	Log.Debug("No 222 cached repository");
				//}
				//else
				//{
				//	t.Log($"Read 2222 repository for {repository2.CommitList.Count} commits");
				//}
			

				repository.CompleteDeserialization();
				t.Log("CompleteDeserialization");
				return repository;
			}));
		}


		public async Task<IDictionary<string, IList<CommitFile>>> ReadCommitFilesAsync()
		{
			return await TaskThrottler.Run(() => Task.Run(() =>
			{
				Log.Debug("Reading cached commit files ...");
				string cachePath = GetCachePath(null) + ".files";
				Timing t = new Timing();

				IDictionary<string, IList<CommitFile>> commitsFiles =
				Deserialize2<IDictionary<string, IList<CommitFile>>>(cachePath);

				commitsFiles = commitsFiles ?? new Dictionary<string, IList<CommitFile>>();
				t.Log($"Read commits file for {commitsFiles.Count} commits");

				return commitsFiles;
			}));
		}


		//private void Serialize<T>(string cachePath, T data)
		//{
		//	string tempPath = cachePath + ".tmp." + Guid.NewGuid();
		//	string tempPath2 = cachePath + ".tmp." + Guid.NewGuid();

		//	using (FileStream fs = File.Open(tempPath, FileMode.Create))
		//	using (StreamWriter sw = new StreamWriter(fs))
		//	using (JsonWriter jw = new JsonTextWriter(sw))
		//	{
		//		JsonSerializer serializer = new JsonSerializer();
		//		serializer.Serialize(jw, data);
		//	}

		//	if (File.Exists(cachePath))
		//	{
		//		File.Move(cachePath, tempPath2);
		//	}

		//	File.Move(tempPath, cachePath);

		//	Task.Run(() =>
		//	{
		//		if (File.Exists(tempPath2))
		//		{
		//			File.Delete(tempPath2);
		//		}
		//	}).RunInBackground();
		//}


		private void Serialize2<T>(string cachePath, T data)
		{
			cachePath += ".2";
			string tempPath = cachePath + ".tmp." + Guid.NewGuid();
			string tempPath2 = cachePath + ".tmp." + Guid.NewGuid();

			using (var file = File.Create(tempPath))
			{
				Serializer.Serialize(file, data);
			}

			if (File.Exists(cachePath))
			{
				File.Move(cachePath, tempPath2);
			}

			File.Move(tempPath, cachePath);

			Task.Run(() =>
			{
				if (File.Exists(tempPath2))
				{
					File.Delete(tempPath2);
				}
			}).RunInBackground();
			
		}


		//private T Deserialize<T>(string cachePath)
		//{
		//	if (!File.Exists(cachePath))
		//	{
		//		return default(T);
		//	}

		//	using (StreamReader file = File.OpenText(cachePath))
		//	{
		//		JsonSerializer serializer = new JsonSerializer();
		//		T deserialize = (T)serializer.Deserialize(file, typeof(T));
		//		return deserialize;
		//	}
		//}

		private T Deserialize2<T>(string cachePath)
		{
			cachePath += ".2";

			if (!File.Exists(cachePath))
			{
				return default(T);
			}

			using (var file = File.OpenRead(cachePath))
			{
				return Serializer.Deserialize<T>(file);
			}
		}



		private static string GetCachePath(string path)
		{
			string workingFolderPath = path ?? Environment.CurrentDirectory;
			return Path.Combine(workingFolderPath, ".git", "gitmind.cache");
		}
	}
}