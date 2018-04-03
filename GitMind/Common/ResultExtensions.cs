using GitMind.Utils;
using GitMind.Utils.Git;
using GitMind.Utils.Git.Private;
using GitMind.Utils.OsSystem;


namespace GitMind.Common
{
	internal static class ResultExtensions
	{
		public static R AsR(this CmdResult2 result) => result.IsOk ? R.Ok : Error.From(result);
	}
}