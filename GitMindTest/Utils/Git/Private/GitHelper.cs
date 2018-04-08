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

		private bool isRepo = false;


		public GitHelper(AutoMock am, IoHelper io)
		{
			this.am = am;
			this.workingFolder = io.WorkingFolder;
			this.io = io;
		}


		public string OriginUri { get; private set; }


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

			OriginUri = io.CreateTmpDir();

			R result = await Service<IGitRepoService>().InitBareAsync(OriginUri, ct);
			Assert.IsTrue(result.IsOk);

			Directory.CreateDirectory(workingFolder);
			result = await Service<IGitRepoService>().CloneAsync(OriginUri, workingFolder, null, ct);
			Assert.IsTrue(result.IsOk);
			isRepo = true;
		}


		public async Task CloneRepoAsync(string uri)
		{
			if (isRepo)
			{
				throw new InvalidOperationException("Working folder repo already created");
			}

			OriginUri = uri;

			Directory.CreateDirectory(workingFolder);
			R result = await Service<IGitRepoService>().CloneAsync(OriginUri, workingFolder, null, ct);
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


		public async Task BrancheAsync(string name, bool isCheckout = true)
		{
			R result = await Service<IGitBranchService2>().BranchAsync(name, isCheckout, ct);
			Assert.IsTrue(result.IsOk);
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


		public async Task FetchAsync()
		{
			R result = await Service<IGitFetchService>().FetchAsync(ct);
			Assert.IsTrue(result.IsOk);
		}

		public async Task CheckoutAsync(string branchName)
		{
			R result = await Service<IGitCheckoutService>().CheckoutAsync(branchName, ct);
			Assert.IsTrue(result.IsOk);
		}

		public async Task MergeAsync(string branchName)
		{
			R result = await Service<IGitMergeService2>().MergeAsync(branchName, ct);
			Assert.IsTrue(result.IsOk);
		}
	}
}