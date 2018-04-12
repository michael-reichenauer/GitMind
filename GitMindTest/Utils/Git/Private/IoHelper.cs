using System.IO;


namespace GitMindTest.Utils.Git.Private
{
	public class IoHelper
	{
		public IoHelper()
		{
			WorkingFolder = GetFullTempPath();
		}


		public string WorkingFolder { get; }
		public string FullPath(string subPath) => Path.Combine(WorkingFolder, subPath);

		public void WriteFile(string subPath, string text) => File.WriteAllText(FullPath(subPath), text);
		public string ReadFile(string subPath) => File.ReadAllText(FullPath(subPath));
		public bool ExistsFile(string subPath) => File.Exists(FullPath(subPath));
		public void DeleteFile(string subPath) => File.Delete(FullPath(subPath));

		public void CreateDir(string subPath) => Directory.CreateDirectory(FullPath(subPath));
		public void DeleteDir(string subPath) => Directory.Delete(FullPath(subPath), true);
		public bool ExistsDir(string subPath) => Directory.Exists(FullPath(subPath));

		public bool Exists(string subPath) => ExistsFile(subPath) || ExistsDir(subPath);

		public string GetFullTempPath() => Path.Combine(GetTempBaseDirPath(), Path.GetRandomFileName());


		public string CreateTmpDir()
		{
			string path = GetFullTempPath();
			Directory.CreateDirectory(path);
			return path;
		}


		public string GetTempBaseDirPath()
		{
			string tempPath = Path.GetTempPath();
			//string tempPath = @"C:\Work Files\TestRepos";

			return Path.Combine(tempPath, "GitMindTest");
		}


		public void CleanTempDirs()
		{
			string path = GetTempBaseDirPath();

			ClearReadOnlyFlag(path);

			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
		}


		private static void ClearReadOnlyFlag(string path)
		{
			if (!Directory.Exists(path))
			{
				return;
			}

			DirectoryInfo rootInfo = new DirectoryInfo(path);

			DirectoryInfo[] objectsInfos = rootInfo.GetDirectories("objects", SearchOption.AllDirectories);

			foreach (DirectoryInfo di in objectsInfos)
			{
				FileInfo[] files = di.GetFiles("*", SearchOption.AllDirectories);

				foreach (FileInfo fileInfo in files)
				{
					FileAttributes attributes = File.GetAttributes(fileInfo.FullName);
					if (attributes.HasFlag(FileAttributes.ReadOnly))
					{
						File.SetAttributes(fileInfo.FullName, attributes & ~FileAttributes.ReadOnly);
					}
				}
			}
		}
	}
}