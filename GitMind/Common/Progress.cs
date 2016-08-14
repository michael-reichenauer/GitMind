using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using GitMind.Utils;


namespace GitMind.Common
{
	public class Progress : IProgressWorker
	{
		private readonly Func<Progress, Task<object>> progressAction;
		private object result;
		private Exception exception;
		private Action<string> progressTextSetter;

		private Progress(Func<Progress, Task<object>> progressAction)
		{
			this.progressAction = progressAction;
		}


		public void SetText(string text)
		{
			Log.Debug($"Progress status: {text}");
			progressTextSetter(text);
		}


		async Task IProgressWorker.DoAsync(Action<string> textSetter)
		{
			progressTextSetter = textSetter;
			try
			{
				result = await progressAction(this);
			}
			catch (Exception e) when (e.IsNotFatal())
			{
				Log.Warn($"Exception {e}");
				exception = e;
			}			
		}


		public static void ShowDialog(Window owner, string text, Func<Task> progressAction)
		{
			ShowImpl(
				owner,
				text,
				async _ =>
				{
					await progressAction();
					return null;
				});
		}

		public static void ShowDialog(Window owner, Func<Progress, Task> progressAction)
		{
			ShowImpl(
				owner,
				null,
				async progress =>
				{
					await progressAction(progress);
					return null;
				});
		}


		public static void ShowDialog(Window owner, string text, Func<Progress, Task> progressAction)
		{
			ShowImpl(
				owner,
				text,
				async progress =>
				{
					await progressAction(progress);
					return null;
				});
		}


		public static T ShowDialog<T>(Window owner, string text, Func<Task<T>> progressAction)
		{
			return ShowDialog(owner, text, async _ => await progressAction());
		}


		public static T ShowDialog<T>(Window owner, Func<Progress, Task<T>> progressAction)
		{
			return ShowDialog(owner, null, async progress => await progressAction(progress));
		}


		public static T ShowDialog<T>(Window owner, string text, Func<Progress, Task<T>> progressAction)
		{
			return (T)ShowImpl(
				owner,
				text,
				async progress => await progressAction(progress));
		}


		private static object ShowImpl(
			Window owner, string text, Func<Progress, Task<object>> progressAction)
		{
			Log.Debug($"Progress status: {text}");
			Progress progress = new Progress(progressAction);

			ProgressDialog progressDialog = new ProgressDialog(owner, text, progress);
			progressDialog.ShowDialog();

			Log.Debug("Progress done");
			if (progress.exception != null)
			{
				ExceptionDispatchInfo.Capture(progress.exception).Throw();
			}

			return progress.result;
		}
	}
}