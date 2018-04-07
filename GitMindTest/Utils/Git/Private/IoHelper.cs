using System.IO;


namespace GitMindTest.Utils.Git.Private
{
	public class IoHelper
	{
		public IoHelper()
		{
			WorkingFolder = GetTempDirPath();
		}


		public string WorkingFolder { get; }

		public void WriteFile(string subPath, string text) => File.WriteAllText(FullPath(subPath), text);

		public string ReadFile(string subPath) => File.ReadAllText(FullPath(subPath));

		public bool FileExists(string subPath) => File.Exists(FullPath(subPath));

		public void DeleteFile(string subPath) => File.Delete(FullPath(subPath));

		public string FullPath(string subPath) => Path.Combine(WorkingFolder, subPath);


		public string GetTempDirPath() =>
			Path.Combine(GetTempBaseDirPath(), Path.GetRandomFileName());


		public string CreateTmpDir()
		{
			string path = GetTempDirPath();
			Directory.CreateDirectory(path);
			return path;
		}


		public string GetTempBaseDirPath()
		{
			//string tempPath = Path.GetTempPath();
			string tempPath = @"C:\Work Files\TestRepos";

			return Path.Combine(tempPath, "GitMindTest");
		}


		public void CleanTempDirs()
		{
			string path = GetTempBaseDirPath();
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
		}
	}
}