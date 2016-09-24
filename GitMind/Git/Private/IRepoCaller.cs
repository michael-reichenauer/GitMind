using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal interface IRepoCaller
	{
		R UseRepo(
			string workingFolder,
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "");


		Task<R> UseRepoAsync(
			string workingFolder,
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "");


		Task<R> UseRepoAsync(
			string workingFolder,
			TimeSpan timeout,
			Action<GitRepository> doAction,
			[CallerMemberName] string memberName = "");


		R<T> UseRepo<T>(
			string workingFolder,
			Func<GitRepository, T> doFunction,
			[CallerMemberName] string memberName = "");


		R UseRepo(
			string workingFolder,
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "");


		Task<R> UseRepoAsync(
			string workingFolder,
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "");


		Task<R> UseRepoAsync(
			string workingFolder,
			TimeSpan timeout,
			Func<GitRepository, R> doFunction,
			[CallerMemberName] string memberName = "");


		Task<R<T>> UseRepoAsync<T>(
			string workingFolder,
			Func<GitRepository, T> doFunction,
			[CallerMemberName] string memberName = "");


		Task<R<T>> UseRepoAsync<T>(
			string workingFolder,
			Func<GitRepository, Task<T>> doFunction,
			[CallerMemberName] string memberName = "");
	}
}