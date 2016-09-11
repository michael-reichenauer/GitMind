using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GitMind.Utils.UI;


namespace GitMind.Features.Branching
{
	/// <summary>
	/// Interaction logic for DeleteBranchDialog.xaml
	/// </summary>
	public partial class DeleteBranchDialog : Window
	{
		private readonly DeleteBranchDialogViewModel viewModel;


		public DeleteBranchDialog(
			Window owner, 
			string branchName, 
			bool isLocal, 
			bool isRemote)
		{
			Owner = owner;
			InitializeComponent();

			viewModel = new DeleteBranchDialogViewModel();
			DataContext = viewModel;

			viewModel.BranchName = branchName;
			viewModel.IsLocal = isLocal;
			viewModel.CanLocal = isLocal && isRemote;
			viewModel.IsRemote = isRemote;
			viewModel.CanRemote = isRemote && isLocal;
		}


		public string BranchName => viewModel.BranchName;
		public bool IsLocal => viewModel.IsLocal;
		public bool IsRemote => viewModel.IsRemote;
	}
}
