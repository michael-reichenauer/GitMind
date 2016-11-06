using System.Threading.Tasks;
using GitMind.Utils;


namespace GitMind.Git
{
	internal interface INetworkService
	{
		Task<R> FetchAsync(string workingFolder);

		Task<R> FetchBranchAsync(string workingFolder, BranchName branchName);

		Task<R> PushCurrentBranchAsync(string workingFolder);

		Task<R> PushBranchAsync(string workingFolder, BranchName branchName);

		Task PushNotesAsync(string workingFolder, string rootId);

		Task<R> FetchAllNotesAsync(string workingFolder);
	}
}