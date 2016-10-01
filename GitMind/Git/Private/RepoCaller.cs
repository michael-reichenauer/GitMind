using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Git.Private
{
	internal class RepoCaller : IRepoCaller
	{
		public R UseRepo(
			string workingFolder,
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "")
		{
			Log.Debug($"Start {memberName} in {workingFolder} ...");
			try
			{
				using (GitRepository gitRepository = GitRepository.Open(workingFolder))
				{
					doAction(gitRepository);

					Log.Debug($"Done  {memberName} in {workingFolder}");

					return R.Ok;
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
				return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
			}
		}

		public R UseLibRepo(
			string workingFolder,
			Action<Repository> doAction,
			[CallerMemberName] string memberName = "")
		{
			Log.Debug($"Start {memberName} in {workingFolder} ...");
			try
			{
				using (Repository gitRepository = new Repository(workingFolder))
				{
					doAction(gitRepository);

					Log.Debug($"Done  {memberName} in {workingFolder}");

					return R.Ok;
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
				return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
			}
		}


		public Task<R> UseRepoAsync(
			string workingFolder,
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "")
		{
			return Task.Run(() => UseRepo(workingFolder, doAction, memberName));
		}


		public Task<R> UseLibRepoAsync(string workingFolder, Action<Repository> doAction, string memberName = "")
		{
			return Task.Run(() => UseLibRepo(workingFolder, doAction, memberName));
		}


		public async Task<R> UseRepoAsync(
			string workingFolder,
			TimeSpan timeout,
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "")
		{
			CancellationTokenSource cts = new CancellationTokenSource(timeout);

			try
			{
				return await Task.Run(() => UseRepo(workingFolder, doAction, memberName), cts.Token)
					.WithCancellation(cts.Token);
			}
			catch (OperationCanceledException e)
			{
				Log.Warn($"Timeout for {memberName} in {workingFolder}, {e.Message}");
				Error error = Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
				return error;
			}
		}


		public R<T> UseRepo<T>(
			string workingFolder,
			Func<GitRepository, T> doFunction,
			[CallerMemberName] string memberName = "")
		{
			Log.Debug($"Start {memberName} in {workingFolder} ...");
			try
			{
				using (GitRepository gitRepository = GitRepository.Open(workingFolder))
				{
					T functionResult = doFunction(gitRepository);

					R<T> result = R.From(functionResult);

					Log.Debug($"Done  {memberName} in {workingFolder}");

					return result;
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
				return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
			}
		}


		public R<T> UseRepo<T>(
			string workingFolder, 
			Func<Repository, T> doFunction, 
			string memberName = "")
		{
			Log.Debug($"Start {memberName} in {workingFolder} ...");
			try
			{
				using (Repository gitRepository = new Repository(workingFolder))
				{
					T functionResult = doFunction(gitRepository);

					R<T> result = R.From(functionResult);

					Log.Debug($"Done  {memberName} in {workingFolder}");

					return result;
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
				return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
			}
		}


		public R<T> UseLibRepo<T>(
			string workingFolder,
			Func<Repository, T> doFunction,
			[CallerMemberName] string memberName = "")
		{
			Log.Debug($"Start {memberName} in {workingFolder} ...");
			try
			{
				using (Repository repository = new Repository(workingFolder))
				{
					T functionResult = doFunction(repository);

					R<T> result = R.From(functionResult);

					Log.Debug($"Done  {memberName} in {workingFolder}");

					return result;
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
				return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
			}
		}


		public R UseRepo(
			string workingFolder,
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "")
		{
			Log.Debug($"Start {memberName} in {workingFolder} ...");
			try
			{
				using (GitRepository gitRepository = GitRepository.Open(workingFolder))
				{
					R result = doFunction(gitRepository);

					Log.Debug($"Done  {memberName} in {workingFolder}");

					return result;
				}
			}
			catch (Exception e)
			{
				Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
				return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
			}
		}

		public Task<R> UseRepoAsync(
			string workingFolder,
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "")
		{
			return Task.Run(() => UseRepo(workingFolder, doFunction, memberName));
		}


		public async Task<R> UseRepoAsync(
			string workingFolder,
			TimeSpan timeout,
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "")
		{
			CancellationTokenSource cts = new CancellationTokenSource(timeout);

			try
			{
				return await Task.Run(() => UseRepo(workingFolder, doFunction, memberName), cts.Token)
					.WithCancellation(cts.Token);
			}
			catch (OperationCanceledException e)
			{
				Log.Warn($"Timeout for {memberName} in {workingFolder}, {e.Message}");
				Error error = Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
				return error;
			}
		}


		public Task<R<T>> UseRepoAsync<T>(
			string workingFolder,
			Func<GitRepository, T> doFunction,
			[CallerMemberName] string memberName = "")
		{
			return Task.Run(() => UseRepo(workingFolder, doFunction, memberName));
		}


		public Task<R<T>> UseLibRepoAsync<T>(
			string workingFolder, 
			Func<Repository, T> doFunction, 
			string memberName = "")
		{
			return Task.Run(() => UseLibRepo(workingFolder, doFunction, memberName));
		}


		public Task<R<T>> UseRepoAsync<T>(
			string workingFolder,
			Func<GitRepository, Task<T>> doFunction,
			[CallerMemberName] string memberName = "")
		{
			return Task.Run(async () =>
			{
				Log.Debug($"{memberName} in {workingFolder} ...");
				try
				{
					using (GitRepository gitRepository = GitRepository.Open(workingFolder))
					{
						T functionResult = await doFunction(gitRepository);

						R<T> result = R.From(functionResult);

						Log.Debug($"Done {memberName} in {workingFolder}");

						return result;
					}
				}
				catch (Exception e)
				{
					Log.Warn($"Failed to {memberName} in {workingFolder}, {e.Message}");
					return Error.From(e, $"Failed to {memberName} in {workingFolder}, {e.Message}");
				}
			});
		}
	}
}