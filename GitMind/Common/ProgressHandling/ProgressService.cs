using System;
using System.Threading;
using System.Threading.Tasks;
using GitMind.MainWindowViews;
using GitMind.Utils;


namespace GitMind.Common.ProgressHandling
{
	internal class ProgressService : IProgressService
	{
		private readonly WindowOwner owner;
		private Progress currentState = null;

		public ProgressService(WindowOwner owner)
		{
			this.owner = owner;
		}


		public void SetText(string text)
		{
			currentState?.SetText(text);
		}


		public Progress ShowDialog(string text = "")
		{
			Log.Debug($"Progress status: {text}");

			ProgressImpl state = new ProgressImpl(owner, text);

			state.StartShowDialog();
			currentState = state;
			return state;
		}


		public void Show(string text, Func<Task> progressAction)
		{
			ShowImpl(
				text,
				async _ =>
				{
					await progressAction();
					return null;
				});
		}

		public void Show(Func<ProgressState, Task> progressAction)
		{
			ShowImpl(
				null,
				async progress =>
				{
					await progressAction(progress);
					return null;
				});
		}


		public void Show(string text, Func<ProgressState, Task> progressAction)
		{
			ShowImpl(
				text,
				async progress =>
				{
					await progressAction(progress);
					return null;
				});


		}


		public T Show<T>(string text, Func<Task<T>> progressAction)
		{
			return Show(text, async _ => await progressAction());
		}


		public T Show<T>(Func<ProgressState, Task<T>> progressAction)
		{
			return Show(null, async progress => await progressAction(progress));
		}


		public T Show<T>(string text, Func<ProgressState, Task<T>> progressAction)
		{
			return (T)ShowImpl(text, async progress => await progressAction(progress));
		}


		private object ShowImpl(string text, Func<ProgressState, Task<object>> progressAction)
		{
			return ProgressState.ShowImpl(owner, text, progressAction);
		}


		internal class ProgressImpl : Progress
		{
			private readonly ProgressDialog progressDialog;
			private readonly TaskCompletionSource<bool> closeTask = new TaskCompletionSource<bool>();	

			public ProgressImpl(WindowOwner owner, string text)
			{
				progressDialog = new ProgressDialog(owner, text, closeTask.Task);
			}
	

			public override void Dispose()
			{
				closeTask.TrySetResult(true);
			}


			public override void SetText(string text)
			{
				Log.Debug($"Progress status: {text}");
				progressDialog.SetText(text);
			}


			public void StartShowDialog()
			{
				SynchronizationContext.Current.Post(_ => progressDialog.ShowDialog(), null);
			}
		}
	}
}