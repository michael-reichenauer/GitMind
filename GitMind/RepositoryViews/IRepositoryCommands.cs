using System.Threading.Tasks;
using System.Windows;
using GitMind.GitModel;


namespace GitMind.RepositoryViews
{
	internal interface IRepositoryCommands
	{
		string WorkingFolder { get; }
		Window Owner { get; }
		Repository Repository { get;  }
		Commit UnCommited { get; }
		CredentialHandler GetCredentialsHandler();

		DisabledStatus DisableStatus();
		void ShowBranch(string branchName);
		Task RefreshAfterCommandAsync(bool useFreshRepository);
		void SetCurrentMerging(Branch branch);
	}
}