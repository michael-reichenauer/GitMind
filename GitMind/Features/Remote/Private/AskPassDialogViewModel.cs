using System.Windows;
using GitMind.Utils.UI;


namespace GitMind.Features.Remote.Private
{
	internal class AskPassDialogViewModel : ViewModel
	{
		public Command<Window> OkCommand => Command<Window>(SetOK);
		public Command<Window> CancelCommand => Command<Window>(w => w.DialogResult = false);

		public string PromptText { get => Get(); set => Set(value); }


		private void SetOK(Window window)
		{
			window.DialogResult = true;
		}
	}
}