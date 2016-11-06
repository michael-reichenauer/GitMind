using System.Threading.Tasks;
using GitMind.GitModel;
using GitMind.Utils.UI;


namespace GitMind.Features.Committing
{
	internal class UncommitCommand : Command<Commit>
	{
		private readonly ICommitService commitService;



		public UncommitCommand(ICommitService commitService)
		{
			this.commitService = commitService;
		}


		protected override Task RunAsync(Commit commit)
		{
			return commitService.UnCommitAsync(commit);
		}


		protected override bool CanRun(Commit commit)
		{
			return commit.Id != Commit.UncommittedId && commit.IsCurrent && commit.IsLocalAhead;
		}
	}
}