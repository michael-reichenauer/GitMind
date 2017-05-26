using System.Text.RegularExpressions;


namespace GitMind.GitModel.Private
{
	internal class Pattern
	{
		public Pattern(string linkPattern, string regExp, LinkType linkType)
		{
			LinkPattern = linkPattern;
			RegExp = regExp;
			Rgx = new Regex(regExp);
			LinkType = linkType;
		}


		public string LinkPattern { get; }

		public string RegExp { get; }

		public Regex Rgx { get; }

		public LinkType LinkType { get; }
	}
}