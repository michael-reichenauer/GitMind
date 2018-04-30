namespace GitMind.ApplicationHandling
{
	public interface IStartInstanceService
	{
		bool StartInstance(string workingFolder);
		bool OpenOrStartInstance(string workingFolder);
	}
}