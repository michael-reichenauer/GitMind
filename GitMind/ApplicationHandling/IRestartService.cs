namespace GitMind.ApplicationHandling
{
	public interface IRestartService
	{
		bool TriggerRestart(string workingFolder);
	}
}