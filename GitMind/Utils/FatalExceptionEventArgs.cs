namespace System
{
	internal class FatalExceptionEventArgs : EventArgs
	{
		public string Message { get; }

		public Exception Exception { get; }


		public FatalExceptionEventArgs(string message, Exception exception)
		{
			Message = message;
			Exception = exception;
		}
	}
}