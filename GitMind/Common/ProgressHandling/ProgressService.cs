using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using GitMind.MainWindowViews;
using GitMind.Utils;


namespace GitMind.Common.ProgressHandling
{
	internal class ProgressService : IProgressService
	{
		private readonly WindowOwner windowsOwner;
		private readonly Lazy<MainWindowViewModel> mainWindowViewModel;
		private Progress currentProgress = null;

		public ProgressService(
			WindowOwner owner,
			Lazy<MainWindowViewModel> mainWindowViewModel)
		{
			this.windowsOwner = owner;
			this.mainWindowViewModel = mainWindowViewModel;
		}

		public IDisposable ShowBusy()
		{
			return mainWindowViewModel.Value.Busy.Progress();
		}

		public void SetText(string text)
		{
			currentProgress?.SetText(text);
		}


		public Progress ShowDialog(string text = "", Window owner = null)
		{
			Log.Debug($"Progress status: {text}");
			owner = owner ?? windowsOwner;
			ProgressBox progress = new ProgressBox(owner, text);

			progress.StartShowDialog();
			currentProgress = progress;
			return progress;
		}


		internal class ProgressBox : Progress
		{
			private readonly ProgressDialog progressDialog;
			private readonly TaskCompletionSource<bool> closeTask = new TaskCompletionSource<bool>();
			private Timing timing;

			public ProgressBox(Window owner, string text)
			{
				timing = new Timing();
				timing.Log($"Progress status: {text}");
				progressDialog = new ProgressDialog(owner, text, closeTask.Task);
			}
	

			public override void Dispose()
			{
				closeTask.TrySetResult(true);
				progressDialog.DialogResult = true;
			}


			public override void SetText(string text)
			{
				timing.Log($"Progress status: {text}");
				progressDialog.SetText(text);
			}


			public void StartShowDialog()
			{
				SynchronizationContext.Current.Post(_ => progressDialog.ShowDialog(), null);
			}
		}
	}
}