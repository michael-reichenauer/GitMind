using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Git.Private
{
	internal interface IRepoCaller
	{
		R UseRepo(
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "");


		Task<R> UseRepoAsync(
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "");

		Task<R> UseRepoAsync(
			Action<LibGit2Sharp.Repository> doAction,
			[CallerMemberName] string memberName = "");

		Task<R> UseLibRepoAsync(
			Action<LibGit2Sharp.Repository> doAction,
			[CallerMemberName] string memberName = "");


		Task<R> UseRepoAsync(
			TimeSpan timeout,
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "");

		Task<R> UseRepoAsync(
			TimeSpan timeout,
			Action<LibGit2Sharp.Repository> doAction,
			[CallerMemberName] string memberName = "");


		R<T> UseRepo<T>(
			Func<GitRepository, T> doFunction,
			[CallerMemberName] string memberName = "");

		R<T> UseRepo<T>(
			Func<LibGit2Sharp.Repository, T> doFunction,
			[CallerMemberName] string memberName = "");


		R UseRepo(
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "");

		R UseLibRepo(
			Func<LibGit2Sharp.Repository, R> doFunction,
			[CallerMemberName] string memberName = "");


		Task<R> UseRepoAsync(
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "");

		Task<R> UseRepoAsync(
			TimeSpan timeout,
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "");

		Task<R> UseRepoAsync(
			TimeSpan timeout,
			Func<LibGit2Sharp.Repository, R> doFunction,
			[CallerMemberName] string memberName = "");

		Task<R<T>> UseRepoAsync<T>(
			Func<GitRepository, T> doFunction,
			[CallerMemberName] string memberName = "");

		Task<R<T>> UseLibRepoAsync<T>(
			Func<LibGit2Sharp.Repository, T> doFunction,
			[CallerMemberName] string memberName = "");


		Task<R<T>> UseRepoAsync<T>(
			Func<GitRepository, Task<T>> doFunction,
			[CallerMemberName] string memberName = "");


		R UseLibRepo(
			Action<Repository> doAction,
			[CallerMemberName] string memberName = "");
	}
}