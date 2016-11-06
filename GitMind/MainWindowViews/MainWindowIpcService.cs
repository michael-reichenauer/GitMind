using System;
using System.Windows;
using GitMind.ApplicationHandling;
using GitMind.Features.Committing;
using GitMind.Utils;


namespace GitMind.MainWindowViews
{
	internal class MainWindowIpcService : IpcService
	{
		private readonly CommitCommand commitCommand;
		private static readonly string InstanceId = "0000278d-5c40-4973-aad9-1c33196fd1a2";


		public MainWindowIpcService(CommitCommand commitCommand)
		{
			this.commitCommand = commitCommand;

		}


		public static string GetId(string workingFolder) =>
			InstanceId + Uri.EscapeDataString(workingFolder);


		public void Activate(string[] args)
		{
			CommandLine commandLine = new CommandLine();

			Application.Current.Dispatcher.InvokeAsync(() =>
			{
				Application.Current.MainWindow.WindowState = WindowState.Minimized;
				Application.Current.MainWindow.Activate();
				Application.Current.MainWindow.WindowState = WindowState.Normal;

				if (commandLine.IsCommitCommand(args))
				{
					Log.Usage("Activated and commit");
					if (commitCommand.CanExecute())
					{
						commitCommand.Execute();
					}
				}
				else
				{
					Log.Usage("Activated");
				}
			});
		}
	}
}