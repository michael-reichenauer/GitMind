using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;



namespace GitMind.Utils.UI
{
	public class VisibleWindow
	{
		public static bool IsVisible(System.Windows.Window window)
		{
			WindowInteropHelper nativeWindow = new WindowInteropHelper(window);

			int testCount = 4;
			int xStep = (int)(window.Width / (testCount + 1));
			int yStep = (int)(window.Height / (testCount + 1));

			for (int i = 1; i < testCount - 1; i++)
			{
				for (int j = 1; j < testCount - 1; j++)
				{
					int x = (int)(window.Left + (j * xStep));
					int y = (int)(window.Top + (i * yStep));

					System.Drawing.Point testPoint = new System.Drawing.Point(x, y);
					if (nativeWindow.Handle != WindowFromPoint(testPoint))
					{
						return false;
					}
				}
			}

			return true;
		}


		[DllImport("user32.dll")]
		public static extern IntPtr WindowFromPoint(System.Drawing.Point lpPoint);
	}
}