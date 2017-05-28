using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GitMind.ApplicationHandling;
using GitMind.Utils;
using LibGit2Sharp;


namespace GitMind.Git.Private
{
	internal class GitNetworkService : IGitNetworkService
	{
		private static readonly string Origin = "origin";
	
		private static readonly TimeSpan FetchTimeout = TimeSpan.FromSeconds(30);
		private static readonly TimeSpan PushTimeout = TimeSpan.FromSeconds(30);

		private readonly WorkingFolder workingFolder;
		private readonly IRepoCaller repoCaller;
		private readonly ICredentialHandler credentialHandler;


		public GitNetworkService(
			WorkingFolder workingFolder,
			IRepoCaller repoCaller,
			ICredentialHandler credentialHandler)
		{
			this.workingFolder = workingFolder;
			this.repoCaller = repoCaller;
			this.credentialHandler = credentialHandler;
		}


		public Task<R> FetchAsync()
		{
			FetchOptions fetchOptions = GetFetchOptions(
				new FetchOptions { Prune = true, TagFetchMode = TagFetchMode.All });

			Log.Debug("Fetch all ...");
			return repoCaller.UseRepoAsync(FetchTimeout, repo =>
			{
				try
				{
					if (!repo.Network.Remotes.Any(r => r.Name == Origin))
					{
						Log.Debug("No 'origin' remote, skipping fetch");
						return;
					};

					repo.Fetch(Origin, fetchOptions);
					credentialHandler.SetConfirm(true);
				}
				catch (NoCredentialException)
				{
					Log.Debug("Canceled enter credentials");
					credentialHandler.SetConfirm(false);
				}
				catch (Exception e)
				{
					if (IsInvalidProtocol(e))
					{
						return;
					}
					Log.Error($"Error {e}");
					credentialHandler.SetConfirm(false);
					throw;
				}
			});
		}


		public Task<R> FetchBranchAsync(BranchName branchName)
		{
			Log.Debug($"Fetch branch {branchName}...");

			string[] refspecs = { $"{branchName}:{branchName}" };

			return FetchRefsAsync(refspecs);
		}


		public Task<R> FetchRefsAsync(IEnumerable<string> refspecs)
		{
			FetchOptions fetchOptions = GetFetchOptions(new FetchOptions());
			string refsText = string.Join(",", refspecs);
			Log.Debug($"Fetch refs {refsText} ...");

			return repoCaller.UseRepoAsync(repo =>
			{
				try
				{
					if (!repo.Network.Remotes.Any(r => r.Name == Origin))
					{
						Log.Debug("No 'origin' remote, skipping fetch");
						return;
					};

					Remote remote = Remote(repo);		
					repo.Network.Fetch(remote, refspecs, fetchOptions);
					}
					catch (NoCredentialException)
					{
						Log.Debug("Canceled enter credentials");
						credentialHandler.SetConfirm(false);
					}
					catch (Exception e)
					{
						if (IsInvalidProtocol(e))
						{
							return;
						}

						Log.Error($"Error {e}");
						credentialHandler.SetConfirm(false);
						throw;
					}
			});
		}


		public Task<R> PushBranchAsync(BranchName branchName)
		{
			Log.Debug($"Push branch {branchName} ...");

			string[] refspecs = { $"refs/heads/{branchName}:refs/heads/{branchName}"};
			return PushRefsAsync(refspecs);
		}


		public Task<R> PushCurrentBranchAsync()
		{
			Log.Debug("Push current branch ...");

			return repoCaller.UseRepoAsync(PushTimeout, repo =>
				{
					Branch currentBranch = repo.Head;
					string[] refspecs = {$"{currentBranch.CanonicalName}:{currentBranch.CanonicalName}"};
					PushRefs(refspecs, repo);
				});
		}


		public Task<R> PushRefsAsync(IEnumerable<string> refspecs)
		{
			string refsText = string.Join(",", refspecs);
			Log.Debug($"Push refs {refsText} ...");

			return repoCaller.UseRepoAsync(PushTimeout, repo =>
			{
				PushRefs(refspecs, repo);
			});
		}


		public Task<R> PublishBranchAsync(BranchName branchName)
		{
			Log.Debug($"Publish branch {branchName} ...");

			return repoCaller.UseLibRepoAsync(repo =>
			{
				try
				{
					Branch localBranch = repo.Branches.FirstOrDefault(b => branchName.IsEqual(b.FriendlyName));
					if (localBranch == null)
					{
						throw new Exception($"No local branch with name {branchName}");
					}

					PushOptions pushOptions = GetPushOptions();

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
						if (repo.Network.Remotes.Any(r => r.Name == Origin))
						{
							Remote remote = Remote(repo);

							repo.Branches.Update(
								localBranch,
								b => b.Remote = remote.Name,
								b => b.UpstreamBranch = localBranch.CanonicalName);
						}
					}

					repo.Network.Push(localBranch, pushOptions);
				}
				catch (Exception e)
				{
					if (IsInvalidProtocol(e))
					{
						return;
					}

					Log.Error($"Error {e}");
					throw;
				}		
			});
		}


		public Task<R> DeleteRemoteBranchAsync(BranchName branchName)
		{
			Log.Debug($"Delete remote branch {branchName} ...");

			return repoCaller.UseRepoAsync(PushTimeout, repo =>
			{
				try
				{
					if (!repo.Network.Remotes.Any(r => r.Name == Origin))
					{
						Log.Debug("No 'origin' remote, skipping delete remote branch");
						return;
					};

					repo.Branches.Remove(branchName, true);

					PushOptions pushOptions = GetPushOptions();

					Remote remote = Remote(repo);

					// Using a refspec, like you would use with git push...
					repo.Network.Push(remote, $":refs/heads/{branchName}", pushOptions);

					credentialHandler.SetConfirm(true);
				}
				catch (Exception e)
				{
					if (IsInvalidProtocol(e))
					{
						return;
					}

					Log.Error($"Error {e}");
					credentialHandler.SetConfirm(false);
					throw;
				}
			});
		}


		private void PushRefs(IEnumerable<string> refspecs, Repository repo)
		{
			try
			{
				if (!repo.Network.Remotes.Any(r => r.Name == Origin))
				{
					Log.Debug("No 'origin' remote, skipping delete remote branch");
					return;
				};

				PushOptions pushOptions = GetPushOptions();

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
				if (IsInvalidProtocol(e))
				{
					return;
				}

				Log.Error($"Error {e}");
				credentialHandler.SetConfirm(false);
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


		public class NoCredentialException : Exception
		{
		}
	}
}