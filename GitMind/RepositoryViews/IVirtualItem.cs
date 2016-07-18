namespace GitMind.RepositoryViews
{
	internal interface IVirtualItem
	{
		string Id { get; }
		int VirtualId { get; }
	}
}