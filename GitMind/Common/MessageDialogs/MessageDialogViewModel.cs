
using System.Windows;
using GitMind.Utils.UI;


namespace GitMind.Common.MessageDialogs
{
	internal class MessageDialogViewModel : ViewModel
	{
		public Command<Window> OkCommand => Command<Window>(SetOK);
		public Command<Window> CancelCommand => Command<Window>(w => w.DialogResult = false);

		public MessageDialogViewModel()
		{
		}

		public string Title
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Message
		{
			get { return Get(); }
			set { Set(value); }
		}
	

		public bool IsInfo
		{
			get { return Get(); }
			set { Set(value); }
		}

		public bool IsQuestion
		{
			get { return Get(); }
			set { Set(value); }
		}	

		public bool IsWarn
		{
			get { return Get(); }
			set { Set(value); }
		}

		public bool IsError
		{
			get { return Get(); }
			set { Set(value); }
		}

	

		public string OkText
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string CancelText
		{
			get { return Get(); }
			set { Set(value); }
		}

		public bool IsCancelVisible
		{
			get { return Get(); }
			set { Set(value); }
		}

		private void SetOK(Window window)
		{
			window.DialogResult = true;
		}
	}
}