using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Common;
using GitMind.Features.Diffing.Private;
using GitMind.Git;
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


		//public T Service<T>() => am.Resolve<T>();


		public async Task InitRepoAsync()
		{
			if (isRepo)
			{
				throw new InvalidOperationException("Working folder repo already created");
			}

			Directory.CreateDirectory(workingFolder);
			await Service<IGitRepoService>().Call(s => s.InitAsync(workingFolder, ct));
			isRepo = true;
		}


		public async Task CloneRepoAsync()
		{
			if (isRepo)
			{
				throw new InvalidOperationException("Working folder repo already created");
			}

			OriginUri = io.CreateTmpDir();

			await Service<IGitRepoService>().Call(s => s.InitBareAsync(OriginUri, ct));

			Directory.CreateDirectory(workingFolder);
			await Service<IGitRepoService>().Call(s => s.CloneAsync(OriginUri, workingFolder, null, ct));
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
			await Service<IGitRepoService>().Call(s => s.CloneAsync(OriginUri, workingFolder, null, ct));
			isRepo = true;
		}

		public Task<GitStatus2> GetStatusAsync() => Service<IGitStatusService2>().Call(s => s.GetStatusAsync(ct));

		public Task<GitConflicts> GetConflictsAsync() => Service<IGitStatusService2>().Call(s => s.GetConflictsAsync(ct));

		public Task<string> GetConflictFileAsync(string fileId) => Service<IGitStatusService2>().Call(s => s.GetConflictFile(fileId, ct));

		public Task<IReadOnlyList<GitBranch2>> GetBranchesAsync() => Service<IGitBranchService2>().Call(s => s.GetBranchesAsync(ct));

		public async Task<GitBranch2> GetCurrentBranchAsync() => (await GetBranchesAsync()).First(branch => branch.IsCurrent);

		public Task BranchAsync(string name, bool isCheckout = true) => Service<IGitBranchService2>().Call(s => s.BranchAsync(name, isCheckout, ct));

		public Task<GitCommit> CommitAllChangesAsync(string message) => Service<IGitCommitService2>().Call(s => s.CommitAllChangesAsync(message, ct));

		public Task UncommitAsync() => Service<IGitCommitService2>().Call(s => s.UnCommitAsync(ct));

		public Task UndoUncommitedAsync() => Service<IGitStatusService2>().Call(s => s.UndoAllUncommittedAsync(ct));

		public Task CleanWorkingFolderAsync() => Service<IGitStatusService2>().Call(s => s.UndoAllUncommittedAsync(ct));

		public Task PushAsync() => Service<IGitPushService>().Call(s => s.PushAsync(ct));

		public Task FetchAsync() => Service<IGitFetchService>().Call(s => s.FetchAsync(ct));

		public Task CheckoutAsync(string branchName) => Service<IGitCheckoutService>().Call(s => s.CheckoutAsync(branchName, ct));

		public Task MergeAsync(string branchName) => Service<IGitMergeService2>().Call(s => s.MergeAsync(branchName, ct));

		public Task<IReadOnlyList<GitCommit>> GetLogAsync() => Service<IGitLogService>().Call(s => s.GetLogAsync(ct));

		public Task<GitCommit> GetCommit(string sha) => Service<IGitCommitService2>().Call(s => s.GetCommitAsync(sha, ct));


		public Task<CommitDiff> ParsePatchAsync(CommitSha commitSha, string patch, bool addPrefixes = true) =>
			am.Resolve<IGitDiffParser>().ParseAsync(commitSha, patch, addPrefixes, false);

		public SomeService<T> Service<T>() => new SomeService<T>(am.Resolve<T>());


		public class SomeService<T>
		{
			private readonly T service;

			public SomeService(T service) => this.service = service;

			public async Task<TResult> Call<TResult>(Func<T, Task<R<TResult>>> func)
			{
				R<TResult> result = await func(service);
				Assert.IsTrue(result.IsOk);
				return result.Value; ;
			}

			public async Task Call(Func<T, Task<R>> func)
			{
				R result = await func(service);
				Assert.IsTrue(result.IsOk);
			}
		}
	}
}