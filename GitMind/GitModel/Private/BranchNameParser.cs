namespace GitMind.GitModel.Private
{
	internal static class BranchNameParser
	{
		private static readonly string MergeBranchSubject = "Merge branch ";
		private static readonly string MergeCommitSubject = "Merge commit ";
		private static readonly string MergeBranchSubject2 = "Merge ";
		//private static readonly string OldMergedBranchSubject = "[MERGED] from ";
		//private static readonly string OldMergedBranchSubject2 = "MERGED from ";
		private static readonly string MergedBranchSubject = "Merged from ";
		private static readonly string MergedBranchSubject2 = "Merged ";
		private static readonly string MergeRemoteTrackingBranch = "Merge remote-tracking branch ";
		private static readonly string IntoText = " into ";
		private static readonly string OfText = " of ";
		private static readonly string Origin = "origin/";
		private static readonly string RemotesOrigin = "remotes/origin/";
		private static readonly string RefsRemotesOrigin = "refs/remotes/origin/";
		private static readonly char[] TrimChars = " './,".ToCharArray();

		public static readonly MergeBranchNames NoMerge = new MergeBranchNames(null, null);

		public static MergeBranchNames ParseBranchNamesFromSubject(string subject)
		{
			int sourceBranchNameStart = -1;
			int sourceBranchNameEnd = -1;

			if (subject.StartsWith(MergeCommitSubject))
			{
				// Ignoring "Merge commit " messages, since they specify commit id source and not branch 
			}
			else if (subject.StartsWith(MergeBranchSubject)
				&& subject.Length > MergeBranchSubject.Length + 1)
			{
				// Found a "Merge branch "
				sourceBranchNameStart = MergeBranchSubject.Length;
			}
			//else if (subject.StartsWith(OldMergedBranchSubject)
			//	&& subject.Length > OldMergedBranchSubject.Length + 1)
			//{
			//	// Found a "[MERGED] from "
			//	sourceBranchNameStart = OldMergedBranchSubject.Length;
			//}
			//else if (subject.StartsWith(OldMergedBranchSubject2)
			//	&& subject.Length > OldMergedBranchSubject2.Length + 1)
			//{
			//	// Found a "MERGED from "
			//	sourceBranchNameStart = OldMergedBranchSubject2.Length;
			//}
			else if (subject.StartsWith(MergedBranchSubject)
				&& subject.Length > MergedBranchSubject.Length + 1)
			{
				// Found a "Merged from "
				sourceBranchNameStart = MergedBranchSubject.Length;
			}
			else if (subject.StartsWith(MergedBranchSubject2)
				&& subject.Length > MergedBranchSubject2.Length + 1)
			{
				// Found a "Merged "
				sourceBranchNameStart = MergedBranchSubject2.Length;
			}
			else if (subject.StartsWith(MergeRemoteTrackingBranch)
				&& subject.Length > MergeRemoteTrackingBranch.Length + 1)
			{
				// Found a "Merge remote-tracking branch "
				sourceBranchNameStart = MergeRemoteTrackingBranch.Length;
			}
			else if (subject.StartsWith(MergeBranchSubject2)
				&& subject.Length > MergeBranchSubject2.Length + 1)
			{
				// Found a "Merge "
				sourceBranchNameStart = MergeBranchSubject2.Length;
			}


			if (sourceBranchNameStart > 0)
			{
				sourceBranchNameEnd = subject.IndexOf(' ', sourceBranchNameStart);

				if (sourceBranchNameEnd == -1)
				{
					sourceBranchNameEnd = subject.Length;
				}
			}


			string sourceBranchName = null;
			string targetBranchName = null;
			if (sourceBranchNameEnd > 0)
			{
				// There is a source branch name
				sourceBranchName = subject.Substring(sourceBranchNameStart, sourceBranchNameEnd - sourceBranchNameStart);

				// Lets try to get a targter branch in messages like:
				// "Merge <source-branch> into <target-branch>"
				if (subject.Length > sourceBranchNameEnd + IntoText.Length)
				{
					int intoIndex = subject.IndexOf(IntoText, sourceBranchNameEnd);
					int ofIndex = subject.IndexOf(OfText, sourceBranchNameEnd);
					if (ofIndex > -1 && !subject.StartsWith("Merge branch '"))
					{
						ofIndex = -1;
					}

					if (intoIndex > 0 && subject.Length > intoIndex + IntoText.Length)
					{
						// Found the "into" word after the source branch name
						targetBranchName = subject.Substring(intoIndex + IntoText.Length);
					}
					else if (intoIndex == -1 && ofIndex > 0 && subject.Length > ofIndex + OfText.Length)
					{
						// Found the "of" word after the source branch name
						targetBranchName = sourceBranchName;
					}
				}
			}

			if (sourceBranchName != null && sourceBranchName.StartsWith(RefsRemotesOrigin))
			{
				// Trim branch names that start with "refs/remotes/origin"
				sourceBranchName = sourceBranchName.Substring(RefsRemotesOrigin.Length);
			}
			else if (sourceBranchName != null && sourceBranchName.StartsWith(RemotesOrigin))
			{
				// Trim branch names that start with "remotes/origin"
				sourceBranchName = sourceBranchName.Substring(RemotesOrigin.Length);
			}
			else if (sourceBranchName != null && sourceBranchName.StartsWith(Origin))
			{
				// Trim branch names that start with "remotes/origin"
				sourceBranchName = sourceBranchName.Substring(Origin.Length);
			}

			// Trim "'" and " " from barnch names
			sourceBranchName = sourceBranchName?.Trim(TrimChars);
			targetBranchName = targetBranchName?.Trim(TrimChars);

			if (targetBranchName != null && targetBranchName.StartsWith("origin/"))
			{
				targetBranchName = targetBranchName.Substring(7);
			}
			if (sourceBranchName != null && sourceBranchName.StartsWith("origin/"))
			{
				sourceBranchName = sourceBranchName.Substring(7);
			}

			if (targetBranchName != null && targetBranchName.StartsWith("remotes/origin/"))
			{
				targetBranchName = targetBranchName.Substring(15);
			}

			if (sourceBranchName != null && sourceBranchName.StartsWith("remotes/origin/"))
			{
				sourceBranchName = sourceBranchName.Substring(15);
			}

			if (targetBranchName != null && targetBranchName.StartsWith("refs/remotes/origin/"))
			{
				targetBranchName = targetBranchName.Substring(20);
			}

			if (sourceBranchName != null && sourceBranchName.StartsWith("refs/remotes/origin/"))
			{
				sourceBranchName = sourceBranchName.Substring(20);
			}

			//if (sourceBranchName != null
			//	&& (sourceBranchName.EndsWith("trunk") || sourceBranchName.EndsWith("Trunk")))
			//{
			//	sourceBranchName = "master";
			//}

			//if (targetBranchName != null 
			//	&& (targetBranchName.EndsWith("trunk") || targetBranchName.EndsWith("Trunk")))
			//{
			//	targetBranchName = "master";
			//}

			if (sourceBranchName == "Master")
			{
				sourceBranchName = "master";
			}

			if (targetBranchName == "Master")
			{
				targetBranchName = "master";
			}


			if (string.IsNullOrWhiteSpace(sourceBranchName))
			{
				sourceBranchName = null;
			}

			if (string.IsNullOrWhiteSpace(targetBranchName))
			{
				targetBranchName = null;
			}

			return new MergeBranchNames(sourceBranchName, targetBranchName);
		}
	}
}