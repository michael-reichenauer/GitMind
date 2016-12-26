using System.Threading.Tasks;


namespace GitMind.GitModel.Private
{
	internal interface IBranchTipMonitorService
	{
		Task CheckAsync(Repository repository);
	}
}