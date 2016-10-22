using System;
using System.Windows;
using GitMind.Settings;
using GitMind.Utils;


namespace GitMind.MainWindowViews
{
	internal class MainWindowRemoteService : RemoteService
	{
		public void Activate()
		{
			Application.Current.Dispatcher.InvokeAsync(() =>
			{
				Application.Current.MainWindow.Activate();
				Application.Current.MainWindow.WindowState = WindowState.Normal;
			});
		}


		public static string GetId(string workingFolder) => 
			ProgramPaths.ProductGuid + Uri.EscapeDataString(workingFolder);
	}
}