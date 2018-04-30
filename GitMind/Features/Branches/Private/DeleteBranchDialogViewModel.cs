using System.Windows;
using GitMind.Utils.UI;


namespace GitMind.Features.Branches.Private
{
	internal class DeleteBranchDialogViewModel : ViewModel
	{
		public Command<Window> OkCommand => Command<Window>(SetOK);
		public Command<Window> CancelCommand => Command<Window>(w => w.DialogResult = false);


		public string BranchName { get => Get(); set => Set(value); }

		public bool IsLocal { get => Get(); set => Set(value); }

		public bool CanLocal { get => Get(); set => Set(value); }

		public bool IsRemote { get => Get(); set => Set(value); }

		public bool CanRemote { get => Get(); set => Set(value); }

		public bool IsForce { get => Get(); set => Set(value); }

		private void SetOK(Window window)
		{
			if (string.IsNullOrEmpty(BranchName))
			{
				return;
			}

			window.DialogResult = true;
		}
	}
}