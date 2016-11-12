namespace GitMind.Common.ProgressHandling
{
	internal interface IProgressService
	{
		Progress ShowDialog(string text = "");

		void SetText(string text);
	}
}