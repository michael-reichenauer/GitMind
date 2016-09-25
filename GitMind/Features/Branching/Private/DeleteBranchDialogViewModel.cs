using System.Windows;
using GitMind.Utils.UI;


namespace GitMind.Features.Branching.Private
{
	internal class DeleteBranchDialogViewModel : ViewModel
	{
		public Command<Window> OkCommand => Command<Window>(SetOK);
		public Command<Window> CancelCommand => Command<Window>(w => w.DialogResult = false);


		public string BranchName
		{
			get { return Get(); }
			set { Set(value); }
		}

		public bool IsLocal
		{
			get { return Get(); }
			set { Set(value); }
		}

		public bool CanLocal
		{
			get { return Get(); }
			set { Set(value); }
		}

		public bool IsRemote
		{
			get { return Get(); }
			set { Set(value); }
		}

		public bool CanRemote
		{
			get { return Get(); }
			set { Set(value); }
		}

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