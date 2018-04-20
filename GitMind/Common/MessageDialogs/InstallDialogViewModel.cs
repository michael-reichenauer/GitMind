
using System;
using System.Threading.Tasks;
using System.Windows;
using GitMind.Common.ProgressHandling;
using GitMind.Utils.UI;


namespace GitMind.Common.MessageDialogs
{
	internal class InstallDialogViewModel : ViewModel
	{
		//private readonly IProgressService progressService;
		//private readonly Window owner;
		private readonly Func<Task> installActionAsync;
		public Command<Window> OkCommand => Command<Window>(SetOK);
		public Command<Window> CancelCommand => Command<Window>(w => w.DialogResult = false);

		public InstallDialogViewModel(
			//IProgressService progressService,
			//Window owner,
			Func<Task> installActionAsync)
		{
			//this.progressService = progressService;
			//this.owner = owner;
			this.installActionAsync = installActionAsync;
			IsButtonsVisible = true;
		}

		public string Title { get => Get(); set => Set(value); }

		public string Message { get => Get(); set => Set(value); }

		public string CancelText { get => Get(); set => Set(value); }

		public bool IsButtonsVisible { get => Get(); set => Set(value); }


		private async void SetOK(Window window)
		{
			await installActionAsync();
			
			window.DialogResult = true;
		}
	}
}