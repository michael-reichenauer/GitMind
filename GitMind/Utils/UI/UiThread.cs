using System;
using System.Windows;
using System.Windows.Threading;


namespace GitMind.Utils.UI
{
	public static class UiThread
	{
		public static void Run(Action uiAction)
		{
			Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

			if (dispatcher.CheckAccess())
			{
				uiAction();
			}
			else
			{
				dispatcher.Invoke(uiAction);
			}
		}
	}
}
