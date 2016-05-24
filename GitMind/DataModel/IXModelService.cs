using GitMind.DataModel.Private;
using GitMind.Git;


namespace GitMind.DataModel
{
	internal interface IXModelService
	{
		XModel XGetModel(IGitRepo gitRepo);
	}
}