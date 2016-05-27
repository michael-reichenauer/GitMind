using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


namespace GitMind.CommitsHistory
{
	internal interface IOldHistoryViewModel
	{
		ICommand ShowBranchCommand { get; }

		ICommand HideBranchCommand { get; }

		ICommand ToggleDetailsCommand { get; }

		ObservableCollection<BranchName> AllBranches { get; }

		Task LoadAsync(Window window);
		Task RefreshAsync(bool isShift);

		void SetBranches(IReadOnlyList<string> activeBranches);

		IReadOnlyList<string> GetAllBranchNames();
		Task HideBranchNameAsync(string branchName);
		void SetFilter(string text);
	}
}