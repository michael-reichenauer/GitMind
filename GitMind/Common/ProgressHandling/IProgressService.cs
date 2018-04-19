using System;
using System.Windows;


namespace GitMind.Common.ProgressHandling
{
	public interface IProgressService
	{
		Progress ShowDialog(string text = "", Window owner = null);

		void SetText(string text);
		IDisposable ShowBusy();
	}
}