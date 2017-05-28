namespace GitMind.GitModel
{
	internal class Link
	{
		public Link(string text, string uri, LinkType linkType)
		{
			Text = text;
			Uri = uri;
			LinkType = linkType;
		}


		public string Text { get; }

		public string Uri { get; }
		public LinkType LinkType { get; }

		public override string ToString() => $"{Text} -> {Uri}";

	}
}