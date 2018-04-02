using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;


namespace GitMind.Common
{
	internal static class ResultExtensions
	{
		public static R AsR(this GitResult gitResult) => gitResult.IsOk ? R.Ok : Error.From(gitResult);
	}
}