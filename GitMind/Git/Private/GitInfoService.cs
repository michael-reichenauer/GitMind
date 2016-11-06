using System;
using System.IO;
using System.Linq;
using GitMind.Utils;


namespace GitMind.Git.Private
{
	internal class GitInfoService : IGitInfoService
	{
		private readonly IRepoCaller repoCaller;


		public GitInfoService(IRepoCaller repoCaller)
		{
			this.repoCaller = repoCaller;
		}


		public R<string> GetCurrentRootPath(string folder)
		{
			try
			{
				// The specified folder, might be a sub folder of the root working folder,
				// lets try to find root folder by testing folder and then its parent folders
				// until a root folder is found or no root folder is found.
				string rootFolder = folder;
				while (!string.IsNullOrEmpty(rootFolder))
				{
					if (LibGit2Sharp.Repository.IsValid(rootFolder))
					{
						Log.Debug($"Root folder for {folder} is {rootFolder}");
						return rootFolder;
					}

					// Get the parent folder to test that
					rootFolder = Path.GetDirectoryName(rootFolder);
				}

				return R<string>.NoValue;
			}
			catch (Exception e)
			{
				return Error.From(e, $"Failed to get root working folder for {folder}, {e.Message}");
			}
		}


		public bool IsSupportedRemoteUrl(string workingFolder)
		{
			return repoCaller.UseRepo(repo =>
			{
				return !repo.Network.Remotes
					.Any(remote => remote.Url.StartsWith("ssh:", StringComparison.OrdinalIgnoreCase));
			})
			.Or(false);
		}
	}
}