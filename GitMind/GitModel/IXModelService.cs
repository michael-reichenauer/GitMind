using GitMind.Git;
using GitMind.GitModel.Private;


namespace GitMind.GitModel
{
	internal interface IXModelService
	{
		MModel XGetModel(IGitRepo gitRepo);
	}
}