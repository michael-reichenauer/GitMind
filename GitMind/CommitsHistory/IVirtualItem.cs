namespace GitMind.CommitsHistory
{
	internal interface IVirtualItem
	{
		string Id { get; }
		int VirtualId { get; }
	}
}