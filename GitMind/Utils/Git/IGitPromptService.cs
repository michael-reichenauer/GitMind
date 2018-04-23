namespace GitMind.Utils.Git
{
	internal interface IGitPromptService
	{
		bool TryPromptText(string promptText, out string response);

		bool TryPromptYesNo(string promptText);
	}
}