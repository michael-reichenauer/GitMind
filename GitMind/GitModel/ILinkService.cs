using System.Collections.Generic;


namespace GitMind.GitModel
{
	internal enum LinkType
	{
		tag,
		issue
	}

	internal class Link
	{
		public Link(string text, string uri, LinkType linkType)
		{
			Text = text;
			Uri = uri;
		}


		public string Text { get; }

		public string Uri { get; }
	}

	internal class Links
	{
		public Links(IReadOnlyList<Link> links, string totalText)
		{
			AllLinks = links;
			TotalText = totalText;
		}


		public IReadOnlyList<Link> AllLinks { get; }

		public string TotalText { get; }
	}


	internal interface ILinkService
	{
		Links Parse(string text);
	}
}