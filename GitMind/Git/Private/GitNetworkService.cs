using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Git.Private
{
	internal class GitNetworkService : IGitNetworkService
	{
		private static readonly string Origin = "origin";
		private static readonly  FetchOptions fetchAllOptions = new FetchOptions
			{ Prune = true, TagFetchMode = TagFetchMode.All };

		private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan PushTimeout = TimeSpan.FromSeconds(30);

		private readonly IRepoCaller repoCaller;


		public GitNetworkService()
			: this(new RepoCaller())
		{
		}


		public GitNetworkService(IRepoCaller repoCaller)
		{
			this.repoCaller = repoCaller;
		}


		public Task<R> FetchAsync(string workingFolder)
		{
			Log.Debug("Fetch all ...");
			return repoCaller.UseRepoAsync(workingFolder, FetchTimeout,
				repo => repo.Fetch(Origin, fetchAllOptions));
		}


		public Task<R> FetchBranchAsync(string workingFolder, BranchName branchName)
		{
			Log.Debug($"Fetch branch {branchName}...");

			string[] refspecs = { $"{branchName}:{branchName}" };

			return FetchRefsAsync(workingFolder, refspecs);
		}


		public Task<R> FetchRefsAsync(string workingFolder, IEnumerable<string> refspecs)
		{
			string refsText = string.Join(",", refspecs);
			Log.Debug($"Fetch refs {refsText} ...");

			return repoCaller.UseRepoAsync(workingFolder, repo =>
				{
					Remote remote = Remote(repo);		
					repo.Network.Fetch(remote, refspecs);
				});
		}


		public Task<R> PushBranchAsync(
			string workingFolder, 
			BranchName branchName, 
			ICredentialHandler credentialHandler)
		{
			Log.Debug($"Push branch {branchName} ...");

			string[] refspecs = { $"refs/heads/{branchName}:refs/heads/{branchName}"};
			return PushRefsAsync(workingFolder, refspecs, credentialHandler);
		}


		public Task<R> PushCurrentBranchAsync(
			string workingFolder, 
			ICredentialHandler credentialHandler)
		{
			Log.Debug("Push current branch ...");

			return repoCaller.UseRepoAsync(workingFolder, PushTimeout, repo =>
				{
					Branch currentBranch = repo.Head;
					string[] refspecs = {$"{currentBranch.CanonicalName}:{currentBranch.CanonicalName}"};
					PushRefs(refspecs, credentialHandler, repo);
				});
		}


		public Task<R> PushRefsAsync(
			string workingFolder,
			IEnumerable<string> refspecs,
			ICredentialHandler credentialHandler)
		{
			string refsText = string.Join(",", refspecs);
			Log.Debug($"Push refs {refsText} ...");

			return repoCaller.UseRepoAsync(workingFolder, PushTimeout,
				repo => PushRefs(refspecs, credentialHandler, repo));
		}


		public Task<R> PublishBranchAsync(
			string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Publish branch {branchName} ...");

			return repoCaller.UseLibRepoAsync(workingFolder, repo =>
			{
				Branch localBranch = repo.Branches.FirstOrDefault(b => branchName.IsEqual(b.FriendlyName));
				if (localBranch == null)
				{
					throw new Exception($"No local branch with name {branchName}");
				}

				PushOptions pushOptions = GetPushOptions(credentialHandler);

				// Check if corresponding remote branch exists
				Branch remoteBranch = repo.Branches
					.FirstOrDefault(b => b.FriendlyName == "origin/" + branchName);

				if (remoteBranch != null)
				{
					// Remote branch exists, so connect local and remote branch
					localBranch = repo.Branches.Add(branchName, remoteBranch.Tip);
					repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
				}
				else
				{
					// Remote branch does not yet exists
					Remote remote = Remote(repo);

					repo.Branches.Update(
						localBranch,
						b => b.Remote = remote.Name,
						b => b.UpstreamBranch = localBranch.CanonicalName);
				}

				repo.Network.Push(localBranch, pushOptions);
			});
		}


		public Task<R> DeleteRemoteBranchAsync(
			string workingFolder, BranchName branchName, ICredentialHandler credentialHandler)
		{
			Log.Debug($"Delete remote branch {branchName} ...");

			return repoCaller.UseRepoAsync(workingFolder, PushTimeout, repo =>
			{
				repo.Branches.Remove(branchName, true);

				PushOptions pushOptions = GetPushOptions(credentialHandler);

				Remote remote = Remote(repo);

				// Using a refspec, like you would use with git push...
				repo.Network.Push(remote, $":refs/heads/{branchName}", pushOptions);

				credentialHandler.SetConfirm(true);
			});
		}


		private static void PushRefs(
			IEnumerable<string> refspecs, 
			ICredentialHandler credentialHandler,
			Repository repo)
		{
			try
			{
				PushOptions pushOptions = GetPushOptions(credentialHandler);

				Remote remote = Remote(repo);

				repo.Network.Push(remote, refspecs, pushOptions);

				credentialHandler.SetConfirm(true);
			}
			catch (NoCredentialException)
			{
				Log.Debug("Canceled enter credentials");
				credentialHandler.SetConfirm(false);
			}
			catch (Exception e)
			{
				Log.Error($"Error {e}");
				credentialHandler.SetConfirm(false);
				throw;
			}
		}


		private static PushOptions GetPushOptions(ICredentialHandler credentialHandler)
		{
			PushOptions pushOptions = new PushOptions();

			pushOptions.CredentialsProvider = (url, usernameFromUrl, types) =>
			{
				NetworkCredential credential = credentialHandler.GetCredential(url, usernameFromUrl);

				if (credential == null)
				{
					throw new NoCredentialException();
				}

				return new UsernamePasswordCredentials
				{
					Username = credential?.UserName,
					Password = credential?.Password
				};
			};

			return pushOptions;
		}


		private static Remote Remote(IRepository repo)
		{
			return repo.Network.Remotes[Origin];
		}


		public class NoCredentialException : Exception
		{
		}
	}
}