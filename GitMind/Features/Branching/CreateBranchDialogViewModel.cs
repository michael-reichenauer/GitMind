using System.Windows;
using GitMind.Utils.UI;


namespace GitMind.Features.Branching
{
	internal class CreateBranchDialogViewModel : ViewModel
	{
		public Command<Window> OkCommand => Command<Window>(SetOK);
		public Command<Window> CancelCommand => Command<Window>(w => w.DialogResult = false);


		public CreateBranchDialogViewModel()
		{
			IsPublish = true;
		}

		public string BranchName
		{
			get { return Get(); }
			set { Set(value).Notify(nameof(OkCommand)); }
		}


		public bool IsPublish
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