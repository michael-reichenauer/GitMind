namespace GitMind.ApplicationHandling
{
	internal interface IApplicationService
	{
		void SetIsStarted();

		void Start();

		bool IsActivatedOtherInstance(string workingFolder);

		bool IsCommands();

		void HandleCommands();

		void TryDeleteTempFiles();
		string WorkingFolder { get; }
	}
}