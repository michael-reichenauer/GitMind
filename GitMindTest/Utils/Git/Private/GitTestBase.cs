using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.OsSystem;
using GitMindTest.AutoMocking;
using NUnit.Framework;


namespace GitMindTest.Utils.Git.Private
{
	public class GitTestBase<TInterface>
	{
		private Lazy<TInterface> resolved;
		protected readonly CancellationToken ct = CancellationToken.None;
		protected AutoMock am;
		protected TInterface gitCmd => resolved.Value;
		private string workingFolder;
		private bool isRepo;

		protected string WorkingFolder
		{
			get
			{
				if (!isRepo)
				{
					throw new InvalidOperationException("Working folder repo not yet created");
				}

				return workingFolder;
			}
		}


		[SetUp]
		public void Setup()
		{
			//CleanTempDirs();

			workingFolder = CreateTmpDir();

			am = new AutoMock()
				.RegisterNamespaceOf<IGitInfo>()
				.RegisterNamespaceOf<ICmd2>()
				.RegisterType<IMessageService>()
				.RegisterSingleInstance(new WorkingFolderPath(workingFolder));
			resolved = new Lazy<TInterface>(() => am.Resolve<TInterface>());
		}



		[TearDown]
		public void Teardown()
		{
			am.Dispose();
			// CleanTempDirs();
		}


		protected async Task InitRepoAsync()
		{
			if (isRepo)
			{
				throw new InvalidOperationException("Working folder repo already created");
			}

			await CreateNewGitRepoAsync(workingFolder);
			isRepo = true;
		}

		protected async Task<Status2> GetStatusAsync()
		{
			IGitStatus gitStatus = am.Resolve<IGitStatus>();
			var result = await gitStatus.GetStatusAsync(ct);
			Assert.IsTrue(result.IsOk);

			return result.Value;
		}

		protected async Task<GitCommit> CommitAllChangesAsync(string message)
		{
			IGitCommit gitCommit = am.Resolve<IGitCommit>();
			R<GitCommit> result = await gitCommit.CommitAllChangesAsync(message, ct);
			Assert.IsTrue(result.IsOk);
			return result.Value;
		}


		protected void WriteFile(string name, string text)
		{
			File.WriteAllText(GetPath(name), text);
		}

		protected void DeleteFile(string name)
		{
			File.Delete(GetPath(name));
		}


		protected string GetPath(string subPath)
		{
			return Path.Combine(WorkingFolder, subPath);
		}

		private async Task<string> GetNewGitRepoAsync()
		{
			string path = CreateTmpDir();
			await CreateNewGitRepoAsync(path);
			return path;
		}

		private async Task CreateNewGitRepoAsync(string path)
		{
			IGitRepo gitRepo = am.Resolve<IGitRepo>();

			R result = await gitRepo.InitAsync(path, ct);
			Assert.IsTrue(result.IsOk);
		}


		protected string CreateTmpDir()
		{
			string path = GetTempDirPath();
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}

			Directory.CreateDirectory(path);
			return path;
		}


		protected string GetTempDirPath()
		{
			string tempDirectory = Path.Combine(GetTempBaseDirPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempDirectory);
			return tempDirectory;
		}


		private string GetTempBaseDirPath()
		{
			//string tempPath = Path.GetTempPath();
			string tempPath = @"C:\Work Files\TestRepos";

			return Path.Combine(tempPath, "GitMindTest");
		}


		private void CleanTempDirs()
		{
			string path = GetTempBaseDirPath();
			if (Directory.Exists(path))
			{
				Directory.Delete(path, true);
			}
		}
	}
}