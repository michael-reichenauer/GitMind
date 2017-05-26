using System.Collections.Generic;


namespace GitMind.GitModel
{
	internal enum LinkType
	{
		tag,
		issue
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
		Links ParseIssues(string text);
		Links ParseTags(string text);
	}
}