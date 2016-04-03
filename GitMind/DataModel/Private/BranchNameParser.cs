namespace GitMind.DataModel.Private
{
	internal static class BranchNameParser
	{
		private static readonly string MergeBranchSubject = "Merge branch ";
		private static readonly string MergeBranchSubject2 = "Merge ";
		private static readonly string OldMergedBranchSubject = "[MERGED] from ";
		private static readonly string OldMergedBranchSubject2 = "MERGED from ";
		private static readonly string MergedBranchSubject = "Merged from ";
		private static readonly string MergedBranchSubject2 = "Merged ";
		private static readonly string MergeRemoteTrackingBranch = "Merge remote-tracking branch ";
		private static readonly string IntoText = " into ";
		private static readonly string OfText = " of ";
		private static readonly string Origin = "origin/";
		private static readonly string RemotesOrigin = "remotes/origin/";
		private static readonly string RefsRemotesOrigin = "refs/remotes/origin/";
		private static readonly char[] TrimChars = " './,".ToCharArray();

		private static readonly MergeBranchNames NoMerge = new MergeBranchNames(null, null);

		public static MergeBranchNames ParseBranchNamesFromSubject(Commit commit)
		{
			if (commit.SecondParent == Commit.None)
			{
				// This is no merge commit, i.e. no branch names to parse
				return NoMerge;
			}

			int sourceBranchNameStart = -1;
			int sourceBranchNameEnd = -1;
			if (commit.Subject.StartsWith(MergeBranchSubject)
				&& commit.Subject.Length > MergeBranchSubject.Length + 1)
			{
				// Found a "Merge branch "
				sourceBranchNameStart = MergeBranchSubject.Length;
			}
			else if (commit.Subject.StartsWith(OldMergedBranchSubject)
				&& commit.Subject.Length > OldMergedBranchSubject.Length + 1)
			{
				// Found a "[MERGED] from "
				sourceBranchNameStart = OldMergedBranchSubject.Length;
			}
			else if (commit.Subject.StartsWith(OldMergedBranchSubject2)
				&& commit.Subject.Length > OldMergedBranchSubject2.Length + 1)
			{
				// Found a "MERGED from "
				sourceBranchNameStart = OldMergedBranchSubject2.Length;
			}
			else if (commit.Subject.StartsWith(MergedBranchSubject)
				&& commit.Subject.Length > MergedBranchSubject.Length + 1)
			{
				// Found a "Merged from "
				sourceBranchNameStart = MergedBranchSubject.Length;
			}
			else if (commit.Subject.StartsWith(MergedBranchSubject2)
				&& commit.Subject.Length > MergedBranchSubject2.Length + 1)
			{
				// Found a "Merged "
				sourceBranchNameStart = MergedBranchSubject2.Length;
			}
			else if (commit.Subject.StartsWith(MergeRemoteTrackingBranch)
				&& commit.Subject.Length > MergeRemoteTrackingBranch.Length + 1)
			{
				// Found a "Merge remote-tracking branch "
				sourceBranchNameStart = MergeRemoteTrackingBranch.Length;
			}
			else if (commit.Subject.StartsWith(MergeBranchSubject2)
				&& commit.Subject.Length > MergeBranchSubject2.Length + 1)
			{
				// Found a "Merge "
				sourceBranchNameStart = MergeBranchSubject2.Length;
			}


			if (sourceBranchNameStart > 0)
			{
				sourceBranchNameEnd = commit.Subject.IndexOf(' ', sourceBranchNameStart);

				if (sourceBranchNameEnd == -1)
				{
					sourceBranchNameEnd = commit.Subject.Length;
				}
			}


			string sourceBranchName = null;
			string targetBranchName = null;
			if (sourceBranchNameEnd > 0)
			{
				// There is a source branch name
				sourceBranchName = commit.Subject.Substring(sourceBranchNameStart, sourceBranchNameEnd - sourceBranchNameStart);

				// Lets try to get a targter branch in messages like:
				// "Merge <source-branch> into <target-branch>"
				if (commit.Subject.Length > sourceBranchNameEnd + IntoText.Length)
				{
					int intoIndex = commit.Subject.IndexOf(IntoText, sourceBranchNameEnd);
					int ofIndex = commit.Subject.IndexOf(OfText, sourceBranchNameEnd);

					if (intoIndex > 0 && commit.Subject.Length > intoIndex + IntoText.Length)
					{
						// Found the "into" word after the source branch name
						targetBranchName = commit.Subject.Substring(intoIndex + IntoText.Length);
					}
					else if (intoIndex == -1 && ofIndex > 0 && commit.Subject.Length > ofIndex + OfText.Length)
					{
						// Found the "into" word after the source branch name
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



			if (commit.SecondParent != Commit.None && sourceBranchName == null)
			{
				//Log.Warn(
				//	$"Failed to parse source branch of {commit.DateTime} {commit.Subject} {commit.Author}");
			}

			if (sourceBranchName == "" || targetBranchName == "")
			{

			}

			return new MergeBranchNames(sourceBranchName, targetBranchName);
		}
	}
}