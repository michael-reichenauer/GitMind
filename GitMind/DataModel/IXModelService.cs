using GitMind.DataModel.Private;
using GitMind.Git;


namespace GitMind.DataModel
{
	internal interface IXModelService
	{
		MModel XGetModel(IGitRepo gitRepo);
	}
}