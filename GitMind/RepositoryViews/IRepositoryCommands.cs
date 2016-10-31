using System.Threading.Tasks;
using System.Windows;
using GitMind.Git;
using GitMind.GitModel;


namespace GitMind.RepositoryViews
{
	internal interface IRepositoryCommands
	{
		Window Owner { get; }
		Repository Repository { get;  }
		Commit UnCommited { get; }
		CredentialHandler GetCredentialsHandler();

		DisabledStatus DisableStatus();
		void ShowBranch(BranchName branchName);
		Task RefreshAfterCommandAsync(bool useFreshRepository);
		void SetCurrentMerging(Branch branch);
	}
}