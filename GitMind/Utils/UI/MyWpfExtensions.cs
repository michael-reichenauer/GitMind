namespace System.Windows.Media
{
	public static class MyWpfExtensions
	{
		public static Forms.IWin32Window GetIWin32Window(this Visual visual)
		{
			var source = PresentationSource.FromVisual(visual) as Interop.HwndSource;
			Forms.IWin32Window win = new OldWindow(source.Handle);
			return win;
		}

		private class OldWindow : Forms.IWin32Window
		{
			private readonly IntPtr _handle;
			public OldWindow(IntPtr handle)
			{
				_handle = handle;
			}

			IntPtr Forms.IWin32Window.Handle => _handle;
		}
	}
}