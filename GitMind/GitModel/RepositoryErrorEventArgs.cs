using System;


namespace GitMind.GitModel
{
	internal class RepositoryErrorEventArgs : EventArgs
	{
		public string ErrorText { get; }


		public RepositoryErrorEventArgs(string errorText)
		{
			ErrorText = errorText;
		}
	}
}