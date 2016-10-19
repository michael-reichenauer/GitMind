using System;
using System.IO;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	internal class CacheService : ICacheService
	{
		private readonly TaskThrottler TaskThrottler = new TaskThrottler(1);


		public async Task CacheAsync(MRepository repository)
		{
			//await Task.Yield();
			await WriteRepository(repository);
		}


		public bool IsRepositoryCached(string workingFolder)
		{
			string cachePath = GetCachePath(workingFolder);
			return File.Exists(cachePath);
		}


		public async Task<MRepository> TryGetRepositoryAsync(string gitRepositoryPath)
		{
			//await Task.Yield();
			//return null;
			MRepository repository = await TryReadRepositoryAsync(gitRepositoryPath);
			if (repository != null)
			{
				repository.CommitsFiles = new CommitsFiles();
			}

			return repository;
		}


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
			try
			{
				string tempPath = cachePath + ".tmp." + Guid.NewGuid();
				string tempPath2 = cachePath + ".tmp." + Guid.NewGuid();

				SerializeData(data, tempPath);

				if (File.Exists(cachePath))
				{
					File.Move(cachePath, tempPath2);
				}

				if (File.Exists(tempPath))
				{
					File.Move(tempPath, cachePath);
				}

				Task.Run(() =>
				{
					try
					{
						if (File.Exists(tempPath2))
						{
							File.Delete(tempPath2);
						}
					}
					catch (Exception e) when(e.IsNotFatal())
					{
						Log.Warn($"Failed to delete {tempPath2}, {e.Message}");
					}			
				}).RunInBackground();
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to serialize data {e.Message}");
			}		
		}


		private static void SerializeData<T>(T data, string path)
		{
			try
			{
				using (var file = File.Create(path))
				{
					Serializer.Serialize(file, data);
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to serialize data {e.Message}");

				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}			
		}


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
				Log.Warn($"Failed to read cache {e.Message}");
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
						CommitFiles commitFiles = Serializer.DeserializeWithLengthPrefix<CommitFiles>(file);
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