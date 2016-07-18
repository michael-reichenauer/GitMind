using System;
using System.IO;
using System.Threading.Tasks;
using GitMind.Utils;
using ProtoBuf;


namespace GitMind.GitModel.Private
{
	internal class CacheService : ICacheService
	{
		private readonly TaskThrottler TaskThrottler = new TaskThrottler(1);


		public async Task CacheAsync(MRepository repository)
		{
			await Task.Yield();
			await WriteRepository(repository);
		}


		public async Task<MRepository> TryGetRepositoryAsync(string gitRepositoryPath)
		{
			//await Task.Yield();
			//return null;
			MRepository repository = await TryReadRepositoryAsync(gitRepositoryPath);
			if (repository != null)
			{
				repository.CommitsFiles = new CommitsFiles();
				// ReadCommitFilesAsync(repository.CommitsFiles).RunInBackground();
			}

			return repository;
		}


		//public async Task CacheCommitFilesAsync(List<CommitFiles> commitsFiles)
		//{
		//	await Task.Yield();
		//	//await WriteCommitFilesAsync(commitsFiles);
		//}



		private async Task WriteRepository(MRepository repository)
		{
			await TaskThrottler.Run(() => Task.Run(() =>
			{
				string cachePath = GetCachePath(repository.WorkingFolder);
				Timing t = new Timing();

				Serialize(cachePath, repository);
				t.Log($"Wrote cached repository with {repository.Commits.Count} commits");
			}));
		}


		//private async Task WriteCommitFilesAsync(IReadOnlyList<CommitFiles> commitsFiles)
		//{
		//	await TaskThrottler.Run(() => Task.Run(() =>
		//	{
		//		//Log.Debug("Caching commit files  ...");
		//		string cachePath = GetCachePath(null) + ".files";
		//		Timing t = new Timing();

		//		SerializeCommitsFiles(cachePath, commitsFiles);

		//		//t.Log($"Wrote commit files for {commitsFiles.Count} commits");
		//	}));
		//}


		public async Task<MRepository> TryReadRepositoryAsync(string gitRepositoryPath)
		{
			return await TaskThrottler.Run(() => Task.Run(() =>
			{
				string cachePath = GetCachePath(gitRepositoryPath);
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

				repository.CompleteDeserialization(gitRepositoryPath);
				t.Log($"Read cached repository with {repository.Commits.Count} commits");
				return repository;
			}));
		}


		public async Task ReadCommitFilesAsync(CommitsFiles commitsFiles)
		{
			await TaskThrottler.Run(() => Task.Run(() =>
			{
				Log.Debug("Reading cached commit files ...");
				string cachePath = GetCachePath(null) + ".files";
				Timing t = new Timing();

				DeserializeCommitsFiles(cachePath, commitsFiles);

				t.Log($"Read commits file for {commitsFiles.Count} commits");
			}));
		}



		private void Serialize<T>(string cachePath, T data)
		{
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


		//private void SerializeCommitsFiles(string cachePath, IReadOnlyList<CommitFiles> commitsFiles)
		//{
		//	using (var file = File.Open(cachePath, FileMode.Append))
		//	{
		//		foreach (CommitFiles commitFiles in commitsFiles)
		//		{
		//			Serializer.SerializeWithLengthPrefix(file, commitFiles, PrefixStyle.Fixed32);
		//		}
		//	}
		//}



		private T Deserialize<T>(string cachePath)
		{
			try
			{
				if (!File.Exists(cachePath))
				{
					return default(T);
				}

				using (var file = File.OpenRead(cachePath))
				{
					return Serializer.Deserialize<T>(file);
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to read cache {e}");
				return default(T);
			}
		}


		private void DeserializeCommitsFiles(string cachePath, CommitsFiles commitsFiles)
		{
			try
			{
				if (!File.Exists(cachePath))
				{
					Log.Debug("No commits files cache");
					return;
				}

				using (var file = File.OpenRead(cachePath))
				{
					while (true)
					{
						CommitFiles commitFiles = Serializer.DeserializeWithLengthPrefix<CommitFiles>(
							file, PrefixStyle.Fixed32);
						if (commitFiles == null)
						{
							return;
						}

						commitsFiles.Add(commitFiles);
					}
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to read cache {e}");
				return;
			}
		}


		private static string GetCachePath(string gitRepositoryPath)
		{
			return Path.Combine(gitRepositoryPath, ".git", "gitmind.cache");
		}
	}
}