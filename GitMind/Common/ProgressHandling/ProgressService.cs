using System;
using System.Threading.Tasks;
using GitMind.MainWindowViews;


namespace GitMind.Common.ProgressHandling
{
	internal class ProgressService : IProgressService
	{
		private readonly WindowOwner owner;


		public ProgressService(WindowOwner owner)
		{
			this.owner = owner;
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
	}
}