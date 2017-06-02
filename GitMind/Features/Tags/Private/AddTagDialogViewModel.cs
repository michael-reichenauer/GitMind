using System.Windows;
using GitMind.Utils.UI;


namespace GitMind.Features.Tags.Private
{
	internal class AddTagDialogViewModel : ViewModel
	{
		public Command<Window> OkCommand => Command<Window>(SetOK);
		public Command<Window> CancelCommand => Command<Window>(w => w.DialogResult = false);

		public string TagText
		{
			get => Get();
			set => Set(value).Notify(nameof(OkCommand));
		}



		private void SetOK(Window window)
		{
			if (string.IsNullOrEmpty(TagText))
			{
				return;
			}

			window.DialogResult = true;
		}
	}
}