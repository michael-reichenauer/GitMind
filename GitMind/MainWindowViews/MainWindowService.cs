﻿using System;


namespace GitMind.MainWindowViews
{
	internal class MainWindowService : IMainWindowService
	{
		private readonly Lazy<MainWindow> mainWindow;


		public MainWindowService(Lazy<MainWindow> mainWindow)
		{
			this.mainWindow = mainWindow;
		}


		public bool IsNewVersionAvailable
		{
			set { mainWindow.Value.IsNewVersionAvailable = value; }
		}


		public void SetSearchFocus()
		{
			mainWindow.Value.SetSearchFocus();
		}


		public void SetRepositoryViewFocus()
		{
			mainWindow.Value.SetRepositoryViewFocus();
		}


		public void SetMainWindowFocus()
		{
			mainWindow.Value.Focus();
		}
	}
}