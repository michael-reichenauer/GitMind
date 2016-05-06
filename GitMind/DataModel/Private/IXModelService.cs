using GitMind.Git;


namespace GitMind.DataModel.Private
{
	internal interface IXModelService
	{
		XModel XGetModel(IGitRepo gitRepo);
	}
}