using System;


namespace GitMind.Utils
{
	public class AsserterEventArgs : EventArgs
	{
		public Exception Exception { get; }

		public AsserterEventArgs(Exception exception) => Exception = exception;
	}
}