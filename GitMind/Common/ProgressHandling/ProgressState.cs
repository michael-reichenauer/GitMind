using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using GitMind.Utils;


namespace GitMind.Common.ProgressHandling
{
	internal class ProgressState : IProgressState
	{
		private readonly Func<ProgressState, Task<object>> progressAction;
		private object result;
		private Exception exception;
		private Action<string> progressTextSetter;

		private ProgressState(Func<ProgressState, Task<object>> progressAction)
		{
			this.progressAction = progressAction;
		}


		public void SetText(string text)
		{
			Log.Debug($"Progress status: {text}");
			progressTextSetter(text);
		}


		async Task IProgressState.DoAsync(Action<string> textSetter)
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

		public static void ShowDialog(Window owner, Func<ProgressState, Task> progressAction)
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


		public static void ShowDialog(Window owner, string text, Func<ProgressState, Task> progressAction)
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


		public static T ShowDialog<T>(Window owner, Func<ProgressState, Task<T>> progressAction)
		{
			return ShowDialog(owner, null, async progress => await progressAction(progress));
		}


		public static T ShowDialog<T>(Window owner, string text, Func<ProgressState, Task<T>> progressAction)
		{
			return (T)ShowImpl(
				owner,
				text,
				async progress => await progressAction(progress));
		}


		public static object ShowImpl(
			Window owner, string text, Func<ProgressState, Task<object>> progressAction)
		{
			Log.Debug($"Progress status: {text}");
			ProgressState progressState = new ProgressState(progressAction);

			ProgressDialog progressDialog = new ProgressDialog(owner, text, progressState);
			progressDialog.ShowDialog();

			Log.Debug("Progress done");
			if (progressState.exception != null)
			{
				ExceptionDispatchInfo.Capture(progressState.exception).Throw();
			}

			return progressState.result;
		}
	}
}