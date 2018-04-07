using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.GitModel.Private;
using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMindTest.AutoMocking;
using NUnit.Framework;


namespace GitMindTest.Utils.Git.Private
{
	public class GitHelper
	{
		protected readonly CancellationToken ct = CancellationToken.None;

		private readonly AutoMock am;
		private readonly string workingFolder;
		private readonly IoHelper io;

		private string originUri;
		private bool isRepo = false;


		public GitHelper(AutoMock am, string workingFolder, IoHelper io)
		{
			this.am = am;
			this.workingFolder = workingFolder;
			this.io = io;
		}


		public T Service<T>() => am.Resolve<T>();


		public async Task InitRepoAsync()
		{
			if (isRepo)
			{
				throw new InvalidOperationException("Working folder repo already created");
			}

			Directory.CreateDirectory(workingFolder);
			R result = await Service<IGitRepoService>().InitAsync(workingFolder, ct);
			Assert.IsTrue(result.IsOk);
			isRepo = true;
		}


		public async Task CloneRepoAsync()
		{
			if (isRepo)
			{
				throw new InvalidOperationException("Working folder repo already created");
			}

			originUri = io.CreateTmpDir();

			R result = await Service<IGitRepoService>().InitBareAsync(originUri, ct);
			Assert.IsTrue(result.IsOk);

			Directory.CreateDirectory(workingFolder);
			result = await Service<IGitRepoService>().CloneAsync(originUri, workingFolder, null, ct);
			Assert.IsTrue(result.IsOk);
			isRepo = true;
		}


		public async Task<Status2> GetStatusAsync()
		{
			var result = await Service<IGitStatusService2>().GetStatusAsync(ct);
			Assert.IsTrue(result.IsOk);

			return result.Value;
		}


		public async Task<IReadOnlyList<GitBranch2>> GetBranchesAsync()
		{
			R<IReadOnlyList<GitBranch2>> branches = await Service<IGitBranchService2>().GetBranchesAsync(ct);
			Assert.IsTrue(branches.IsOk);

			return branches.Value;
		}

		public async Task<GitBranch2> GetCurrentBranchAsync()
		{
			var branches = await GetBranchesAsync();

			return branches.First(branch => branch.IsCurrent);
		}


		public async Task<GitCommit> CommitAllChangesAsync(string message)
		{
			R<GitCommit> result = await Service<IGitCommitService2>().CommitAllChangesAsync(message, ct);
			Assert.IsTrue(result.IsOk);
			return result.Value;
		}


		public async Task UncommitAsync()
		{
			R result = await Service<IGitCommitService2>().UnCommitAsync(ct);
			Assert.IsTrue(result.IsOk);
		}


		public async Task UndoUncommitedAsync()
		{
			R result = await Service<IGitCommitService2>().UndoUncommitedAsync(ct);
			Assert.IsTrue(result.IsOk);
		}


		public async Task PushAsync()
		{
			R result = await Service<IGitPushService>().PushAsync(ct);
			Assert.IsTrue(result.IsOk);
		}
	}
}