using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using GitMind.ApplicationHandling;
using GitMind.Utils;


namespace GitMind.GitModel.Private
{
	[SingleInstance]
	internal class LinkService : ILinkService
	{
		private static readonly string splitChars = @"[\,; :]";

		private readonly WorkingFolder workingFolder;

		private List<Pattern> issuePatterns;
		private List<Pattern> tagPatterns;
		private Regex issuesRgx;
		private Regex tagsRgx;


		public LinkService(WorkingFolder workingFolder)
		{
			this.workingFolder = workingFolder;

			OnChangedWorkingFolder();
			workingFolder.OnChange += (s, e) => OnChangedWorkingFolder();
		}


		public Links ParseIssues(string text)
		{
			return Parse(text, issuesRgx, issuePatterns);
		}


		public Links ParseTags(string text)
		{
			return Parse(text, tagsRgx, tagPatterns);
		}


		private void OnChangedWorkingFolder()
		{
			issuePatterns = new List<Pattern>();
			tagPatterns = new List<Pattern>();
			string filePath = Path.Combine(workingFolder, ".gitmind");
			string[] fileLines = File.ReadAllLines(filePath);
			foreach (string line in fileLines)
			{
				if (line.StartsWith("issue:", StringComparison.OrdinalIgnoreCase))
				{
					int patternIndex = line.IndexOf(" ", 8);
					string linkPattern = line.Substring(6, patternIndex - 6).Trim();
					string regexp = line.Substring(patternIndex).Trim();
					issuePatterns.Add(new Pattern(linkPattern, regexp, LinkType.issue));
				}
				else if(line.StartsWith("tag:", StringComparison.OrdinalIgnoreCase))
				{
					int patternIndex = line.IndexOf(" ", 6);
					string linkPattern = line.Substring(4, patternIndex - 4).Trim();
					string regexp = line.Substring(patternIndex).Trim();
					tagPatterns.Add(new Pattern(linkPattern, regexp, LinkType.tag));
				}
			}

			issuesRgx = GetPatternsRegExp(issuePatterns);
			tagsRgx = GetPatternsRegExp(tagPatterns);
		}


		private static Regex GetPatternsRegExp(IEnumerable<Pattern> patterns)
		{
			string issueTxt = "";
			int index = 0;
			foreach (Pattern pattern in patterns)
			{
				if (index > 0)
				{
					issueTxt += "|";
				}

				issueTxt += $"(?<n{index}>{splitChars}*{pattern.RegExp}{splitChars}*)";
				index++;
			}

			return new Regex(issueTxt);
		}



		private static Links Parse(string text, Regex rgx, List<Pattern> patterns)
		{
			string totalText = "";
			List<Link> links = new List<Link>();
			foreach (Match match in rgx.Matches(text ?? ""))
			{
				totalText += match.Value;

				for (int i = 0; i < patterns.Count; i++)
				{
					string g1 = match.Groups["n" + i].Value;
					if (!string.IsNullOrEmpty(g1))
					{
						var m1 = patterns[i].Rgx.Match(g1);
						string matchText = m1.Groups[0].Value;
						string value = m1.Groups[1].Value;
						string uri = string.Format(patterns[i].LinkPattern, value);

						Link item = new Link(matchText, uri, patterns[i].LinkType);
						links.Add(item);
						break;
					}
				}			
			}

			return new Links(links, totalText);
		}
	}
}