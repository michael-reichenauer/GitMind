using System;
using System.Diagnostics;
using System.Windows.Media;
using GitMind.GitModel;
using GitMind.Utils;
using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class LinkItem : ViewModel
	{
		private readonly CommitViewModel viewModel;
		private readonly LinkType linkType;


		public LinkItem(CommitViewModel viewModel, string text, string uri, LinkType linkType)
		{
			this.viewModel = viewModel;
			this.linkType = linkType;
			Text = text;
			Uri = uri;
		}

		public string Text { get; }
		public bool IsLink => !string.IsNullOrEmpty(Uri);
		public string Uri { get; }
		public string ToolTip => "Show " + Uri;
		public Brush TicketBrush => linkType == LinkType.issue ? viewModel.TicketBrush : viewModel.TagBrush;
		public Brush TicketBackgroundBrush => linkType == LinkType.issue ? viewModel.TicketBackgroundBrush : viewModel.TagBackgroundBrush;
		public Command GotoTicketCommand => Command(GotoTicket);
	


		private void GotoTicket()
		{
			try
			{
				Process process = new Process();
				process.StartInfo.FileName = Uri;
				process.Start();

			}
			catch (Exception ex) when (ex.IsNotFatal())
			{
				Log.Error($"Failed to open help link {ex}");
			}
		}


	
	}
}