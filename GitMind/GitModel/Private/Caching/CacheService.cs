using System;
using System.IO;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.GitModel.Private.Caching
{
	internal class CacheService : ICacheService
	{
		private readonly AsyncLock asyncLock = new AsyncLock();


		public async Task CacheAsync(MRepository repository)
		{
			// await Task.Yield();
			await WriteRepositoryAsync(repository);
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
			return await TryReadRepositoryAsync(gitRepositoryPath);
		}

		public void TryDeleteCache(string workingFolder)
		{
			try
			{
				string cachePath = GetCachePath(workingFolder);
				string tempPath = cachePath + ".tmp." + Guid.NewGuid();

				// Just moving cache now, it will be deleted the next time the cache written
				if (File.Exists(cachePath))
				{
					File.Move(cachePath, tempPath);
				}
			}
			catch (Exception e) when(e.IsNotFatal())
			{
				Log.Warn($"Failed to delete cache {e}");
			}
		}

		private async Task WriteRepositoryAsync(MRepository repository)
		{
			using (await asyncLock.LockAsync())
			{
				await Task.Run(() =>
				{
					string cachePath = GetCachePath(repository.WorkingFolder);
					Timing t = new Timing();

					Serialize(cachePath, repository);
					t.Log($"Wrote cached repository with {repository.Commits.Count} commits");
				});
			}
		}


		private async Task<MRepository> TryReadRepositoryAsync(string gitRepositoryPath)
		{
			using (await asyncLock.LockAsync())
			{
				return await Task.Run(() =>
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
				});
			}
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
						string folderPath = Path.GetDirectoryName(cachePath);
						string name = Path.GetFileName(cachePath);
						DirectoryInfo dirInfo = new DirectoryInfo(folderPath);
						FileInfo[] files = dirInfo.GetFiles(name + ".tmp.*");
						foreach (FileInfo fileInfo in files)
						{
							TryDeleteFile(fileInfo.FullName);
						}				
					}
					catch (Exception e) when(e.IsNotFatal())
					{
						Log.Warn($"Failed to delete temp files, {e.Message}");
					}			
				}).RunInBackground();
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to serialize data {e.Message}");
			}		
		}


		private static void TryDeleteFile(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					File.Delete(path);
				}
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Warn($"Failed to delete {path}, {e.Message}");
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
				Log.Warn($"Failed to read cache {e}");
				return default(T);
			}
		}


		private static string GetCachePath(string gitRepositoryPath)
		{
			return Path.Combine(gitRepositoryPath, ".git", "gitmind.cache");
		}
	}
}