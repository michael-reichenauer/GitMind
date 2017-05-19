using System.Collections.Generic;
using System.Text.RegularExpressions;


namespace GitMind.GitModel.Private
{
	internal class LinkService : ILinkService
	{
		//private static string splitChars = @"[\,; ]";


		private static Regex rgx = new Regex(@"(?<n1>[\,; ]*#(?<ticketnbr>\d\d*)[\,; ]*)|(?<n2>[\,; ]*#CST(?<cstnbr>\d\d*)[\,; ]*)");
		private static Regex rgx1 = new Regex(@"#(\d\d*)");
		private static Regex rgx2 = new Regex(@"#CST(\d\d*)");



		public Links Parse(string text)
		{
			string totalText = "";
			List<Link> links = new List<Link>();
			foreach (Match match in rgx.Matches(text))
			{
				totalText += match.Value;
				string g1 = match.Groups["n1"].Value;
				string g2 = match.Groups["n2"].Value;

				string t1 = null;
				string v1 = null;
				string uriTemplate = null;
				LinkType linkType = LinkType.issue;

				if (!string.IsNullOrEmpty(g1))
				{
					var m1 = rgx1.Match(g1);
					t1 = m1.Groups[0].Value;
					v1 = m1.Groups[1].Value;
					uriTemplate = $"https://trouble.se.axis.com/ticket/{v1}";
					linkType = LinkType.issue;
				}

				if (!string.IsNullOrEmpty(g2))
				{
					var m2 = rgx2.Match(g2);
					t1 = m2.Groups[0].Value;
					v1 = m2.Groups[1].Value;
					uriTemplate = $"https://cst.axis.com/case.cgi?id={v1}";
					linkType = LinkType.issue;
				}

				if (t1 != null && v1 != null && uriTemplate != null)
				{
					Link item = new Link(t1, uriTemplate, linkType);
					links.Add(item);
				}
			}

			return new Links(links, totalText);
		}
	}
}