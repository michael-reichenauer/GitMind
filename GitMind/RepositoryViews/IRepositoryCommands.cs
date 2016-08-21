using System.Threading.Tasks;
using System.Windows;


namespace GitMind.RepositoryViews
{
	internal interface IRepositoryCommands
	{
		string WorkingFolder { get; }
		Window Owner { get; }

		DisabledStatus DisableStatus();
		void AddSpecifiedBranch(string branchName);
		Task RefreshAfterCommandAsync(bool b);
	}
}