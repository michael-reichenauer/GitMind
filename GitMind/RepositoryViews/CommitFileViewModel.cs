using GitMind.Utils.UI;


namespace GitMind.RepositoryViews
{
	internal class CommitFileViewModel : ViewModel
	{
		private readonly IDiffService diffService = new DiffService();

		public string Id { get; set; }

		public string WorkingFolder { get; set; }

		public string Name
		{
			get { return Get(); }
			set { Set(value); }
		}

		public string Status
		{
			get { return Get(); }
			set { Set(value); }
		}


		public Command ShowDiffCommand => Command(
			() => diffService.ShowFileDiffAsync(WorkingFolder, Id, Name));
	}
}