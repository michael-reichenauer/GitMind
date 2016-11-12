﻿using System;
using System.Threading;
using System.Threading.Tasks;
using GitMind.MainWindowViews;
using GitMind.Utils;


namespace GitMind.Common.ProgressHandling
{
	internal class ProgressService : IProgressService
	{
		private readonly WindowOwner owner;
		private Progress currentProgress = null;

		public ProgressService(WindowOwner owner)
		{
			this.owner = owner;
		}


		public void SetText(string text)
		{
			currentProgress?.SetText(text);
		}


		public Progress ShowDialog(string text = "")
		{
			Log.Debug($"Progress status: {text}");

			ProgressImpl progress = new ProgressImpl(owner, text);

			progress.StartShowDialog();
			currentProgress = progress;
			return progress;
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