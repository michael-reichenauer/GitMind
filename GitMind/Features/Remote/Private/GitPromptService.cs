using System.Windows;
using GitMind.Utils;
using GitMind.Utils.Git;


namespace GitMind.Features.Remote.Private
{
	internal class GitPromptService : IGitPromptService
	{
		public bool TryPromptText(string promptText, out string response)
		{
			AskPassDialog dialog = new AskPassDialog(null);
			Application.Current.MainWindow = dialog;

			dialog.Prompt = promptText;
			if (dialog.ShowDialog() == true)
			{
				Log.Debug($"Git response Pass:'{dialog.ResponseText.Password}'");
				response = dialog.ResponseText.Password;
				return true;
			}


			response = null;
			return false;
		}


		public bool TryPromptYesNo(string promptText)
		{
			throw new System.NotImplementedException();
		}
	}
}