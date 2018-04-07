using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Common.MessageDialogs;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
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
		protected Status2 status;
		protected IReadOnlyList<GitBranch2> branches;

		private string workingFolder;
		private string originUri;
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

		protected string OriginUri =>
			originUri ?? throw new InvalidOperationException("Origin repo not yet created");



		[SetUp]
		public void Setup()
		{
			// CleanTempDirs();
			isRepo = false;
			workingFolder = GetTempDirPath();
			status = new Status2(0, 0, 0, 0, new GitFile2[0]);

			am = new AutoMock()
				.RegisterNamespaceOf<IGitInfoService>()
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

			IGitRepoService gitRepoService = am.Resolve<IGitRepoService>();

			Directory.CreateDirectory(workingFolder);
			R result = await gitRepoService.InitAsync(workingFolder, ct);
			Assert.IsTrue(result.IsOk);
			isRepo = true;
		}


		protected async Task CloneRepoAsync()
		{
			if (isRepo)
			{
				throw new InvalidOperationException("Working folder repo already created");
			}

			IGitRepoService gitRepoService = am.Resolve<IGitRepoService>();

			originUri = DirCreateTmp();

			R result = await gitRepoService.InitBareAsync(originUri, ct);
			Assert.IsTrue(result.IsOk);

			Directory.CreateDirectory(workingFolder);
			result = await gitRepoService.CloneAsync(originUri, workingFolder, null, ct);
			Assert.IsTrue(result.IsOk);
			isRepo = true;
		}


		protected async Task<Status2> GetStatusAsync()
		{
			IGitStatusService2 gitStatusService2 = am.Resolve<IGitStatusService2>();
			var result = await gitStatusService2.GetStatusAsync(ct);
			Assert.IsTrue(result.IsOk);

			return result.Value;
		}


		protected async Task<IReadOnlyList<GitBranch2>> GetBranchesAsync()
		{
			IGitBranchService2 branchService2 = am.Resolve<IGitBranchService2>();
			R<IReadOnlyList<GitBranch2>> branches = await branchService2.GetBranchesAsync(ct);
			Assert.IsTrue(branches.IsOk);

			return branches.Value;
		}

		protected async Task<GitBranch2> GetCurrentBranchAsync()
		{
			var branches = await GetBranchesAsync();

			return branches.First(branch => branch.IsCurrent);
		}

		protected async Task<GitCommit> CommitAllChangesAsync(string message)
		{
			IGitCommitService2 gitCommitService2 = am.Resolve<IGitCommitService2>();
			R<GitCommit> result = await gitCommitService2.CommitAllChangesAsync(message, ct);
			Assert.IsTrue(result.IsOk);
			return result.Value;
		}

		protected async Task UncommitAsync()
		{
			IGitCommitService2 gitCommitService2 = am.Resolve<IGitCommitService2>();
			R result = await gitCommitService2.UnCommitAsync(ct);
			Assert.IsTrue(result.IsOk);
		}

		protected async Task UndoUncommitedAsync()
		{
			IGitCommitService2 gitCommitService2 = am.Resolve<IGitCommitService2>();
			R result = await gitCommitService2.UndoUncommitedAsync(ct);
			Assert.IsTrue(result.IsOk);
		}


		protected async Task PushAsync()
		{
			IGitPushService gitPushService = am.Resolve<IGitPushService>();
			R result = await gitPushService.PushAsync(ct);
			Assert.IsTrue(result.IsOk);
		}

		protected void FileWrite(string name, string text) => File.WriteAllText(FileFullPath(name), text);

		protected string FileRead(string name) => File.ReadAllText(FileFullPath(name));

		protected bool FileExists(string name) => File.Exists(FileFullPath(name));

		protected void FileDelete(string name) => File.Delete(FileFullPath(name));

		protected string FileFullPath(string subPath) => Path.Combine(WorkingFolder, subPath);


		protected string DirCreateTmp()
		{
			string path = GetTempDirPath();
			Directory.CreateDirectory(path);
			return path;
		}


		private string GetTempDirPath()
		{
			return Path.Combine(GetTempBaseDirPath(), Path.GetRandomFileName());
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