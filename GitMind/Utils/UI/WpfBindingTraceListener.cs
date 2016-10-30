using System.Diagnostics;


namespace GitMind.Utils.UI
{
	/// <summary>
	/// Support warning logging of WPF binding errors
	/// </summary>
	internal class WpfBindingTraceListener : TraceListener
	{
		public override void Write(string message)
		{
		}

		public override void WriteLine(string message)
		{
			Log.Warn($"WPF binding error:\n{message}");
		}


		public static void Register()
		{
			PresentationTraceSources.Refresh();
			PresentationTraceSources.DataBindingSource.Listeners.Add(new WpfBindingTraceListener());
			PresentationTraceSources.DataBindingSource.Switch.Level =
				SourceLevels.Warning | SourceLevels.Error;
		}
	}
}