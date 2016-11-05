using System;
using System.Windows;
using GitMind.ApplicationHandling;
using GitMind.Utils;


namespace GitMind.MainWindowViews
{
	internal class MainWindowIpcService : IpcService
	{
		private static readonly string InstanceId = "0000278d-5c40-4973-aad9-1c33196fd1a2";

		private readonly MainWindowViewModel mainWindowViewModel;


		public MainWindowIpcService(MainWindowViewModel mainWindowViewModel)
		{
			this.mainWindowViewModel = mainWindowViewModel;
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
					if (mainWindowViewModel.RepositoryViewModel.CommitCommand.CanExecute())
					{
						mainWindowViewModel.RepositoryViewModel.CommitCommand.Execute();
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