using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GitMind.Common.Tracking;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Git.Private
{
	internal class GitNetworkService : IGitNetworkService
	{
		private static readonly string Origin = "origin";

		private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan PushTimeout = TimeSpan.FromSeconds(30);


		private readonly IRepoCaller repoCaller;
		private readonly ICredentialHandler credentialHandler;


		public GitNetworkService(
			IRepoCaller repoCaller,
			ICredentialHandler credentialHandler)
		{
			this.repoCaller = repoCaller;
			this.credentialHandler = credentialHandler;
		}


		//public Task<R> FetchAsync()
		//{
		//	FetchOptions fetchOptions = GetFetchOptions(
		//		new FetchOptions { Prune = true, TagFetchMode = TagFetchMode.All });

		//	Log.Debug("Fetch all ...");
		//	return repoCaller.UseRepoAsync(FetchTimeout, repo =>
		//	{
		//		Timing timing = new Timing();
		//		string remoteUrl = "";
		//		try
		//		{
		//			if (!HasRemote(repo))
		//			{
		//				Log.Debug("No 'origin' remote, skipping fetch");
		//				return;
		//			}

		//			remoteUrl = Remote(repo).Url;
		//			repo.Fetch(Origin, fetchOptions);
		//			credentialHandler.SetConfirm(true);
		//			Track.Dependency("Fetch", remoteUrl, timing.Elapsed, true);
		//		}
		//		catch (NoCredentialException e)
		//		{
		//			Log.Debug("Canceled enter credentials");
		//			credentialHandler.SetConfirm(false);
		//			Log.Exception(e, "");
		//			Track.Dependency("Fetch", remoteUrl, timing.Elapsed, false);
		//		}
		//		catch (Exception e)
		//		{
		//			if (IsInvalidProtocol(e))
		//			{
		//				return;
		//			}

		//			Log.Exception(e, "");
		//			credentialHandler.SetConfirm(false);
		//			Track.Dependency("Fetch", remoteUrl, timing.Elapsed, false);
		//			throw;
		//		}
		//	});
		//}


		//public Task<R> FetchBranchAsync(BranchName branchName)
		//{
		//	Log.Debug($"Fetch branch {branchName}...");

		//	string[] refspecs = { $"{branchName}:{branchName}" };

		//	return FetchRefsAsync(refspecs);
		//}


		//public Task<R> FetchRefsAsync(IEnumerable<string> refspecs)
		//{
		//	FetchOptions fetchOptions = GetFetchOptions(new FetchOptions());
		//	string refsText = string.Join(",", refspecs);
		//	Log.Debug($"Fetch refs {refsText} ...");

		//	return repoCaller.UseRepoAsync(repo =>
		//	{
		//		Timing timing = new Timing();
		//		string remoteUrl = "";

		//		try
		//		{
		//			if (!HasRemote(repo))
		//			{
		//				Log.Debug("No 'origin' remote, skipping fetch");
		//				return;
		//			};

		//			Remote remote = Remote(repo);
		//			remoteUrl = remote.Url;
		//			repo.Network.Fetch(remote, refspecs, fetchOptions);
		//			Track.Dependency("FetchRefs", remoteUrl, timing.Elapsed, true);
		//		}
		//		catch (NoCredentialException e)
		//		{
		//			Log.Debug("Canceled enter credentials");
		//			Log.Exception(e, "");
		//			credentialHandler.SetConfirm(false);
		//			Track.Dependency("FetchRefs", remoteUrl, timing.Elapsed, false);
		//		}
		//		catch (Exception e)
		//		{
		//			if (IsInvalidProtocol(e))
		//			{
		//				return;
		//			}

		//			Log.Exception(e, "");
		//			credentialHandler.SetConfirm(false);
		//			Track.Dependency("FetchRefs", remoteUrl, timing.Elapsed, false);
		//			throw;
		//		}
		//	});
		//}


		//public Task<R> PushBranchAsync(BranchName branchName)
		//{
		//	Log.Debug($"Push branch {branchName} ...");

		//	string[] refspecs =
		//	{
		//		$"refs/heads/{branchName}:refs/heads/{branchName}"
		//	};
		//	return PushRefsAsync(refspecs);
		//}




		//public async Task<R> PushTagAsync(string tagCanonicalName)
		//{
		//	Log.Debug($"Push tag {tagCanonicalName} ...");

		//	string[] refspecs = { tagCanonicalName };

		//	return await PushRefsAsync(refspecs);
		//}


		//public Task<R> PushCurrentBranchAsync()
		//{
		//	Log.Debug("Push current branch ...");

		//	return repoCaller.UseRepoAsync(PushTimeout, repo =>
		//		{
		//			Branch currentBranch = repo.Head;
		//			string[] refspecs =
		//			{
		//				$"{currentBranch.CanonicalName}:{currentBranch.CanonicalName}"
		//			};
		//			PushRefs(refspecs, repo);
		//		});
		//}


		//public Task<R> PushRefsAsync(IEnumerable<string> refspecs)
		//{
		//	string refsText = string.Join(",", refspecs);
		//	Log.Debug($"Push refs {refsText} ...");

		//	return repoCaller.UseRepoAsync(PushTimeout, repo =>
		//	{
		//		PushRefs(refspecs, repo);
		//	});
		//}


		//public Task<R> PublishBranchAsync(BranchName branchName)
		//{
		//	Log.Debug($"Publish branch {branchName} ...");

		//	return repoCaller.UseLibRepoAsync(repo =>
		//	{
		//		Timing timing = new Timing();
		//		string remoteUrl = "";

		//		try
		//		{


		//			Branch localBranch = repo.Branches.FirstOrDefault(b => branchName.IsEqual(b.FriendlyName));
		//			if (localBranch == null)
		//			{
		//				throw new Exception($"No local branch with name {branchName}");
		//			}

		//			PushOptions pushOptions = GetPushOptions();

		//			// Check if corresponding remote branch exists
		//			Branch remoteBranch = repo.Branches
		//				.FirstOrDefault(b => b.FriendlyName == "origin/" + branchName);

		//			if (remoteBranch != null)
		//			{
		//				// Remote branch exists, so connect local and remote branch
		//				localBranch = repo.Branches.Add(branchName, remoteBranch.Tip);
		//				repo.Branches.Update(localBranch, b => b.TrackedBranch = remoteBranch.CanonicalName);
		//			}
		//			else
		//			{
		//				// Remote branch does not yet exists
		//				if (repo.Network.Remotes.Any(r => r.Name == Origin))
		//				{
		//					Remote remote = Remote(repo);
		//					remoteUrl = remote.Url;

		//					repo.Branches.Update(
		//						localBranch,
		//						b => b.Remote = remote.Name,
		//						b => b.UpstreamBranch = localBranch.CanonicalName);
		//				}
		//			}

		//			repo.Network.Push(localBranch, pushOptions);
		//			Track.Dependency("PublishBranch", remoteUrl, timing.Elapsed, true);
		//		}
		//		catch (Exception e)
		//		{
		//			if (IsInvalidProtocol(e))
		//			{
		//				return;
		//			}

		//			Log.Exception(e, "");
		//			Track.Dependency("PublishBranch", remoteUrl, timing.Elapsed, false);
		//			throw;
		//		}
		//	});
		//}


		public Task<R> DeleteRemoteBranchAsync(BranchName branchName)
		{
			Log.Debug($"Delete remote branch {branchName} ...");
			Timing timing = new Timing();
			string remoteUrl = "";

			return repoCaller.UseRepoAsync(PushTimeout, repo =>
			{
				try
				{
					if (!HasRemote(repo))
					{
						Log.Debug("No 'origin' remote, skipping delete remote branch");
						return;
					};

					repo.Branches.Remove(branchName, true);

					PushOptions pushOptions = GetPushOptions();

					Remote remote = Remote(repo);
					remoteUrl = remote.Url;

					// Using a refspec, like you would use with git push...
					repo.Network.Push(remote, $":refs/heads/{branchName}", pushOptions);

					credentialHandler.SetConfirm(true);
					Track.Dependency("DeleteRemoteBranch", remoteUrl, timing.Elapsed, true);
				}
				catch (Exception e)
				{
					if (IsInvalidProtocol(e))
					{
						return;
					}

					Log.Exception(e, "");
					credentialHandler.SetConfirm(false);
					Track.Dependency("DeleteRemoteBranch", remoteUrl, timing.Elapsed, false);
					throw;
				}
			});
		}



		public Task<R> DeleteRemoteTagAsync(string tagName)
		{
			Log.Debug($"Delete remote tag {tagName} ...");

			return repoCaller.UseRepoAsync(PushTimeout, repo =>
			{
				Timing timing = new Timing();
				string remoteUrl = "";

				try
				{
					if (!HasRemote(repo))
					{
						Log.Debug("No 'origin' remote, skipping delete remote tag");
						return;
					};

					PushOptions pushOptions = GetPushOptions();

					Remote remote = Remote(repo);
					remoteUrl = remote.Url;

					// Using a refspec, like you would use with git push...
					repo.Network.Push(remote, $":refs/tags/{tagName}", pushOptions);

					credentialHandler.SetConfirm(true);
					Track.Dependency("DeleteRemoteTag", remoteUrl, timing.Elapsed, true);
				}
				catch (Exception e)
				{
					if (IsInvalidProtocol(e))
					{
						return;
					}

					Log.Exception(e, "");
					credentialHandler.SetConfirm(false);
					Track.Dependency("DeleteRemoteTag", remoteUrl, timing.Elapsed, false);
					throw;
				}
			});
		}

		public Task<R> PruneLocalTagsAsync()
		{
			Log.Debug("Prune local tags  ...");

			return repoCaller.UseRepoAsync(PushTimeout, repo =>
			{
				try
				{
					if (!HasRemote(repo))
					{
						Log.Debug("No 'origin' remote, skipping pruning local tags");
						return;
					};

					Remote remote = Remote(repo);

					var refs = repo.Network.ListReferences(remote);
					var remoteTagRefs = refs.Where(r => r.CanonicalName.StartsWith("refs/tags/")).ToList();

					// Should retrieve the local tags
					var allRefs = repo.Refs.Where(r => r.CanonicalName.StartsWith("refs/tags/")).ToList();
					var localTags = allRefs.Where(r => !remoteTagRefs.Contains(r)).ToList();

					foreach (Reference reference in localTags)
					{
						Log.Debug($"Remove {reference.CanonicalName}");
						repo.Refs.Remove(reference);
					}
				}
				catch (Exception e)
				{
					if (IsInvalidProtocol(e))
					{
						return;
					}

					Log.Exception(e, "");
					throw;
				}
			});
		}

		private void PushRefs(IEnumerable<string> refspecs, Repository repo)
		{
			Timing timing = new Timing();
			string remoteUrl = "";

			try
			{
				if (!HasRemote(repo))
				{
					Log.Debug("No 'origin' remote, skipping delete remote branch");
					return;
				};

				PushOptions pushOptions = GetPushOptions();

				Remote remote = Remote(repo);
				remoteUrl = remote.Url;

				repo.Network.Push(remote, refspecs, pushOptions);

				credentialHandler.SetConfirm(true);
				Track.Dependency("PushRefs", remoteUrl, timing.Elapsed, true);
			}
			catch (NoCredentialException e)
			{
				Log.Debug("Canceled enter credentials");
				credentialHandler.SetConfirm(false);
				Log.Exception(e, "");
				Track.Dependency("PushRefs", remoteUrl, timing.Elapsed, false);
			}
			catch (Exception e)
			{
				if (IsInvalidProtocol(e))
				{
					return;
				}

				Log.Exception(e, "");
				credentialHandler.SetConfirm(false);
				Track.Dependency("PushRefs", remoteUrl, timing.Elapsed, false);
				throw;
			}
		}


		private PushOptions GetPushOptions()
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


		private FetchOptions GetFetchOptions(FetchOptions fetchOptions)
		{
			fetchOptions.CredentialsProvider = (url, usernameFromUrl, types) =>
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

			return fetchOptions;
		}



		private static Remote Remote(IRepository repo)
		{
			return repo.Network.Remotes[Origin];
		}


		private static bool IsInvalidProtocol(Exception e)
		{
			if (-1 != e.Message.IndexOfOic("Unsupported URL protocol"))
			{
				Log.Debug("Invalid protocol");
				return true;
			}

			return false;
		}

		private static bool HasRemote(Repository repo)
		{
			try
			{
				return repo.Network.Remotes.Any(r => r.Name == Origin);
			}
			catch (Exception e)
			{
				Log.Debug($"No remotes {e.Message}");
				return false;
			}
		}


		public class NoCredentialException : Exception
		{
		}
	}
}