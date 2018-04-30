using System;
using System.Windows;
using GitMind.Utils;



namespace GitMind.MainWindowViews
{
	[SingleInstance]
	internal class WindowOwner
	{
		private readonly Lazy<MainWindow> mainWindow;


		public WindowOwner(Lazy<MainWindow> mainWindow)
		{
			this.mainWindow = mainWindow;
		}

		public static implicit operator Window(WindowOwner owner) => owner.Window;


		public Window Window
		{
			get
			{
				if (Application.Current?.MainWindow is MainWindow)
				{
					return mainWindow.Value;
				}

				Log.Warn("Main Window is null");
				return null;
			}
		}


		public System.Windows.Forms.IWin32Window Win32Window
		{
			get
			{
				if (Window == null)
				{
					return null;
				}

				return AsWin32Window(Window);
			}
		}


		public static System.Windows.Forms.IWin32Window AsWin32Window(Window window)
		{
			var source = PresentationSource.FromVisual(window) as System.Windows.Interop.HwndSource;
			System.Windows.Forms.IWin32Window win = new OldWindow(source.Handle);
			return win;
		}


		private class OldWindow : System.Windows.Forms.IWin32Window
		{
			private readonly IntPtr _handle;
			public OldWindow(IntPtr handle)
			{
				_handle = handle;
			}

			IntPtr System.Windows.Forms.IWin32Window.Handle => _handle;
		}
	}
}