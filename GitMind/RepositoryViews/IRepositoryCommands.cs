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

		DisabledStatus DisableStatus();
		void AddSpecifiedBranch(string branchName);
		Task RefreshAfterCommandAsync(bool b);
	}
}